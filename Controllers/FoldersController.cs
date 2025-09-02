using DocumentManager.API.Domain.DTOs;
using DocumentManager.API.Domain.Entities;
using DocumentManager.API.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Security.Claims;

namespace DocumentManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FoldersController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public FoldersController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FolderCreateDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();

            if (dto.ParentId.HasValue)
            {
                var parentFolder = await _uow.Folders.GetByIdAsync(dto.ParentId.Value);
                if (parentFolder == null)
                    return NotFound("Parent folder not found.");

                if (!IsAdmin() && parentFolder.UserId != userId)
                    return Forbid("You don't have permission to create a folder here.");
            }

            var folder = new Folder
            {
                FolderName = dto.FolderName,
                ParentId = dto.ParentId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Folders.AddAsync(folder);
            await _uow.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = folder.Id }, new FolderDto
            {
                Id = folder.Id,
                FolderName = folder.FolderName,
                ParentId = folder.ParentId,
                CreatedAt = folder.CreatedAt
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to create folder: {ex.Message}");
        }
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? parentId = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var isAdmin = IsAdmin();

            Expression<Func<Folder, bool>> filter;
            if (isAdmin)
            {
                filter = parentId.HasValue
                    ? f => f.ParentId == parentId
                    : f => f.ParentId == null;
            }
            else
            {
                filter = parentId.HasValue
                    ? f => f.ParentId == parentId && f.UserId == userId
                    : f => f.ParentId == null && f.UserId == userId;
            }

            var folders = await _uow.Folders.GetAllAsync(
                filter: filter,
                orderBy: q => q.OrderBy(x => x.FolderName)
            );

            var result = folders.Select(f => new FolderDto
            {
                Id = f.Id,
                FolderName = f.FolderName,
                ParentId = f.ParentId,
                CreatedAt = f.CreatedAt
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Server error: {ex.Message}");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var folder = await _uow.Folders.GetByIdAsync(id);

            if (folder == null)
                return NotFound();

            if (!IsAdmin() && folder.UserId != userId)
                return Forbid("You don't have permission to access this folder.");

            return Ok(new FolderDto
            {
                Id = folder.Id,
                FolderName = folder.FolderName,
                ParentId = folder.ParentId,
                CreatedAt = folder.CreatedAt
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to get folder: {ex.Message}");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] FolderUpdateDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var folder = await _uow.Folders.GetByIdAsync(id);

            if (folder == null)
                return NotFound();

            if (!IsAdmin() && folder.UserId != userId)
                return Forbid("You don't have permission to update this folder.");

            folder.FolderName = dto.FolderName;
            _uow.Folders.Update(folder);
            await _uow.SaveChangesAsync();

            return Ok(new FolderDto
            {
                Id = folder.Id,
                FolderName = folder.FolderName,
                ParentId = folder.ParentId,
                CreatedAt = folder.CreatedAt
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to update folder: {ex.Message}");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var folder = await _uow.Folders.GetByIdAsync(id);

            if (folder == null)
                return NotFound();

            if (!IsAdmin() && folder.UserId != userId)
                return Forbid("You don't have permission to delete this folder.");

            var hasChildren = await _uow.Folders.GetAllAsync(f => f.ParentId == folder.Id);
            var hasFiles = await _uow.Files.GetAllAsync(f => f.FolderId == folder.Id);

            if (hasChildren.Any() || hasFiles.Any())
                return BadRequest("Cannot delete folder that contains files or subfolders. Delete contents first.");

            folder.IsDeleted = true;
            folder.DeletedAt = DateTime.UtcNow;

            _uow.Folders.Update(folder);
            await _uow.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Failed to delete folder: {ex.Message}");
        }
    }

    [HttpDelete("{id}/force")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ForceDelete(int id)
    {
        try
        {
            await SoftDeleteFolderRecursive(id);
            await _uow.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Failed to force delete folder: {ex.Message}");
        }
    }

    [HttpGet("{id}/breadcrumb")]
    public async Task<IActionResult> GetBreadcrumb(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var breadcrumb = new List<FolderDto>();
            int? currentId = id;

            while (currentId.HasValue)
            {
                var folder = await _uow.Folders.GetByIdAsync(currentId.Value);
                if (folder == null) break;

                if (!IsAdmin() && folder.UserId != userId)
                    return Forbid("You don't have permission to access this folder hierarchy.");

                breadcrumb.Insert(0, new FolderDto
                {
                    Id = folder.Id,
                    FolderName = folder.FolderName,
                    ParentId = folder.ParentId,
                    CreatedAt = folder.CreatedAt
                });

                currentId = folder.ParentId;
            }

            return Ok(breadcrumb);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to get breadcrumb: {ex.Message}");
        }
    }

    private async Task SoftDeleteFolderRecursive(int folderId)
    {
        var childFolders = await _uow.Folders.GetAllAsync(f => f.ParentId == folderId);
        foreach (var child in childFolders)
            await SoftDeleteFolderRecursive(child.Id);

        var files = await _uow.Files.GetAllAsync(f => f.FolderId == folderId);
        foreach (var file in files)
        {
            file.IsDeleted = true;
            file.DeletedAt = DateTime.UtcNow;
            _uow.Files.Update(file);
        }

        var folder = await _uow.Folders.GetByIdAsync(folderId);
        if (folder != null)
        {
            folder.IsDeleted = true;
            folder.DeletedAt = DateTime.UtcNow;
            _uow.Folders.Update(folder);
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user ID in token");

        return userId;
    }

    private bool IsAdmin()
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        return roleClaim == "Admin";
    }
}

public class FolderUpdateDto
{
    public string FolderName { get; set; } = null!;
}
