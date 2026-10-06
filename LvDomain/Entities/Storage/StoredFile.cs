using LvDomain.Common;

namespace LvDomain.Entities.Storage;

/// <summary>
/// A small binary file kept in the database (PostgreSQL bytea), e.g. a profile photo. The app
/// host has no persistent disk, see docs/adr/0002-file-storage.md.
/// </summary>
public class StoredFile : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

#pragma warning disable CA1819 // EF Core maps byte[] to bytea; a copy-on-read wrapper buys nothing here.
    public byte[] Content { get; set; } = [];
#pragma warning restore CA1819
}
