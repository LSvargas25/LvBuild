using LvApplication.Services.Storage;
using LvDomain.Entities.Storage;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Storage;

/// <summary>Keeps files in the stored_files table (bytea). See docs/adr/0002-file-storage.md.</summary>
public class DatabaseFileStorageService : IFileStorageService
{
    private readonly AppDbContext _context;

    public DatabaseFileStorageService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveAsync(
        byte[] content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        var file = new StoredFile
        {
            FileName = Path.GetFileName(fileName),
            ContentType = contentType,
            SizeBytes = content.LongLength,
            Content = content,
            CreatedAt = DateTime.UtcNow,
        };
        _context.StoredFiles.Add(file);
        await _context.SaveChangesAsync(cancellationToken);
        return file.Id;
    }

    public Task<StoredFileContent?> GetAsync(
        int fileId,
        CancellationToken cancellationToken = default
    ) =>
        _context
            .StoredFiles.AsNoTracking()
            .Where(f => f.Id == fileId)
            .Select(f => new StoredFileContent(f.Content, f.ContentType, f.FileName))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task DeleteAsync(int fileId, CancellationToken cancellationToken = default)
    {
        var file = await _context.StoredFiles.FindAsync([fileId], cancellationToken);
        if (file is null)
            return;

        _context.StoredFiles.Remove(file);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
