using System.ComponentModel.DataAnnotations;

namespace DocumentManager.API.Domain.Entities;

public class Folder
{
    public int Id { get; set; }

    [Required, MaxLength(255)]
    public string FolderName { get; set; } = null!;

    public int? ParentId { get; set; }
    public Folder? Parent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Folder> Children { get; set; } = new List<Folder>();
    public ICollection<FileEntity> Files { get; set; } = new List<FileEntity>();
}