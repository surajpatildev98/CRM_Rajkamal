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

        // ---------- regularization (employee request -> manager approval) ----------
        Task<RegularizationListResponse> GetMyRegularizations(int loggedInEmployeeId);
        Task<RegularizationListResponse> GetTeamRegularizations(int loggedInEmployeeId);
        Task<RegularizationActionResponse> ApplyRegularization(ApplyRegularizationRequest request, int loggedInEmployeeId);
        Task<RegularizationActionResponse> CancelRegularization(RegularizationActionRequest request, int loggedInEmployeeId);
        Task<RegularizationActionResponse> ApproveRegularization(RegularizationActionRequest request, int loggedInEmployeeId);
        Task<RegularizationActionResponse> RejectRegularization(RegularizationActionRequest request, int loggedInEmployeeId);
    }
}
