using System.ComponentModel.DataAnnotations;

namespace DocumentManager.API.Domain.Entities;

public class FileEntity
{
    public int Id { get; set; }

    [Required, MaxLength(500)]
    public string FileName { get; set; } = null!;         // Orijinal ad

    [Required, MaxLength(500)]
    public string StoredFileName { get; set; } = null!;   // Sunucudaki benzersiz ad (GUID + ext)

    [Required, MaxLength(600)]
    public string RelativePath { get; set; } = null!;     // /uploads/xxx.pdf

    [Required, MaxLength(200)]
    public string ContentType { get; set; } = "application/octet-stream";

    public long Size { get; set; }

    // Existing folder relationship
    public int? FolderId { get; set; }
    public Folder? Folder { get; set; }

    // NEW: User ownership
    public int? UserId { get; set; }
    public User? User { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastOpenedAt { get; set; }

    // NEW: Soft delete fields
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}