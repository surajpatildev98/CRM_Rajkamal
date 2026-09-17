using CRMWebServices.Helpers;
using static CRMWebServices.Models.FreightPolicyModels;

namespace CRMWebServices.Interfaces
{
    public interface IFreightPolicyRepository
    {
        Task<ApiResponse> CreateFreightPolicy(CreateFreightPolicyRequest model,int employeeId);
        Task<ApiResponse> UpdateFreightPolicy(UpdateFreightPolicyRequest model, int employeeId);
        Task<ApiResponse> DeactivateFreightPolicy(int freightPolicyId, int employeeId);
        Task<ApiResponse> GetFreightPolicyList();
        Task<ApiResponse> GetFreightPolicyDetails(int freightPolicyId);
        Task<ApiResponse> GetApplicableFreight(decimal orderValue);
        Task<ApiResponse> GetActiveFreightPolicy();
    }
}
