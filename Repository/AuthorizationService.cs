using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Repository;
using System.Security.Claims;

namespace CRMWebServices.Services
{
    public class AuthorizationService : ICRMAuthorizationService
    {
        private readonly ILeadRepository _repository;
        private readonly IOpportunityRepository _opportunityRepository;

        public AuthorizationService(ILeadRepository repository, IOpportunityRepository opportunityRepository)
        {
            _repository = repository;
            _opportunityRepository = opportunityRepository;
        }

        public bool CanCreateLead(ClaimsPrincipal user, int? assignedEmployeeId)
        {          

            if (user.HasRole( RoleIds.Admin, RoleIds.Manager,RoleIds.Sales))
            {
                return true;
            }

            return user.EmployeeId() == assignedEmployeeId;
        }

        public async Task<bool> CanAccessLead(ClaimsPrincipal user,int leadId)
        {           

            if (user.HasRole( RoleIds.Admin,RoleIds.Manager))
            {
                return true;
            }

            var assignedEmployeeId = await _repository.GetAssignedEmployeeId(leadId);

            if (assignedEmployeeId == null)
                return false;

            return user.EmployeeId() == assignedEmployeeId.Value;
        }

        public bool CanCreateOpportunity( ClaimsPrincipal user,int? assignedEmployeeId)
        {
            if (user.HasRole(
                RoleIds.Admin,
                RoleIds.Manager,
                RoleIds.Sales))
            {
                return true;
            }

            return user.EmployeeId() == assignedEmployeeId;
        }

        public async Task<bool> CanAccessOpportunity(ClaimsPrincipal user,int opportunityId)
        {
            if (user.HasRole(
                RoleIds.Admin,
                RoleIds.Manager))
            {
                return true;
            }

            var assignedEmployeeId = await _opportunityRepository.GetAssignedEmployeeId(opportunityId);

            if (assignedEmployeeId == null)
                return false;

            return user.EmployeeId() == assignedEmployeeId.Value;
        }


    }
}