using CRMWebServices.Helpers;
using CRMWebServices.Models;

namespace CRMWebServices.Interfaces
{
    public interface ILeadRepository
    {
        Task<ApiResponse> CreateLead(LeadRegisterRequest model);
        Task<ApiResponse> TransferLead(LeadTransferRequest model);
        Task<ApiResponse> UpdateLeadStatus(LeadStatusUpdateRequest model);        
        Task<ApiResponse> CreateActivity(LeadActivityRequest model);
        Task<ApiResponse> CreateFollowUp(LeadFollowUpRequest model);

        Task<ApiResponse> GetLeadFullDetails(int leadId);
        Task<ApiResponse> GetLeadList(LeadListFilterRequest filter);

        Task<ApiResponse> UpdateFollowUpStatus(FollowUpStatusUpdateRequest model);

        Task<ApiResponse> CloseLead(LeadCloseRequest model);

    }
}
