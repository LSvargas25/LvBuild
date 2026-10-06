# ADR 0002: Generated and uploaded files without a persistent disk

- **Status:** accepted (2026-10-05)
- **Context:** Phase 2, deployment to Render (Docker, free plan) + Neon PostgreSQL

## Context

The API produced two kinds of files and kept both on the local disk:

1. **Offer PDFs**: written to `GeneratedFiles/Offers/OF-YYYY-NNNN.pdf` when an offer was sent to
   the client. The path was stored in `offers.generated_pdf_path` and served by
   `GET /api/offers/{id}/pdf`.
2. **Profile photos**: uploaded to `wwwroot/uploads/profile-photos/` (up to 5 MB) and served as
   static files.

On Render, the container's filesystem is ephemeral: it is wiped on every deploy and every restart,
and the free plan offers no persistent disk. Both kinds of files would disappear, leaving broken
links in the database. The container also runs as a non-root user that cannot write next to the
binaries.

## Decision

**Offer PDFs are generated on demand, in memory.** `GET /api/offers/{id}/pdf` renders the PDF with
QuestPDF from the offer data on every request and streams it. Nothing is written to disk and the
`generated_pdf_path` column is gone. The API now exposes `pdfUrl` instead of a file path, set once
the offer leaves Draft. An offer is small (a few chapters), so rendering takes milliseconds.

**Profile photos are stored in PostgreSQL** (`stored_files`, a `bytea` column) behind
`IFileStorageService` (`DatabaseFileStorageService`). `users.profile_photo_file_id` references the
file, and `GET /api/users/{id}/photo` (authenticated) serves it. Uploads are limited to **1 MB**
and to **JPEG, PNG or WEBP**. The type is checked against the file's magic bytes, not just the
client-supplied `Content-Type`. The new photo is stored before the old one is deleted.

## Consequences

- Deploys and restarts lose nothing; the database is the only state.
- Neon's free tier has limited storage. With 1 MB photos and a handful of users this is
  negligible, but the table must not become general-purpose blob storage.
- Photos travel through the API instead of a CDN. That is fine for avatars at this scale; the
  response carries `Cache-Control: private, max-age=300`.
- A PDF always reflects the current offer data. Offers are no longer editable once sent, so the
  document a client received can be regenerated identically. The exception is the company
  name/logo, which come from configuration.

## Alternatives considered

- **Render persistent disk:** not available on the free plan, and it ties the service to a
  single instance.
- **Object storage (S3, Cloudflare R2, Supabase Storage):** the right choice for large files or
  many users, but it adds a third service, credentials and signed URLs for a demo that stores
  a few avatars. `IFileStorageService` keeps that switch to a single new implementation.
- **Keeping generated PDFs in the database:** unnecessary, because they can be derived from data
  that is already stored.
