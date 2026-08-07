namespace CRMWebServices.Models
{
    public class PermissionModels
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
    }
}
