using System.ComponentModel.DataAnnotations;

namespace DocumentManager.API.Domain.Entities;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Username { get; set; } = null!;

    [Required, MaxLength(255), EmailAddress]
    public string Email { get; set; } = null!;

    [Required, MaxLength(500)]
    public string PasswordHash { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public ICollection<FileEntity> Files { get; set; } = new List<FileEntity>();
    public ICollection<Folder> Folders { get; set; } = new List<Folder>();
}