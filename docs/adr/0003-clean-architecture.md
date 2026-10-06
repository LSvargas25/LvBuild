# ADR 0003: Layered (Clean Architecture) solution structure

- **Status:** accepted (2026-10-05)
- **Context:** documents the structure the codebase already follows

## Context

LvBuild models a construction company: budgets, offers, projects, weekly site logs, payroll,
warehouse and a store with cash registers and invoices. Most of the value is in business rules
(state machines, stock, chapter costs, budget vs. actual) that must be testable without a web
server or a database, and that must not depend on the persistence technology, which already
changed once (ADR 0001).

## Decision

Four projects with dependencies pointing inwards:

| Project            | Contains                                                                 | Depends on            |
|--------------------|--------------------------------------------------------------------------|-----------------------|
| `LvDomain`         | Entities and enums. No framework references.                             | nothing               |
| `LvApplication`    | Services (use cases), DTOs, FluentValidation validators, repository and  | `LvDomain`            |
|                    | service interfaces (`IFileStorageService`, `ITokenService`, ...), errors |                       |
| `LvInfrastructure` | EF Core `AppDbContext`, configurations, migrations, repositories, JWT,   | `LvApplication`       |
|                    | PDF generation (QuestPDF), file storage, demo seeder                     |                       |
| `LvApi`            | Controllers, middleware (error mapping), auth, CORS, rate limiting,      | all of the above      |
|                    | health checks, Swagger, hosting configuration                            | (composition root)    |

- Controllers are thin: they read the user from the JWT, call one application service and
  return its DTO. Business rules and state transitions live in the services.
- Application errors (`NotFoundException`, `ConflictException`, `ForbiddenException`,
  `ValidationAppException`) are mapped to HTTP status codes by one middleware.
- Infrastructure details sit behind interfaces declared in `LvApplication`, so they can be
  replaced (e.g. moving profile photos from PostgreSQL to object storage, ADR 0002) without
  touching the use cases.

## Consequences

- Application services are unit-tested with EF Core InMemory; HTTP behaviour is tested with
  `WebApplicationFactory`; persistence rules are tested against real PostgreSQL.
- The demo seeder goes through the same application services as the API, so demo data obeys the
  same rules as real data.
- More files and mapping code than a single-project API; acceptable for a domain of this size.
- Repositories expose EF-shaped queries in places; a pure Clean Architecture would hide them
  further, but full abstraction over EF Core was not worth the cost here.

## Alternatives considered

- **Single project / vertical slices:** less ceremony, but the business rules would be coupled
  to ASP.NET Core and EF Core, which the PostgreSQL migration showed is a real risk.
- **Full DDD with rich aggregates and domain events:** more than this domain needs today; the
  state machines are small and well served by application services.
