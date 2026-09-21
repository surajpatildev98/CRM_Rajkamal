namespace CRMWebServices.Models
{
    public partial class PermissionModels
    {
        public class UserPermission
        {
            public string PermissionCode { get; set; }
            public string PermissionName { get; set; }
            public string ModuleName { get; set; }
            public string Description { get; set; }
            public bool IsGranted { get; set; }
            public int PermissionId { get; set; }
        }
        public class ResourceOwner
        {
            public int ResourceId { get; set; }
            public int? CreatedBy { get; set; }
            public int? AssignedEmployeeId { get; set; }
            public string ResourceType { get; set; }
        }
        public class PermissionFilter
        {
            public bool CanViewAll { get; set; }
            public int? EmployeeId { get; set; }
            public string FilterType { get; set; } // "All", "CreatedAndAssigned", "Assigned", "None"
            public bool ShowCreatedAndAssigned { get; set; }
        }
        public class ModulePermission
        {
            public string ModuleName { get; set; }
            public string ModuleDisplayName { get; set; }
            public int TotalPermissions { get; set; }
        }
        public class UserRolePermission
        {
            public int RoleId { get; set; }
            public string RoleCode { get; set; }
            public string RoleName { get; set; }
            public string RoleDescription { get; set; }
            public bool IsPrimaryRole { get; set; }
            public string PermissionCode { get; set; }
            public string PermissionName { get; set; }
            public string ModuleName { get; set; }
            public string PermissionDescription { get; set; }
            public bool IsAllowed { get; set; }
            public string PermissionStatus { get; set; }
        }
        public class PermissionDto
        {
            public int PermissionId { get; set; }
            public string ModuleName { get; set; }
            public string PermissionCode { get; set; }
            public string PermissionName { get; set; }
            public string Description { get; set; }
            public bool IsActive { get; set; }
            public DateTime? CreatedOn { get; set; }
            public int? CreatedBy { get; set; }
        }
        public class LeadAccessInfo
        {
            public int LeadId { get; set; }           // ← Added this
            public int? CreatedBy { get; set; }
            public int? AssignedEmployeeId { get; set; }
            public int? Ref_LeadStatusId { get; set; }
        }
        public class OpportunityAccessInfo
        {
            public int OpportunityId { get; set; }    // ← Added this
            public int? CreatedBy { get; set; }
            public int? AssignedEmployeeId { get; set; }
            public int? Ref_OpportunityStageId { get; set; }
        }

    
        // ----- MODULE -----
        public class CreateModuleRequest
        {
            public string ModuleCode { get; set; }
            public string ModuleName { get; set; }
            public string? Description { get; set; }
            public string? IconName { get; set; }
            public int? DisplayOrder { get; set; }
            public int? CreatedBy { get; set; }
        }

        public class UpdateModuleRequest
        {
            public int ModuleId { get; set; }
            public string? ModuleName { get; set; }
            public string? Description { get; set; }
            public string? IconName { get; set; }
            public int? DisplayOrder { get; set; }
            public int? CreatedBy { get; set; }
        }

        public class DeactivateModuleRequest
        {
            public int ModuleId { get; set; }
            public int? CreatedBy { get; set; }
        }

        public class ModuleInfo
        {
            public int ModuleId { get; set; }
            public string ModuleCode { get; set; }
            public string ModuleName { get; set; }
            public string? Description { get; set; }
            public string? IconName { get; set; }
            public int? DisplayOrder { get; set; }
            public bool IsActive { get; set; }
            public int TotalPermissions { get; set; }
        }

        // ----- PERMISSION CATALOG -----
        public class CreatePermissionRequest
        {
            public int ModuleId { get; set; }
            public string PermissionCode { get; set; }
            public string PermissionName { get; set; }
            public string? Description { get; set; }
            public int? CreatedBy { get; set; }
        }

        public class UpdatePermissionRequest
        {
            public int PermissionId { get; set; }
            public string? PermissionName { get; set; }
            public string? Description { get; set; }
            public int? CreatedBy { get; set; }
        }

        public class DeactivatePermissionRequest
        {
            public int PermissionId { get; set; }
            public int? CreatedBy { get; set; }
        }

        public class ModulePermissionCatalogItem
        {
            public int PermissionId { get; set; }
            public string PermissionCode { get; set; }
            public string PermissionName { get; set; }
            public string? Description { get; set; }
            public bool IsActive { get; set; }
        }

        public class RolePermissionItem
        {
            public int ModuleId { get; set; }
            public string? ModuleName { get; set; }  
            public bool ModuleIsAllowed { get; set; }         
            public int PermissionId { get; set; }
            public string? PermissionCode { get; set; }
            public string? PermissionName { get; set; }
            public string? Description { get; set; }
            public bool IsAllowed { get; set; }
        }
        public class SaveRolePermissionItem
        {
            public int PermissionId { get; set; }
            public bool IsAllowed { get; set; }
        }       
        public class SaveRolePermissionsRequest
        {
            public int Ref_RoleId { get; set; }
            public List<SaveRoleModuleAccessItem>? Modules { get; set; }
            public List<SaveRolePermissionItem>? Permissions { get; set; }
            public List<SaveRoleSuperModuleAccessItem>? SuperModules { get; set; }   // NEW
            public int? CreatedBy { get; set; }
        }
        // ----- ROLE -> MODULE ACCESS -----
        public class RoleModuleAccessItem
        {
            public int ModuleId { get; set; }
            public string ModuleName { get; set; }
            public string? Description { get; set; }
            public string? IconName { get; set; }
            public bool IsEnabled { get; set; }
        }

        public class SaveRoleModuleAccessItem
        {
            public int ModuleId { get; set; }
            public bool IsEnabled { get; set; }
        }

        public class SaveRoleModuleAccessRequest
        {
            public int Ref_RoleId { get; set; }
            public List<SaveRoleModuleAccessItem> Modules { get; set; } = new();
            public int? CreatedBy { get; set; }
        }

        public class ModuleAccessInfo
        {
            public int ModuleId { get; set; }
            public string ModuleCode { get; set; }
            public string ModuleName { get; set; }
            public string? IconName { get; set; }
            public bool IsAllowed { get; set; }
            public List<PermissionAccessInfo> Permissions { get; set; } = new();
        }

        public class PermissionAccessInfo
        {
            public int PermissionId { get; set; }
            public string PermissionCode { get; set; }
            public string PermissionName { get; set; }
            public bool IsAllowed { get; set; }
        }

        public class RegisterEmployeeDeviceRequest
        {
            public string DeviceId { get; set; } = string.Empty;
            public string DeviceToken { get; set; } = string.Empty;
            public string DeviceType { get; set; } = string.Empty;
            public string? AppVersion { get; set; }
        }

        public class ForgotPasswordRequest
        {
            public string LoginId { get; set; }
        }

        public class VerifyResetOtpRequest
        {
            public string LoginId { get; set; }
            public string OTP { get; set; }
        }

        public class ResetPasswordRequest
        {
            public Guid ResetToken { get; set; }
            public string NewPassword { get; set; }
            public string ConfirmPassword { get; set; }
        }

        public class SuperModuleInfo
        {
            public int SuperModuleId { get; set; }
            public string? SuperModuleCode { get; set; }
            public string? SuperModuleName { get; set; }
            public string? Description { get; set; }
            public string? IconName { get; set; }
            public int? DisplayOrder { get; set; }
        }

        public class SuperModuleModuleItem
        {
            public int ModuleId { get; set; }
            public string? ModuleCode { get; set; }
            public string? ModuleName { get; set; }
            public string? IconName { get; set; }
            public int? DisplayOrder { get; set; }
            public bool IsMapped { get; set; }
        }

        public class SuperModuleMappingItem
        {
            public int ModuleId { get; set; }
            public bool IsMapped { get; set; }
        }

        public class SaveSuperModuleMappingRequest
        {
            public int Ref_SuperModuleId { get; set; }
            public List<SuperModuleMappingItem>? Modules { get; set; }
            public int? CreatedBy { get; set; }
        }

        public class SuperModuleAccessInfo
        {
            public int SuperModuleId { get; set; }
            public string? SuperModuleCode { get; set; }
            public string? SuperModuleName { get; set; }
            public string? IconName { get; set; }
            public bool IsAllowed { get; set; }
            public List<ModuleAccessInfo> Modules { get; set; } = new();
        }

        public class RoleSuperModuleAccessItem
        {
            public int SuperModuleId { get; set; }
            public string? SuperModuleCode { get; set; }
            public string? SuperModuleName { get; set; }
            public string? IconName { get; set; }
            public bool IsEnabled { get; set; }
        }

        public class SaveRoleSuperModuleAccessItem
        {
            public int SuperModuleId { get; set; }
            public bool IsEnabled { get; set; }
        }

        public class SaveRoleSuperModuleAccessRequest
        {
            public int Ref_RoleId { get; set; }
            public List<SaveRoleSuperModuleAccessItem>? SuperModules { get; set; }
            public int? CreatedBy { get; set; }
        }
    }
}
