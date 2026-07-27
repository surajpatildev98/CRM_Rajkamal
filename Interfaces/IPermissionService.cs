using CRMWebServices.Models;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using static CRMWebServices.Models.PermissionModels;

namespace CRMWebServices.Interfaces
{
    public interface IPermissionService
    {
        // Check single permission
        Task<bool> HasPermission(ClaimsPrincipal user, string permissionCode);

        // Check any permission
        Task<bool> HasAnyPermission(ClaimsPrincipal user, params string[] permissionCodes);

        // Check all permissions
        Task<bool> HasAllPermissions(ClaimsPrincipal user, params string[] permissionCodes);

        // Check multiple permissions
        Task<Dictionary<string, bool>> CheckMultiplePermissions(ClaimsPrincipal user, string[] permissionCodes);

        // Get user permissions
        Task<List<UserPermission>> GetUserPermissions(ClaimsPrincipal user);

        // Get user permissions as dictionary
        Task<Dictionary<string, bool>> GetUserPermissionDictionary(ClaimsPrincipal user);

        // Get resource owner
        Task<ResourceOwner> GetResourceOwner(string resourceType, int resourceId);

        // Get access filter
        Task<PermissionFilter> GetAccessFilter(ClaimsPrincipal user, string resourceType);

        // Get user modules
        Task<List<ModulePermission>> GetUserModules(ClaimsPrincipal user);

        // Invalidate cache
        void InvalidateCache(int employeeId);
    }
}