using DocumentManager.API.Domain.DTOs.Auth;
using DocumentManager.API.Domain.Entities;

namespace DocumentManager.API.Services.Users;

public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(int userId);
    Task<UserDto?> GetUserByUsernameAsync(string username);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<UserDto> CreateUserAsync(RegisterDto registerDto);
    Task<UserDto> UpdateUserAsync(int userId, UserDto userDto);
    Task<bool> DeleteUserAsync(int userId);
    Task<bool> UserExistsAsync(int userId);
    Task<bool> UserExistsByUsernameAsync(string username);
    Task<bool> UserExistsByEmailAsync(string email);
    Task<UserDto?> ValidateUserCredentialsAsync(string username, string password);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<bool> IsUserInRoleAsync(int userId, string roleName);
    Task<bool> AssignRoleToUserAsync(int userId, string roleName);
    Task<bool> RemoveRoleFromUserAsync(int userId, string roleName);
    Task<IEnumerable<string>> GetUserRolesAsync(int userId);
}