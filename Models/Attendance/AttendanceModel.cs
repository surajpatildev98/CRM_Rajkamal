namespace CRMWebServices.Models.Attendance
{
    public class AttendanceModel
    {
        public int AttendanceId { get; set; }
        public int EmployeeId { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public int? OfficeId { get; set; }
        public string? OfficeName { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public DateTime AttendanceDate { get; set; }
        public DateTime? FirstInTime { get; set; }
        public DateTime? LastOutTime { get; set; }
        public decimal? WorkingHours { get; set; }
        public decimal? OvertimeHours { get; set; }
        public int? LateMinutes { get; set; }
        public int? EarlyExitMinutes { get; set; }
        public decimal? BreakHours { get; set; }
        public int? Ref_AttendanceStatusId { get; set; }
        public int? Ref_ShiftId { get; set; }
        public string? ShiftName { get; set; }
        public string? PunchSource { get; set; }
        public string? Remarks { get; set; }
        public string? WorkMode { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public List<AttendancePunchModel> Punches { get; set; }= new List<AttendancePunchModel>();
    }

    public class AttendancePunchModel
    {
        public int AttendancePunchId { get; set; }
        public int AttendanceId { get; set; }
        public int EmployeeId { get; set; }
        public DateTime PunchDateTime { get; set; }
        public string? PunchType { get; set; }
        public string? PunchSource { get; set; }
        public string? WorkMode { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Remarks { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
    }

    public class AttendancePunchRequest
    {
        public int AttendanceId { get; set; }
    }

    public class AttendancePunchResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<AttendancePunchModel> Data { get; set; }= new List<AttendancePunchModel>();
    }

    public class AttendanceRequest
    {
        public DateTime? AttendanceDate { get; set; }
        public int? OfficeId { get; set; }
        public int? DepartmentId { get; set; }
        public string? Search { get; set; }
    }

    public class AttendanceResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<AttendanceModel> Data { get; set; }= new List<AttendanceModel>();
    }

    public class ManualPunchRequest
    {
        public int EmployeeId { get; set; }
        public string PunchType { get; set; } = string.Empty;
        public string WorkMode { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string? Remarks { get; set; }
        public int CreatedBy { get; set; }
    }

    public class ManualPunchResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? AttendanceId { get; set; }
        public string? PunchType { get; set; }
        public string? WorkMode { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public decimal? DistanceKm { get; set; }
    }

    public class AttendanceDashboardRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? EmployeeId { get; set; }
    }

    public class AttendanceTrendModel
    {
        public DateTime Date { get; set; }
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Late { get; set; }
        public int OnLeave { get; set; }
    }


    public class AttendanceDashboardModel
    {
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Late { get; set; }
        public int OnLeave { get; set; }
        public int TotalAttendance { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<AttendanceTrendModel> AttendanceTrend { get; set; } = new();
    }


    public class AttendanceDashboardResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public AttendanceDashboardModel? Data { get; set; }
    }

    public class AttendanceReportEmployeesRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? CompanyId { get; set; }
        public int? OfficeId { get; set; }
        public int? DepartmentId { get; set; }
        public string? Search { get; set; }
    }

    public class AttendanceEmployeeReportRequest
    {
        public int EmployeeId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class AttendanceReportEmployeeModel
    {
        public int EmployeeId { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public int? CompanyId { get; set; }
        public int? OfficeId { get; set; }
        public string? OfficeName { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public int? DesignationId { get; set; }
        public string? DesignationName { get; set; }
        public int? ReportingManagerId { get; set; }
        public int? ReportingHRManagerId { get; set; }

        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int HalfDays { get; set; }
        public int LeaveDays { get; set; }
        public int WFHDays { get; set; }
        public int LateDays { get; set; }
        public int HolidayDays { get; set; }
        public int WeekOffDays { get; set; }
        public int MarkedDays { get; set; }
        public decimal TotalWorkingHours { get; set; }
    }

    public class AttendanceReportEmployeesResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;

        /// ALL | MAPPED | TEAM | SELF  (the app uses it for the scope card)
        public string ViewScope { get; set; } = string.Empty;

        public List<AttendanceReportEmployeeModel> Data { get; set; } = new();
    }

    public class AttendanceEmployeeSummaryModel
    {
        public int EmployeeId { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public string? OfficeName { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }

        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int HalfDays { get; set; }
        public int LeaveDays { get; set; }
        public int WFHDays { get; set; }
        public int LateDays { get; set; }
        public int HolidayDays { get; set; }
        public int WeekOffDays { get; set; }

        public decimal TotalWorkingHours { get; set; }
        public decimal TotalOvertimeHours { get; set; }
    }

    public class AttendanceEmployeeDayModel
    {
        public DateTime AttendanceDate { get; set; }
        public int? AttendanceId { get; set; }
        public string StatusCode { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public bool IsPresent { get; set; }
        public bool IsLeave { get; set; }
        public bool IsLate { get; set; }
        public DateTime? FirstInTime { get; set; }
        public DateTime? LastOutTime { get; set; }
        public decimal? WorkingHours { get; set; }
        public decimal? BreakHours { get; set; }
        public decimal? OvertimeHours { get; set; }
        public int? LateMinutes { get; set; }
        public int? EarlyExitMinutes { get; set; }
        public string? WorkMode { get; set; }
        public int? ShiftId { get; set; }
        public string? ShiftName { get; set; }
        public string? Remarks { get; set; }
    }
    public class AttendanceEmployeeReportModel
    {
        public AttendanceEmployeeSummaryModel Summary { get; set; } = new();
        public List<AttendanceEmployeeDayModel> Days { get; set; } = new();
    }

    public class AttendanceEmployeeReportResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public AttendanceEmployeeReportModel? Data { get; set; }
    }
}
