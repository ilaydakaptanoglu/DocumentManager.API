using Microsoft.AspNetCore.Http;

namespace DocumentManager.API.Infrastructure.Storage;

public interface IFileStorage
{
    Task<(string storedFileName, string relativePath)> SaveAsync(IFormFile file);
    Task<bool> DeleteAsync(string storedFileName);
}