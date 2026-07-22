using System.Security.Claims;

namespace CRMWebServices.Helpers
{
    public static class ClaimsPrincipalExtension
    {
        public static int EmployeeId(this ClaimsPrincipal user)
        {
            return int.Parse(user.FindFirst("EmployeeId")!.Value);
        }

        public static bool HasRole(this ClaimsPrincipal user, params int[] roles)
        {
            var roleClaim = user.FindFirst("RoleIds")?.Value;

            if (string.IsNullOrWhiteSpace(roleClaim))
                return false;

            var userRoles = roleClaim
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(int.Parse);

            return userRoles.Any(r => roles.Contains(r));
        }
    }
}