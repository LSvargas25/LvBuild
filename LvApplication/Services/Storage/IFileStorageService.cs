namespace LvApplication.Services.Storage;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string originalContentType, string subfolder);
    void DeleteFile(string relativePath);
}
