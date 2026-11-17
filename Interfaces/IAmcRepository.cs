using CRMWebServices.Helpers;
using static CRMWebServices.Models.AmcModels;

namespace CRMWebServices.Interfaces
{
    public interface IAmcRepository
    {
        Task<ApiResponse> GetMasters();
        Task<ApiResponse> GetList(string? filter, string? amcType, string? search);
        Task<ApiResponse> GetCounts(string? amcType, string? search);
        Task<ApiResponse> GetDetails(int amcContractId);
        Task<ApiResponse> Save(AmcSaveRequest model, string status, int employeeId);   // CREATE | UPDATE | RENEW
        Task<ApiResponse> AttachInvoice(AmcInvoiceRequest model, int employeeId);
        Task<ApiResponse> Cancel(AmcCancelRequest model, int employeeId);
        Task<ApiResponse> Upload(AmcUploadRequest model, int employeeId);
        Task<ApiResponse> CheckCoverage(string? customerSource, int sourceCustomerId, string? serialNo, long? productId);
        Task LinkTicket(int ticketId);
        Task<ApiResponse> GetCoverageServices();
        Task<ApiResponse> SaveCoverageService(AmcCoverageSaveRequest model, int employeeId);
    }
}
