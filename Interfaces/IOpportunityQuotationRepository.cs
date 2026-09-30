using System.Threading.Tasks;
using CRMWebServices.Helpers;
using static CRMWebServices.Models.OpportunityQuotationModels;

namespace CRMWebServices.Interfaces
{
    public interface IOpportunityQuotationRepository
    {
        Task<ApiResponse> GetRaiseQuotationData(int opportunityId, int employeeId);
        Task<ApiResponse> RaiseQuotation(RaiseQuotationRequest model);
        Task<ApiResponse> GetQuotationDetails(int opportunityQuotationId);
        Task<ApiResponse> LogCustomerResponse(LogCustomerResponseRequest model);
        Task<ApiResponse> LogNegotiationRound(LogNegotiationRoundRequest model);
        Task<ApiResponse> DecideManagementReview(DecideManagementReviewRequest model);
        Task<ApiResponse> GetQuotationList(QuotationListRequest request);
        Task<ApiResponse> ReviseQuotation(ReviseQuotationRequest model);
        Task<ApiResponse> CheckODA(int deliveryPartnerId, string pinCode);
        Task<ApiResponse> GetODAApprovalList(ODAApprovalListRequest request,int employeeId);
        Task<ApiResponse> GetODAApprovalDetails(long quotationId,int employeeId);
        Task<ApiResponse> DecideODAApproval(DecideODAApprovalRequest model);
    }
}
