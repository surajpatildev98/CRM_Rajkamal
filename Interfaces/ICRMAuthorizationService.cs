using System.Security.Claims;

namespace CRMWebServices.Interfaces
{
    public interface ICRMAuthorizationService
    {
        bool CanCreateLead( ClaimsPrincipal user,int? assignedEmployeeId);

        Task<bool> CanAccessLead( ClaimsPrincipal user, int leadId);

        bool CanCreateOpportunity( ClaimsPrincipal user,int? assignedEmployeeId);

        Task<bool> CanAccessOpportunity(ClaimsPrincipal user,int opportunityId);
    }
}
