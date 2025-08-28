namespace DocumentManager.API.Domain.DTOs;

public class FileDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long Size { get; set; }
    public int? FolderId { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? LastOpenedAt { get; set; }
    public string Url { get; set; } = null!;
}