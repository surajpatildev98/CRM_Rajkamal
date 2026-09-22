using CRMWebServices.Helpers;
using System.Threading.Tasks;
using static CRMWebServices.Models.OpportunityModels;
using static CRMWebServices.Models.PermissionModels;

namespace CRMWebServices.Repository
{
    public interface IOpportunityRepository
    {

        Task<ApiResponse> GetWonLeads(string searchText = null, int? assignedEmployeeId = null,int? loggedInEmployeeId = null);
        Task<ApiResponse> GetWonLeadDetails(int leadId);          
        Task<ApiResponse> ConvertLeadToOpportunity(ConvertLeadRequest request);           
        Task<ApiResponse> GetOpportunityList(OpportunityListRequest request);     
        Task<ApiResponse> GetOpportunityDetails(int opportunityId, int loggedInEmployeeId);
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
        Task<ApiResponse> GetOpportunityTypes();
        Task<ApiResponse> GetOpportunityStages();
        Task<ApiResponse> GetPriorities();
        Task<ApiResponse> GetOpportunityCloseStages();
        Task<int?> GetAssignedEmployeeId(int leadId);
    }
}