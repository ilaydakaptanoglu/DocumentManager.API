using DocumentManager.API.Domain.DTOs.Auth;
using DocumentManager.API.Domain.Entities;
using DocumentManager.API.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DocumentManager.API.Services.Users;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;

    public UserService(IUnitOfWork unitOfWork, ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        return user != null ? MapToUserDto(user) : null;
    }

    public async Task<UserDto?> GetUserByUsernameAsync(string username)
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        var user = users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        return user != null ? MapToUserDto(user) : null;
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        var user = users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        return user != null ? MapToUserDto(user) : null;
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        return users.Select(MapToUserDto);
    }

    public async Task<UserDto> CreateUserAsync(RegisterDto registerDto)
    {
        if (await UserExistsByUsernameAsync(registerDto.Username))
            throw new InvalidOperationException("Username already exists");

        if (await UserExistsByEmailAsync(registerDto.Email))
            throw new InvalidOperationException("Email already exists");

        var user = new User
        {
            Username = registerDto.Username,
            Email = registerDto.Email,
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _unitOfWork.Users.AddAsync(user);

        // Default Role
        var roles = await _unitOfWork.Roles.GetAllAsync();
        var userRole = roles.FirstOrDefault(r => r.Name == "User") ?? new Role { Name = "User" };
        if (!roles.Contains(userRole)) await _unitOfWork.Roles.AddAsync(userRole);

        user.Roles.Add(userRole);

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User created successfully: {Username}", user.Username);
        return MapToUserDto(user);
    }

    public async Task<UserDto> UpdateUserAsync(int userId, UserDto userDto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) throw new ArgumentException("User not found");

        user.FirstName = userDto.FirstName;
        user.LastName = userDto.LastName;
        user.Email = userDto.Email;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User updated successfully: {UserId}", userId);
        return MapToUserDto(user);
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) return false;

        _unitOfWork.Users.Remove(user); // Delete yerine Remove kullanıyoruz
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User deleted successfully: {UserId}", userId);
        return true;
    }

    public async Task<bool> UserExistsAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        return user != null;
    }

    public async Task<bool> UserExistsByUsernameAsync(string username)
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        return users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> UserExistsByEmailAsync(string email)
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        return users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<UserDto?> ValidateUserCredentialsAsync(string username, string password)
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        var user = users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;
        if (!user.IsActive) throw new InvalidOperationException("User account is deactivated");

        user.LastLoginAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return MapToUserDto(user);
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) return false;

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash)) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
        return true;
    }

    public async Task<bool> IsUserInRoleAsync(int userId, string roleName)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        return user?.Roles.Any(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase)) ?? false;
    }

    public async Task<bool> AssignRoleToUserAsync(int userId, string roleName)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) return false;

        var roles = await _unitOfWork.Roles.GetAllAsync();
        var role = roles.FirstOrDefault(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase))
                   ?? new Role { Name = roleName };

        if (!roles.Contains(role)) await _unitOfWork.Roles.AddAsync(role);
        if (!user.Roles.Contains(role)) user.Roles.Add(role);

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Role {RoleName} assigned to user {UserId}", roleName, userId);
        return true;
    }

    public async Task<bool> RemoveRoleFromUserAsync(int userId, string roleName)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user?.Roles == null) return false;

        var roleToRemove = user.Roles.FirstOrDefault(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
        if (roleToRemove != null)
        {
            user.Roles.Remove(roleToRemove);
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Role {RoleName} removed from user {UserId}", roleName, userId);
            return true;
        }

        return false;
    }

    public async Task<IEnumerable<string>> GetUserRolesAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        return user?.Roles.Select(r => r.Name) ?? Enumerable.Empty<string>();
    }

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
