using DocumentManager.API.Domain.DTOs;
using DocumentManager.API.Domain.Entities;
using DocumentManager.API.Infrastructure.Repositories;
using DocumentManager.API.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Security.Claims;

namespace DocumentManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // 🔒 Authentication required for all endpoints
public class FilesController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IFileStorage _storage;

    public FilesController(IUnitOfWork uow, IFileStorage storage)
    {
        _uow = uow;
        _storage = storage;
    }

    // Upload - with user ownership
    [HttpPost("upload")]
    [RequestSizeLimit(1073741824)] // 1GB
    public async Task<IActionResult> Upload([FromForm] List<IFormFile> files, [FromForm] int? folderId)
    {
        if (files == null || files.Count == 0)
            return BadRequest("No files provided.");

        var userId = GetCurrentUserId();

        // Check folder ownership if folderId is provided
        if (folderId.HasValue)
        {
            var folder = await _uow.Folders.GetByIdAsync(folderId.Value);
            if (folder == null)
                return NotFound("Folder not found.");

            // Non-admin users can only upload to their own folders
            if (!IsAdmin() && folder.UserId != userId)
                return Forbid("You don't have permission to upload to this folder.");
        }

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
                UserId = userId, // 🔑 Set file owner
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

    // List - with ownership filtering
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? folderId = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var isAdmin = IsAdmin();

            // Parse folderId
            int? parsedFolderId = null;
            if (!string.IsNullOrEmpty(folderId) && folderId != "null")
            {
                if (int.TryParse(folderId, out int tempId))
                {
                    parsedFolderId = tempId;
                }
                else
                {
                    return BadRequest("Invalid folderId format");
                }
            }

            // Apply ownership filter based on role
            Expression<Func<FileEntity, bool>> filter;
            if (isAdmin)
            {
                filter = parsedFolderId.HasValue
                    ? f => f.FolderId == parsedFolderId
                    : f => f.FolderId == null;
            }
            else
            {
                filter = parsedFolderId.HasValue
                    ? f => f.FolderId == parsedFolderId && f.UserId == userId
                    : f => f.FolderId == null && f.UserId == userId;
            }

            var list = await _uow.Files.GetAllAsync(
                filter: filter,
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
            return StatusCode(500, $"Server error: {ex.Message}");
        }
    }

    // Recent - with ownership filtering
    [HttpGet("recent")]
    public async Task<IActionResult> Recent([FromQuery] int limit = 8)
    {
        var userId = GetCurrentUserId();
        var isAdmin = IsAdmin();

        Expression<Func<FileEntity, bool>> filter = isAdmin
            ? f => f.LastOpenedAt != null
            : f => f.LastOpenedAt != null && f.UserId == userId;

        var list = await _uow.Files.GetAllAsync(
            filter: filter,
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

    // Mark as opened - with ownership check
    [HttpPatch("open/{id}")]
    public async Task<IActionResult> MarkOpened(int id)
    {
        var userId = GetCurrentUserId();
        var entity = await _uow.Files.GetByIdAsync(id);

        if (entity == null)
            return NotFound();

        if (!IsAdmin() && entity.UserId != userId)
            return Forbid("You don't have permission to access this file.");

        entity.LastOpenedAt = DateTime.UtcNow;
        _uow.Files.Update(entity);
        await _uow.SaveChangesAsync();

        return NoContent();
    }

    // Download - with ownership check
    [HttpGet("download/{id}")]
    public async Task<IActionResult> Download(int id)
    {
        var userId = GetCurrentUserId();
        var entity = await _uow.Files.GetByIdAsync(id);

        if (entity == null)
            return NotFound();

        if (!IsAdmin() && entity.UserId != userId)
            return Forbid("You don't have permission to download this file.");

        var fileName = entity.StoredFileName;
        var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

        if (!Directory.Exists(uploadsRoot))
            Directory.CreateDirectory(uploadsRoot);

        var full = Path.Combine(uploadsRoot, fileName);

        if (!System.IO.File.Exists(full))
            return NotFound();

        return PhysicalFile(full, entity.ContentType, entity.FileName);
    }

    // 🗑️ SOFT DELETE - with ownership check
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        var entity = await _uow.Files.GetByIdAsync(id);

        if (entity == null)
            return NotFound();

        if (!IsAdmin() && entity.UserId != userId)
            return Forbid("You don't have permission to delete this file.");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;

        _uow.Files.Update(entity);
        await _uow.SaveChangesAsync();

        return NoContent();
    }

    // 🔧 Helper methods
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }
        return userId;
    }

    private bool IsAdmin()
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        return roleClaim == "Admin";
    }
}
