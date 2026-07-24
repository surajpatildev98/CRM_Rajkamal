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

            if (user.HasRole( RoleIds.Admin, RoleIds.Manager,RoleIds.PreSales))
            {
                return true;
            }

            return user.EmployeeId() == assignedEmployeeId;
        }

        //public async Task<bool> CanAccessLead(ClaimsPrincipal user,int leadId)
        //{           

        //    if (user.HasRole( RoleIds.Admin,RoleIds.Manager,RoleIds.PreSales))
        //    {
        //        return true;
        //    }

        //    var assignedEmployeeId = await _repository.GetAssignedEmployeeId(leadId);

        //    if (assignedEmployeeId == null)
        //        return false;

        //    return user.EmployeeId() == assignedEmployeeId.Value;
        //}
        public async Task<bool> CanAccessLead(ClaimsPrincipal user, int leadId)
        {
            if (user.HasRole(RoleIds.Admin, RoleIds.Manager))
                return true;

            var lead = await _repository.GetLeadAccessInfo(leadId);

            if (lead == null)
                return false;

            return lead.CreatedBy == user.EmployeeId()
                || lead.AssignedEmployeeId == user.EmployeeId();
        }

        public async Task<bool> CanModifyLead(ClaimsPrincipal user, int leadId)
        {
            if (user.HasRole(RoleIds.Admin, RoleIds.Manager))
                return true;

            var lead = await _repository.GetLeadAccessInfo(leadId);

            if (lead == null)
                return false;

            // Lead not assigned yet
            if (lead.AssignedEmployeeId == null)
                return lead.CreatedBy == user.EmployeeId();

            // Lead assigned
            return lead.AssignedEmployeeId == user.EmployeeId();
        }
        public bool CanCreateOpportunity( ClaimsPrincipal user,int? assignedEmployeeId)
        {
            if (user.HasRole(
                RoleIds.Admin,
                RoleIds.Manager))
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

        public async Task<bool> CanConvertOpportunity(ClaimsPrincipal user, int leadId)
        {
            if (user.HasRole(RoleIds.Admin, RoleIds.Manager))
                return true;

            var lead = await _repository.GetLeadAccessInfo(leadId);

            if (lead == null)
                return false;

            return lead.AssignedEmployeeId == user.EmployeeId();
        }

    }
}