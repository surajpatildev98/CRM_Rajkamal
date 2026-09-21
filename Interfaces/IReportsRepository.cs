using CRMWebServices.Helpers;
using CRMWebServices.Models;

namespace CRMWebServices.Interfaces
{
    public interface IReportsRepository
    {
        Task<ApiResponse> GetDashboard(ReportModels.CRMReportFilterRequest request);
        Task<ApiResponse> GetJourney(ReportModels.CRMReportJourneyRequest request);
        Task<ApiResponse> GetReportFilters();
    }
}
