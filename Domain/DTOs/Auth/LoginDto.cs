using System.ComponentModel.DataAnnotations;

namespace DocumentManager.API.Domain.DTOs.Auth;

public class LoginDto
{
    [Required, MaxLength(100)]
    public string Username { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Password { get; set; } = null!;
}