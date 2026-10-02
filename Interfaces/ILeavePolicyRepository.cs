using CRMWebServices.Helpers;
using CRMWebServices.Models;

namespace CRMWebServices.Interfaces
{
    public interface ILeavePolicyRepository
    {
        Task<ApiResponse> SaveLeavePolicy(LeavePolicyModels.LeavePolicySaveRequest request,int createdBy);
        Task<ApiResponse> UpdateLeavePolicy(LeavePolicyModels.LeavePolicyUpdateRequest request,int updatedBy);
        Task<ApiResponse> GetLeavePolicyList(LeavePolicyModels.LeavePolicyListRequest request);
        Task<ApiResponse> GetLeavePolicyDetails(int leavePolicyId);
        Task<ApiResponse> GetLeaveTypes();
    }
}
