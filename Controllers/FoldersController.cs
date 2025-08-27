using DocumentManager.API.Domain.DTOs;
using DocumentManager.API.Domain.Entities;
using DocumentManager.API.Infrastructure.Repositories;
using DocumentManager.API.Infrastructure.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FoldersController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IFileStorage _storage; // FileStorage servisini enjekte et

    public FoldersController(IUnitOfWork uow, IFileStorage storage) // Ctor'a ekle
    {
        _uow = uow;
        _storage = storage;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? parentId)
    {
        var list = await _uow.Folders.GetAllAsync(
            filter: parentId.HasValue ? f => f.ParentId == parentId : f => f.ParentId == null,
            orderBy: q => q.OrderBy(x => x.FolderName)
        );
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FolderCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FolderName))
            return BadRequest("FolderName is required");

        var entity = new Folder
        {
            FolderName = dto.FolderName.Trim(),
            ParentId = dto.ParentId,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Folders.AddAsync(entity);
        await _uow.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = entity.Id }, entity);
    }

    // 📌 YENİ EKLEME: KLASÖR VE İÇERİKLERİNİ SİLME ENDPOINT'İ
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var folder = await _uow.Folders.GetByIdAsync(id);
        if (folder == null) return NotFound();

        // Recursively delete children files and folders
        await DeleteRecursive(id);

        return NoContent();
    }

    // 📌 RECURSIVE DELETE FUNCTION
    private async Task DeleteRecursive(int folderId)
    {
        // Önce alt klasörleri ve içindekileri sil
        var childFolders = await _uow.Folders.GetAllAsync(filter: f => f.ParentId == folderId);
        foreach (var childFolder in childFolders)
        {
            await DeleteRecursive(childFolder.Id);
        }

        // Sonra bu klasördeki dosyaları sil
        var filesInFolder = await _uow.Files.GetAllAsync(filter: f => f.FolderId == folderId);
        foreach (var file in filesInFolder)
        {
            await _storage.DeleteAsync(file.StoredFileName);
            _uow.Files.Remove(file);
        }
        await _uow.SaveChangesAsync();

        // Son olarak klasörü sil
        var folderToDelete = await _uow.Folders.GetByIdAsync(folderId);
        if (folderToDelete != null)
        {
            _uow.Folders.Remove(folderToDelete);
            await _uow.SaveChangesAsync();
        }
    }
}