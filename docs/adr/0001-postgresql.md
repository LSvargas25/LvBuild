# ADR 0001: PostgreSQL instead of SQL Server

- **Status:** accepted (2026-10-05)
- **Context:** Phase 1 of the production plan, before deploying to a free hosting tier

## Context

LvBuild started on SQL Server (EF Core `UseSqlServer`, `nvarchar`, `GETDATE()` defaults,
bracketed identifiers in filters and check constraints). The goal is a live demo that costs
nothing to run and that a technical reviewer can start locally with one command.

- Managed SQL Server has no usable free tier (Azure SQL's free offer is limited and tied to an
  Azure subscription), and the SQL Server container image is heavy and x64-only.
- Render's free web service is a good fit for the API, but Render does not offer SQL Server.
- Neon offers serverless PostgreSQL with a permanent free tier, branching and a pooled endpoint.

## Decision

Use **PostgreSQL** through **Npgsql.EntityFrameworkCore.PostgreSQL**, hosted on **Neon** in
production and on the official `postgres:16` image locally and in CI.

- `UseSnakeCaseNamingConvention()` (EFCore.NamingConventions) for tables, columns and indexes;
  hand-written names (`ToTable`, check constraints, filters) are already in `snake_case`.
- Timestamps are `timestamptz` and always written in UTC; calendar dates without time are `date`.
  `Npgsql.EnableLegacyTimestampBehavior` is **not** used.
- Text comparisons that must ignore case (emails, product SKUs) normalize the value or compare
  with `lower()`, because PostgreSQL's default collation is case-sensitive.
- Seeded rows with explicit Ids declare `HasIdentityOptions(startValue: ...)` above the highest
  seeded Id, and a test fails if a seed reaches that value.
- The migration history was reset into a single `InitialCreate` for PostgreSQL. There was no
  production data to preserve.
- Integration tests run against a real PostgreSQL through Testcontainers, so provider-specific
  behaviour (case sensitivity, `timestamptz`, constraints) is tested, not only EF InMemory.

## Consequences

- Zero hosting cost for the database, and the same engine in local dev, CI and production.
- Several SQL Server assumptions had to be fixed (UTC `DateTime.Kind`, case-insensitive
  comparisons, identity sequences after `HasData`); the conventions are documented in
  `docs/development.md`.
- Neon's free tier suspends idle compute: the first request after a pause is slower.
- Running migrations needs the **direct** Neon endpoint; the app can use the pooled one.

## Alternatives considered

- **Stay on SQL Server:** no free managed option compatible with Render.
- **SQLite:** free and simple, but a file on Render's ephemeral disk would be wiped on every
  deploy, and it lacks features the model relies on (`date`/`timestamptz` semantics, filtered
  unique indexes behaving like PostgreSQL/SQL Server).
- **Supabase PostgreSQL:** also viable; Neon was chosen for its pooled endpoint and branching,
  and because the project only needs the database, not Supabase's other services.
