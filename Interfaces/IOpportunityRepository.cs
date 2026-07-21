using CRMWebServices.Helpers;
using System.Threading.Tasks;
using static CRMWebServices.Models.OpportunityModels;

namespace CRMWebServices.Repository
{
    public interface IOpportunityRepository
    {
        // Won Leads (List + Single)
        Task<ApiResponse> GetWonLeads(string searchText = null);
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
    }
}