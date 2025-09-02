using System.ComponentModel.DataAnnotations;

namespace DocumentManager.API.Domain.DTOs.Auth;

public class RegisterDto
{
    [Required, MaxLength(100)]
    public string Username { get; set; } = null!;

    [Required, MaxLength(255), EmailAddress]
    public string Email { get; set; } = null!;

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [Required, MinLength(6), MaxLength(100)]
    public string Password { get; set; } = null!;

    [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = null!;
}
