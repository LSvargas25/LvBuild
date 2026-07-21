using LvApplication.Common.Exceptions;
using LvApplication.Services.Storage;
using Microsoft.Extensions.Configuration;

namespace LvInfrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private readonly string _webRootPath;

    public LocalFileStorageService(IConfiguration configuration)
    {
        _webRootPath = configuration["Storage:WebRootPath"] ?? "wwwroot";
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string originalContentType, string subfolder)
    {
        if (!AllowedContentTypes.Contains(originalContentType))
        {
            throw new ValidationAppException($"Tipo de archivo no permitido: {originalContentType}. Solo se permiten imágenes JPEG, PNG o WEBP.");
        }

        if (fileStream.Length > MaxFileSizeBytes)
        {
            throw new ValidationAppException("El archivo excede el tamaño máximo permitido de 5MB.");
        }

        var extension = Path.GetExtension(fileName);
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";

        var subfolderPhysicalPath = Path.Combine(_webRootPath, "uploads", subfolder);
        Directory.CreateDirectory(subfolderPhysicalPath);

        var physicalFilePath = Path.Combine(subfolderPhysicalPath, uniqueFileName);

        fileStream.Position = 0;
        await using (var outputStream = new FileStream(physicalFilePath, FileMode.Create, FileAccess.Write))
        {
            await fileStream.CopyToAsync(outputStream);
        }

        return $"/uploads/{subfolder}/{uniqueFileName}";
    }

    public void DeleteFile(string relativePath)
    {
        var physicalFilePath = Path.Combine(_webRootPath, relativePath.TrimStart('/', '\\'));

        if (File.Exists(physicalFilePath))
        {
            File.Delete(physicalFilePath);
        }
    }
}
