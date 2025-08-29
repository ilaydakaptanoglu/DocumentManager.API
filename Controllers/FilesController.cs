using DocumentManager.API.Domain.DTOs;
using DocumentManager.API.Domain.Entities;
using DocumentManager.API.Infrastructure.Repositories;
using DocumentManager.API.Infrastructure.Storage;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IFileStorage _storage;

    public FilesController(IUnitOfWork uow, IFileStorage storage)
    {
        _uow = uow;
        _storage = storage;
    }

    // Upload
    [HttpPost("upload")]
    [RequestSizeLimit(1073741824)] // 1GB
    public async Task<IActionResult> Upload([FromForm] List<IFormFile> files, [FromForm] int? folderId)
    {
        if (files == null || files.Count == 0) return BadRequest("No files provided.");

        // 📌 Uploads klasörünü kontrol et ve yoksa oluştur
        var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
        if (!Directory.Exists(uploadsRoot))
        {
            Directory.CreateDirectory(uploadsRoot);
        }

        var results = new List<FileDto>();

        foreach (var f in files)
        {
            var (stored, relative) = await _storage.SaveAsync(f);

            var entity = new FileEntity
            {
                FileName = f.FileName,
                StoredFileName = stored,
                RelativePath = relative,
                ContentType = f.ContentType ?? "application/octet-stream",
                Size = f.Length,
                FolderId = folderId,
                UploadedAt = DateTime.UtcNow
            };

            await _uow.Files.AddAsync(entity);
            await _uow.SaveChangesAsync();

            results.Add(new FileDto
            {
                Id = entity.Id,
                FileName = entity.FileName,
                ContentType = entity.ContentType,
                Size = entity.Size,
                FolderId = entity.FolderId,
                UploadedAt = entity.UploadedAt,
                LastOpenedAt = entity.LastOpenedAt,
                Url = $"{Request.Scheme}://{Request.Host}{entity.RelativePath}"
            });
        }

        return Ok(results);
    }

    // List (folder bazlı)
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? folderId = null) // string? olarak değiştir
    {
        try
        {
            // String'den int?'ye dönüştürme
            int? parsedFolderId = null;

            if (!string.IsNullOrEmpty(folderId) && folderId != "null")
            {
                if (int.TryParse(folderId, out int tempId))
                {
                    parsedFolderId = tempId;
                }
                else
                {
                    return BadRequest("Geçersiz folderId formatı");
                }
            }

            var list = await _uow.Files.GetAllAsync(
                filter: parsedFolderId.HasValue ? f => f.FolderId == parsedFolderId : f => f.FolderId == null,
                orderBy: q => q.OrderByDescending(x => x.UploadedAt)
            );

            var result = list.Select(f => new FileDto
            {
                Id = f.Id,
                FileName = f.FileName,
                ContentType = f.ContentType,
                Size = f.Size,
                FolderId = f.FolderId,
                UploadedAt = f.UploadedAt,
                LastOpenedAt = f.LastOpenedAt,
                Url = $"{Request.Scheme}://{Request.Host}{f.RelativePath}"
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Sunucu hatası: {ex.Message}");
        }
    }

    // Recent
    [HttpGet("recent")]
    public async Task<IActionResult> Recent([FromQuery] int limit = 8)
    {
        var list = await _uow.Files.GetAllAsync(
            filter: f => f.LastOpenedAt != null,
            orderBy: q => q.OrderByDescending(x => x.LastOpenedAt)
        );

        var result = list.Take(limit).Select(f => new FileDto
        {
            Id = f.Id,
            FileName = f.FileName,
            ContentType = f.ContentType,
            Size = f.Size,
            FolderId = f.FolderId,
            UploadedAt = f.UploadedAt,
            LastOpenedAt = f.LastOpenedAt,
            Url = $"{Request.Scheme}://{Request.Host}{f.RelativePath}"
        });

        return Ok(result);
    }

    // Mark as opened (Home > Son Açılanlar için)
    [HttpPatch("open/{id}")]
    public async Task<IActionResult> MarkOpened(int id)
    {
        var entity = await _uow.Files.GetByIdAsync(id);
        if (entity == null) return NotFound();

        entity.LastOpenedAt = DateTime.UtcNow;
        _uow.Files.Update(entity);
        await _uow.SaveChangesAsync();
        return NoContent();
    }

    // Download
    [HttpGet("download/{id}")]
    public async Task<IActionResult> Download(int id)
    {
        var entity = await _uow.Files.GetByIdAsync(id);
        if (entity == null) return NotFound();

        var fileName = entity.StoredFileName;
        var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

        // 📌 Yine klasörü kontrol et
        if (!Directory.Exists(uploadsRoot))
        {
            Directory.CreateDirectory(uploadsRoot);
        }

        var full = Path.Combine(uploadsRoot, fileName);

        if (!System.IO.File.Exists(full)) return NotFound();

        return PhysicalFile(full, entity.ContentType, entity.FileName);
    }

    // Delete (frontend'teki çöp kutusu için)
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _uow.Files.GetByIdAsync(id);
        if (entity == null) return NotFound();

        var ok = await _storage.DeleteAsync(entity.StoredFileName);
        if (!ok) return StatusCode(500, "Physical file could not be deleted.");

        _uow.Files.Remove(entity);
        await _uow.SaveChangesAsync();
        return NoContent();
    }
}