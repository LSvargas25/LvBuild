namespace LvApplication.Services.Storage;

/// <summary>
/// Stores small user files (profile photos). The implementation keeps them in the database
/// because the app host's disk is ephemeral (docs/adr/0002-file-storage.md); validation of
/// what may be stored belongs to the caller.
/// </summary>
public interface IFileStorageService
{
    /// <returns>The id of the stored file.</returns>
    Task<int> SaveAsync(
        byte[] content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default
    );

    Task<StoredFileContent?> GetAsync(int fileId, CancellationToken cancellationToken = default);

    Task DeleteAsync(int fileId, CancellationToken cancellationToken = default);
}

#pragma warning disable CA1819 // A simple carrier for the bytes returned to the HTTP layer.
public sealed record StoredFileContent(byte[] Content, string ContentType, string FileName);
#pragma warning restore CA1819
