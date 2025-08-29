using System.ComponentModel.DataAnnotations;

namespace DocumentManager.API.Domain.DTOs;

public class FolderCreateDto
{
    [Required, MaxLength(255)]
    public string FolderName { get; set; } = null!;

    public int? ParentId { get; set; }
}