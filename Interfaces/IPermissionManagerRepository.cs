using CRMWebServices.Helpers;
using static CRMWebServices.Models.PermissionModels;

namespace CRMWebServices.Interfaces
{
    public interface IPermissionManagerRepository
    {
        // Module
        Task<ApiResponse> CreateModule(CreateModuleRequest request);
        Task<ApiResponse> UpdateModule(UpdateModuleRequest request);
        Task<ApiResponse> DeactivateModule(DeactivateModuleRequest request);
        Task<ApiResponse> GetModules();
        Task<ApiResponse> GetSuperModules();
        Task<ApiResponse> GetRoleSuperModuleAccess(int roleId);
        Task<ApiResponse> SaveRoleSuperModuleAccess(SaveRoleSuperModuleAccessRequest request);

        // NEW — from this API-driven mapping step
        Task<ApiResponse> GetSuperModuleModules(int superModuleId);
        Task<ApiResponse> SaveSuperModuleMapping(SaveSuperModuleMappingRequest request);

        // Permission catalog
        Task<ApiResponse> CreatePermission(CreatePermissionRequest request);
        Task<ApiResponse> UpdatePermission(UpdatePermissionRequest request);
        Task<ApiResponse> DeactivatePermission(DeactivatePermissionRequest request);
        Task<ApiResponse> GetModulePermissions(int moduleId);

        // Role -> Permission assignment
        Task<ApiResponse> GetRolePermissions(int roleId, int? moduleId, int? superModuleId = null);
        Task<ApiResponse> SaveRolePermissions(SaveRolePermissionsRequest request);

        // Role -> Module access
        Task<ApiResponse> GetRoleModuleAccess(int roleId);
        Task<ApiResponse> SaveRoleModuleAccess(SaveRoleModuleAccessRequest request);
    }
}
