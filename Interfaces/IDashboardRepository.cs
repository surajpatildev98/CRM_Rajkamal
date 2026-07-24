using CRMWebServices.Helpers;

namespace CRMWebServices.Interfaces
{
    public interface IDashboardRepository
    {
        //Task<ApiResponse> GetKpiCards(int? employeeId);
        //Task<ApiResponse> GetLeadFunnel(int? employeeId, DateTime? fromDate, DateTime? toDate);
        //Task<ApiResponse> GetOpportunityPipeline(int? employeeId);
        //Task<ApiResponse> GetPendingTasks(int? employeeId);
        //Task<ApiResponse> GetRecentActivity(int? employeeId);
        //Task<ApiResponse> GetEmployeePerformance();

        Task<ApiResponse> GetDashboardFull(int? employeeId, DateTime? fromDate, DateTime? toDate);
        Task<ApiResponse> GetEmployeePerformance();
    }
}
