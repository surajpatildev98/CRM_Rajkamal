using CRMWebServices.Helpers;

namespace CRMWebServices.Interfaces
{
    public interface IDashboardRepository
    {       
        Task<ApiResponse> GetDashboardFull(int? employeeId, DateTime? fromDate, DateTime? toDate);
        Task<ApiResponse> GetEmployeePerformance();
    }
}
