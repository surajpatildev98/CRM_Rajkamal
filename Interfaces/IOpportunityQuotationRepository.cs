using System.Threading.Tasks;
using CRMWebServices.Helpers;
using static CRMWebServices.Models.OpportunityQuotationModels;

namespace CRMWebServices.Interfaces
{
    public interface IOpportunityQuotationRepository
    {
        Task<ApiResponse> GetRaiseQuotationData(int opportunityId);
        Task<ApiResponse> RaiseQuotation(RaiseQuotationRequest model);
        Task<ApiResponse> GetQuotationDetails(int opportunityQuotationId);
        Task<ApiResponse> LogCustomerResponse(LogCustomerResponseRequest model);
        Task<ApiResponse> LogNegotiationRound(LogNegotiationRoundRequest model);
        Task<ApiResponse> DecideManagementReview(DecideManagementReviewRequest model);
        Task<ApiResponse> GetQuotationList(QuotationListRequest request);
        Task<ApiResponse> ReviseQuotation(ReviseQuotationRequest model);
        Task<ApiResponse> CheckODA(int deliveryPartnerId, string pinCode);
    }
}
