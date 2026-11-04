namespace CRMWebServices.Models.Attendance
{
    public class RegularizationModel
    {
        public int RegularizationId { get; set; }
        public string? RequestNo { get; set; }

        public int EmployeeId { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }

        public DateTime AttendanceDate { get; set; }

        // MISSED_IN | MISSED_OUT | MISSED_BOTH | WRONG_PUNCH
        public string RegularizationType { get; set; } = string.Empty;

        // what was recorded when the request was raised
        public DateTime? CurrentInTime { get; set; }
        public DateTime? CurrentOutTime { get; set; }

        public DateTime? RequestedInTime { get; set; }
        public DateTime? RequestedOutTime { get; set; }

        public string? Reason { get; set; }

        // PENDING | APPROVED | REJECTED | CANCELLED
        public string Status { get; set; } = "PENDING";
        public DateTime? AppliedOn { get; set; }

        public int? ReviewedBy { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTime? ReviewedOn { get; set; }
        public string? ReviewerRemarks { get; set; }
    }

    public class ApplyRegularizationRequest
    {
        // ignored: the employee comes from the login token
        public int EmployeeId { get; set; }

        public DateTime? AttendanceDate { get; set; }
        public string? RegularizationType { get; set; }
        public DateTime? RequestedInTime { get; set; }
        public DateTime? RequestedOutTime { get; set; }
        public string? Reason { get; set; }
    }

    public class RegularizationActionRequest
    {
        // ignored: the employee comes from the login token
        public int EmployeeId { get; set; }

        public int RegularizationId { get; set; }

        // cancel / reject reason
        public string? Reason { get; set; }

        // approve remarks (optional)
        public string? Remarks { get; set; }
    }

    public class RegularizationListResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<RegularizationModel> Data { get; set; } = new();
    }

    public class RegularizationActionResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? RegularizationId { get; set; }
        public string? RequestNo { get; set; }
        public int? AttendanceId { get; set; }
    }
}
