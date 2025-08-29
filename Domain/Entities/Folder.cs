using System.ComponentModel.DataAnnotations;

namespace DocumentManager.API.Domain.Entities;

public class Folder
{
    public int Id { get; set; }

    [Required, MaxLength(255)]
    public string FolderName { get; set; } = null!;

    // Existing parent-child relationship
    public int? ParentId { get; set; }
    public Folder? Parent { get; set; }

    // NEW: User ownership
    public int? UserId { get; set; }
    public User? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // NEW: Soft delete fields
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation Properties
    public ICollection<Folder> Children { get; set; } = new List<Folder>();
    public ICollection<FileEntity> Files { get; set; } = new List<FileEntity>();
}