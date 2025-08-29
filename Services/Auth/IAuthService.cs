// Services/Auth/IAuthService.cs
using DocumentManager.API.Data;
using DocumentManager.API.Domain.DTOs.Auth;
using DocumentManager.API.Domain.Entities;

namespace DocumentManager.API.Services.Auth;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<UserDto?> GetCurrentUserAsync(int userId);
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
}