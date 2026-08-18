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

        public List<AttendancePunchModel> Punches { get; set; }
    = new List<AttendancePunchModel>();
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

        public List<AttendancePunchModel> Data { get; set; }
            = new List<AttendancePunchModel>();
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

        public List<AttendanceModel> Data { get; set; }
            = new List<AttendanceModel>();
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
}
