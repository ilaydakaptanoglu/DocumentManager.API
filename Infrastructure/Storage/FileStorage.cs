using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace DocumentManager.API.Infrastructure.Storage;

public class FileStorage : IFileStorage
{
    private readonly string _uploadsRoot;

    public FileStorage(IWebHostEnvironment env, IConfiguration config)
    {
        var uploads = config["StoredFilesPath"] ?? "Uploads";
        _uploadsRoot = Path.Combine(env.ContentRootPath, uploads);
        Directory.CreateDirectory(_uploadsRoot);
    }

    public async Task<(string storedFileName, string relativePath)> SaveAsync(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName);
        var stored = $"{Guid.NewGuid()}{ext}";
        var full = Path.Combine(_uploadsRoot, stored);

        using (var fs = new FileStream(full, FileMode.Create))
        {
            await file.CopyToAsync(fs);
        }

        var relative = $"/uploads/{stored}";
        return (stored, relative);
    }

    public Task<bool> DeleteAsync(string storedFileName)
    {
        try
        {
            var full = Path.Combine(_uploadsRoot, storedFileName);
            if (File.Exists(full)) File.Delete(full);
            return Task.FromResult(true);
        }
        catch { return Task.FromResult(false); }
    }
}