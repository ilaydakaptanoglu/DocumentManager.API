namespace DocumentManager.API.Domain.DTOs;

public class FolderCreateDto
{
    public string FolderName { get; set; } = null!;
    public int? ParentId { get; set; }
}
