using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using static CRMWebServices.Models.PermissionModels;

namespace CRMWebServices.Services
{
    public class AuthorizationService : ICRMAuthorizationService
    {
        private readonly IPermissionService _permissionService;
        private readonly IDatabaseService _database;

        public AuthorizationService(IPermissionService permissionService, IDatabaseService database)
        {
            _permissionService = permissionService;
            _database = database;
        }

        #region  Can Perform Action
        public async Task<bool> CanPerformAction(ClaimsPrincipal user,string resourceType,string action,int? resourceId = null)
        {
            if (user == null || !user.Identity.IsAuthenticated)
                return false;

            var employeeId = user.EmployeeId(); // Returns int

            // 1. Check ViewAll
            if (await _permissionService.HasPermission(user, $"{resourceType}_ViewAll"))
                return true;

            // 2. Check specific action permission
            var permissionCode = $"{resourceType}_{action}";
            if (!await _permissionService.HasPermission(user, permissionCode))
                return false;

            // 3. Create action - no ownership check
            if (action == "Create")
                return true;           

            // 4. Check ownership
            if (resourceId.HasValue)
            {
                var owner = await _permissionService.GetResourceOwner(resourceType, resourceId.Value);
                if (owner == null)
                    return false;

                // Transfer: only current assigned employee
                if (action == "Transfer")
                {
                    return owner.AssignedEmployeeId == employeeId;
                }

                // Assign: only Admin/Manager/PreSales should assign
                // Since they already passed the permission check above,
                // don't allow creator/owner to bypass it.
                if (action == "Assign")
                {
                    return true;
                }             

                // Default rule
                if (owner.CreatedBy == employeeId || owner.AssignedEmployeeId == employeeId)
                    return true;

                // Ticket-assigned employee can view the Opportunity
                if (resourceType == "Opp" && action == "View")
                {
                    var dt = await _database.ExecuteDataTableAsync("USP_CRM_TicketManagement",
                        new SqlParameter("@Status", "CHECK_OPPORTUNITY_TICKET_ACCESS"),
                        new SqlParameter("@OpportunityId", resourceId.Value),
                        new SqlParameter("@EmployeeId", employeeId)
                    );

                    return dt != null && dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0]["HasAccess"]) == 1;
                }

                return false;
            }

            return false;
        }
        #endregion

        #region  Get Resource Owner
        public async Task<bool> CanPerformActionWithAssignment(ClaimsPrincipal user,string resourceType,string action,int? assignedEmployeeId)
        {
            if (!await CanPerformAction(user, resourceType, action))
                return false;

            var employeeId = user.EmployeeId(); // Returns int

            if (assignedEmployeeId.HasValue && assignedEmployeeId.Value != employeeId)
            {
                return await _permissionService.HasPermission(user, $"{resourceType}_Assign");
            }

            return true;
        }
        #endregion

        #region  Get Access Filter
        public async Task<PermissionFilter> GetAccessFilter(ClaimsPrincipal user, string resourceType)
        {
            return await _permissionService.GetAccessFilter(user, resourceType);
        }
        #endregion

        #region  Get Resource Owner
        public async Task<bool> CanViewAll(ClaimsPrincipal user, string resourceType)
        {
            return await _permissionService.HasPermission(user, $"{resourceType}_ViewAll");
        }
        #endregion

        #region  Get User Permissions
        public async Task<Dictionary<string, bool>> GetUserPermissions(ClaimsPrincipal user)
        {
            return await _permissionService.GetUserPermissionDictionary(user);
        }
        #endregion

        #region  Get User Permission Codes
        public async Task<bool> HasPermission(ClaimsPrincipal user, string permissionCode)
        {
            return await _permissionService.HasPermission(user, permissionCode);
        }
        #endregion
    }
}