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
        Task<int?> GetAssignedEmployeeId(int leadId);
        Task<LeadAccessInfo?> GetLeadAccessInfo(int leadId);
        Task<ApiResponse> AssignLead(LeadAssignRequest model); 
        Task<ApiResponse> CheckCustomerExists(string? gst, string? customerName,string? pan);
        Task<ApiResponse> SearchCustomer(string searchText);
        Task<ApiResponse> AddActivityExpenses(LeadActivityExpenseRequest model);
        Task<ApiResponse> GetActivityExpenseMasters();
        Task<int?> GetEmployeeManagerIdAsync(int employeeId);
        Task<ApiResponse> AddLeadRequirement(LeadRequirementAddRequest model);
        Task<ApiResponse> UpdateLeadRequirement(LeadRequirementUpdateRequest model);
        Task<ApiResponse> DeleteLeadRequirement(LeadRequirementDeleteRequest model);
        Task<int?> GetLeadIdByRequirementId(int leadRequirementId);

    }
}
