namespace DocumentManager.API.Domain.DTOs;

public class FolderDto
{
    public int Id { get; set; }
    public string FolderName { get; set; } = null!;
    public int? ParentId { get; set; }
    public DateTime CreatedAt { get; set; }
}
