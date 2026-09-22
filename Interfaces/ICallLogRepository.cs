using CRMWebServices.Helpers;
using static CRMWebServices.Models.Call_Log.CallLogModels;

namespace CRMWebServices.Interfaces
{
    public interface ICallLogRepository
    {
        Task<ApiResponse> CreateCallLog(CallLogCreateRequest model);

        Task<ApiResponse> GetLeadCallLogs(int leadId);

        Task<ApiResponse> GetOpportunityCallLogs(int opportunityId);
    }
}
