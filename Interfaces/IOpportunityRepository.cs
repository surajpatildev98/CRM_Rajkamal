using CRMWebServices.Helpers;
using System.Threading.Tasks;
using static CRMWebServices.Models.OpportunityModels;
using static CRMWebServices.Models.PermissionModels;

namespace CRMWebServices.Repository
{
    public interface IOpportunityRepository
    {
        // Won Leads (List + Single)
        Task<ApiResponse> GetWonLeads(string searchText = null, int? assignedEmployeeId = null);

        //Task<ApiResponse> GetWonLeads(string? searchText, int? assignedEmployeeId);
        Task<ApiResponse> GetWonLeadDetails(int leadId);

        // Convert Lead
        Task<ApiResponse> ConvertLeadToOpportunity(ConvertLeadRequest request);

        Task<ApiResponse> TransferOpportunity(OpportunityTransferRequest model);

        // Opportunity CRUD
        Task<ApiResponse> GetOpportunityList(OpportunityListRequest request);
        Task<ApiResponse> GetOpportunityDetails(int opportunityId);
        Task<ApiResponse> UpdateOpportunity(UpdateOpportunityRequest request);
        Task<ApiResponse> DeleteOpportunity(DeleteOpportunityRequest request);

        // Dropdowns
        Task<ApiResponse> GetOpportunityTypes();
        Task<ApiResponse> GetOpportunityStages();
        Task<ApiResponse> GetPriorities();

        Task<int?> GetAssignedEmployeeId(int leadId);
      
        Task<OpportunityAccessInfo?> GetOpportunityAccessInfo(int opportunityId);

    }
}