using CRMWebServices.Helpers;
using System.Threading.Tasks;
using static CRMWebServices.Models.OpportunityModels;
using static CRMWebServices.Models.PermissionModels;

namespace CRMWebServices.Repository
{
    public interface IOpportunityRepository
    {
        #region // Get Leads Details
        Task<ApiResponse> GetWonLeads(string searchText = null, int? assignedEmployeeId = null);
        Task<ApiResponse> GetWonLeadDetails(int leadId);
        #endregion  

        #region // ConvertLeadToOpportunity used to convert a lead to an opportunity
        Task<ApiResponse> ConvertLeadToOpportunity(ConvertLeadRequest request);
        #endregion

        #region // Opportunity Management Tasks
        Task<ApiResponse> GetOpportunityList(OpportunityListRequest request);
        Task<ApiResponse> GetOpportunityDetails(int opportunityId);
        Task<ApiResponse> UpdateOpportunity(UpdateOpportunityRequest request);
        Task<ApiResponse> DeleteOpportunity(DeleteOpportunityRequest request);
        Task<ApiResponse> TransferOpportunity(OpportunityTransferRequest model);
        Task<OpportunityAccessInfo?> GetOpportunityAccessInfo(int opportunityId);
        Task<ApiResponse> AddOpportunityRequirement(AddOpportunityRequirementRequest model);
        Task<ApiResponse> UpdateOpportunityRequirement(UpdateOpportunityRequirementRequest model);
        Task<ApiResponse> CreateOpportunityActivity(CreateOpportunityActivityRequest model);
        Task<ApiResponse> CreateOpportunityFollowUp(CreateOpportunityFollowUpRequest model);
        Task<ApiResponse> CloseOpportunity(CloseOpportunityRequest model);
        Task<ApiResponse> UpdateOpportunityFollowUpStatus(UpdateOpportunityFollowUpStatusRequest model);
        Task<ApiResponse> DeleteOpportunityRequirement(DeleteOpportunityRequirementRequest model);
        Task<ApiResponse> AddOpportunityActivityExpenses(AddOpportunityActivityExpenseRequest model);
        #endregion

        #region // Opportunity Permissions
        Task<ApiResponse> GetOpportunityTypes();
        Task<ApiResponse> GetOpportunityStages();
        Task<ApiResponse> GetPriorities();
        Task<ApiResponse> GetOpportunityCloseStages();
        Task<int?> GetAssignedEmployeeId(int leadId);
        #endregion
    }
}