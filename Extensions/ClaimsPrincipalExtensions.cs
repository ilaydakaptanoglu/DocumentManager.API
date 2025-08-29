using System.Security.Claims;

namespace DocumentManager.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }
        return userId;
    }

    public static string GetUserRole(this ClaimsPrincipal principal)
    {
        var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value;
        return roleClaim ?? throw new UnauthorizedAccessException("Role claim not found in token");
    }

    public static bool IsAdmin(this ClaimsPrincipal principal)
    {
        return principal.GetUserRole() == "Admin";
    }

    public static string GetUsername(this ClaimsPrincipal principal)
    {
        var usernameClaim = principal.FindFirst(ClaimTypes.Name)?.Value;
        return usernameClaim ?? throw new UnauthorizedAccessException("Username claim not found in token");
    }

    public static string GetUserEmail(this ClaimsPrincipal principal)
    {
        var emailClaim = principal.FindFirst(ClaimTypes.Email)?.Value;
        return emailClaim ?? throw new UnauthorizedAccessException("Email claim not found in token");
    }
}