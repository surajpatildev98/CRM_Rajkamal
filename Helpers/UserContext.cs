using System.Security.Claims;

namespace ResignationApi.Helpers;

/// <summary>
/// Reads the logged-in user from the login token. The app is never trusted
/// to say who it is or whether it is HR.
/// </summary>
public static class UserContext
{
    /// <summary>Looks for a claim named "employeeId", then the standard NameIdentifier claim.</summary>
    public static int GetEmployeeId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst("employeeId")?.Value
                    ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(value, out var id) || id <= 0)
            throw new UnauthorizedAccessException("Employee id was not found in the login token.");

        return id;
    }

    /// <summary>HR if the token has one of the roles listed in appsettings Resignation:HrRoles.</summary>
    public static bool IsHr(this ClaimsPrincipal user, IConfiguration config)
    {
        var roles = config.GetSection("Resignation:HrRoles").Get<string[]>()
                    ?? new[] { "HR", "HR Manager" };
        return roles.Any(user.IsInRole);
    }
}
