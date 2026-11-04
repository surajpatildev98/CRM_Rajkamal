using CRMWebServices.Helpers;

namespace CRMWebServices.Interfaces
{
    public interface IDashboardRepository
    {       
        Task<ApiResponse> GetDashboardFull(int? employeeId, DateTime? fromDate, DateTime? toDate, int loggedInEmployeeId);
        Task<ApiResponse> GetEmployeePerformance();
        Task<ApiResponse> GetHrmsDashboardFull(int employeeId, bool isAdmin);
    }
}
