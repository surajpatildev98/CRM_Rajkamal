using CRMWebServices.Models.Attendance;

namespace CRMWebServices.Interfaces
{
    public interface IAttendanceRepository
    {
        Task<AttendanceResponse> GetAttendance(AttendanceRequest request);
        Task<AttendancePunchResponse> GetPunches(AttendancePunchRequest request);
        Task<ManualPunchResponse> ManualPunch(ManualPunchRequest request);
        Task<AttendanceDashboardResponse> GetDashboardCount(AttendanceDashboardRequest request);

        Task<AttendanceReportEmployeesResponse> GetReportEmployees(AttendanceReportEmployeesRequest request, int loggedInEmployeeId, string viewScope);

        Task<AttendanceEmployeeReportResponse> GetEmployeeReport(AttendanceEmployeeReportRequest request, int loggedInEmployeeId, string viewScope);
    }
}
