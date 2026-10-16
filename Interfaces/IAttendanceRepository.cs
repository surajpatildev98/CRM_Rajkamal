using CRMWebServices.Models.Attendance;

namespace CRMWebServices.Interfaces
{
    public interface IAttendanceRepository
    {
        /*Task<AttendanceResponse> GetAttendance(AttendanceRequest request);
        Task<AttendancePunchResponse> GetPunches(AttendancePunchRequest request);
        Task<ManualPunchResponse> ManualPunch(ManualPunchRequest request);
        Task<AttendanceDashboardResponse> GetDashboardCount(AttendanceDashboardRequest request);*/

        Task<AttendanceResponse> GetAttendance(AttendanceRequest request, int loggedInEmployeeId, string viewScope);
        Task<AttendancePunchResponse> GetPunches(AttendancePunchRequest request, int loggedInEmployeeId, string viewScope);

        Task<ManualPunchResponse> ManualPunch(ManualPunchRequest request);
        Task<AttendanceDashboardResponse> GetDashboardCount(AttendanceDashboardRequest request);

        Task<AttendanceReportEmployeesResponse> GetReportEmployees(AttendanceReportEmployeesRequest request, int loggedInEmployeeId, string viewScope);

        Task<AttendanceEmployeeReportResponse> GetEmployeeReport(AttendanceEmployeeReportRequest request, int loggedInEmployeeId, string viewScope);
        
         // NEW : upgrades SELF -> TEAM when the employee has people reporting to him
        Task<string> ResolveViewScope(int employeeId, string roleScope);
    }
}
