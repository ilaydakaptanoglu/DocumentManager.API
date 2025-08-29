// Services/Auth/AuthService.cs
using DocumentManager.API.Data;
using DocumentManager.API.Domain.DTOs.Auth;
using DocumentManager.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace DocumentManager.API.Services.Auth;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly ITokenService _tokenService;

    public AuthService(ApplicationDbContext context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        // User'ı rolleriyle birlikte al
        var user = await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Username == dto.Username && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid username or password");
        }

        var token = _tokenService.GenerateToken(user);

        return new AuthResponseDto
        {
            Token = token,
            User = MapToUserDto(user),
            ExpiresAt = _tokenService.GetTokenExpiration()
        };
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        if (await UsernameExistsAsync(dto.Username))
            throw new ArgumentException("Username already exists");

        if (await EmailExistsAsync(dto.Email))
            throw new ArgumentException("Email already exists");

        // Default "User" rolünü al
        var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
        if (userRole == null)
            throw new InvalidOperationException("Default user role not found");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PasswordHash = passwordHash,
            Roles = new List<Role> { userRole },
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // User'ı rolleriyle tekrar yükle
        user = await _context.Users
            .Include(u => u.Roles)
            .FirstAsync(u => u.Id == user.Id);

        var token = _tokenService.GenerateToken(user);

        return new AuthResponseDto
        {
            Token = token,
            User = MapToUserDto(user),
            ExpiresAt = _tokenService.GetTokenExpiration()
        };
    }

    public async Task<UserDto?> GetCurrentUserAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

        return user == null ? null : MapToUserDto(user);
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await _context.Users.AnyAsync(u => u.Username == username);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    // ---------------------------
    // Helper: User -> UserDto
    // ---------------------------
    private UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Roles = user.Roles.Select(r => r.Name).ToList()
        };
    }
}
