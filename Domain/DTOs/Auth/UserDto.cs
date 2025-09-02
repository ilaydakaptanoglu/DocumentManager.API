namespace DocumentManager.API.Domain.DTOs.Auth;

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public List<string> Roles { get; set; } = new List<string>();
}
