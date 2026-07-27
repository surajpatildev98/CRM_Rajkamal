using CRMWebServices.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using static CRMWebServices.Models.PermissionModels;

namespace CRMWebServices.Interfaces
{
    public interface IPermissionRepository
    {
        // Get all permissions for a user
        Task<List<UserPermission>> GetUserPermissions(int employeeId);

        // Check single permission
        Task<bool> UserHasPermission(int employeeId, string permissionCode);

        // Check multiple permissions
        Task<Dictionary<string, bool>> UserHasMultiplePermissions(int employeeId, string[] permissionCodes);

        // Get resource owner
        Task<ResourceOwner> GetResourceOwner(string resourceType, int resourceId);

        // Get user roles with permissions
        Task<List<UserRolePermission>> GetUserRolePermissions(int employeeId);

        // Get user modules
        Task<List<ModulePermission>> GetUserModules(int employeeId);

        // Get all permissions
        Task<List<PermissionDto>> GetAllPermissions();

        // Batch check all permissions
        Task<Dictionary<string, bool>> BatchCheckPermissions(int employeeId);
    }
}