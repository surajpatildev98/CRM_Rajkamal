using System.Security.Claims;
using static CRMWebServices.Models.PermissionModels;

namespace CRMWebServices.Interfaces
{
    public interface ICRMAuthorizationService
    {   

        Task<bool> CanPerformAction(ClaimsPrincipal user,string resourceType,string action,int? resourceId = null);     
        Task<bool> CanPerformActionWithAssignment(ClaimsPrincipal user,string resourceType,string action,int? assignedEmployeeId);     
        Task<PermissionFilter> GetAccessFilter(ClaimsPrincipal user,string resourceTyp);      
        Task<bool> CanViewAll(ClaimsPrincipal user,string resourceType);       
        Task<Dictionary<string, bool>> GetUserPermissions(ClaimsPrincipal user);      
        Task<bool> HasPermission(ClaimsPrincipal user,string permissionCode);

    }
}
