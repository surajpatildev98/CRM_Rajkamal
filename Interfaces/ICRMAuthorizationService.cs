using System.Security.Claims;
using static CRMWebServices.Models.PermissionModels;

namespace CRMWebServices.Interfaces
{
    public interface ICRMAuthorizationService
    {
        //bool CanCreateLead( ClaimsPrincipal user,int? assignedEmployeeId);

        //Task<bool> CanAccessLead( ClaimsPrincipal user, int leadId);

        //Task<bool> CanModifyLead(ClaimsPrincipal user, int leadId);

        //bool CanCreateOpportunity( ClaimsPrincipal user,int? assignedEmployeeId);

        //Task<bool> CanAccessOpportunity(ClaimsPrincipal user,int opportunityId);

        //Task<bool> CanConvertOpportunity(ClaimsPrincipal user, int leadId); 


        Task<bool> CanPerformAction(
           ClaimsPrincipal user,
           string resourceType,
           string action,
           int? resourceId = null
       );

        // Generic: Check action with assignment
        Task<bool> CanPerformActionWithAssignment(
            ClaimsPrincipal user,
            string resourceType,
            string action,
            int? assignedEmployeeId
        );

        // Get access filter for lists
        Task<PermissionFilter> GetAccessFilter(
            ClaimsPrincipal user,
            string resourceType
        );

        // Check ViewAll
        Task<bool> CanViewAll(
            ClaimsPrincipal user,
            string resourceType
        );

        // Get all user permissions
        Task<Dictionary<string, bool>> GetUserPermissions(
            ClaimsPrincipal user
        );

        // Check specific permission
        Task<bool> HasPermission(
            ClaimsPrincipal user,
            string permissionCode
        );

    }
}
