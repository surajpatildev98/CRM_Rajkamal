namespace CRMWebServices.Models
{
    public class LeaveBalanceModel
    {
        public int LeaveBalanceId { get; set; }
        public int EmployeeId { get; set; }
        public int LeaveTypeId { get; set; }
        public string? LeaveCode { get; set; }
        public string? LeaveName { get; set; }
        public bool? IsPaid { get; set; }
        public bool IsHourlyAllowed { get; set; }
        public decimal? OpeningBalance { get; set; }
        public decimal? Earned { get; set; }
        public decimal? Availed { get; set; }
        public decimal? Balance { get; set; }
        public decimal CarriedForward { get; set; }
        public decimal Adjusted { get; set; }
        public decimal Encashed { get; set; }
        public decimal Expired { get; set; }
        public int LeaveYearId { get; set; }
        public string? YearName { get; set; }
    }

    public class LeaveEntryModel
    {
        public int EmployeeLeaveId { get; set; }
        public int EmployeeId { get; set; }
        public int LeaveTypeId { get; set; }
        public string? LeaveCode { get; set; }
        public string? LeaveName { get; set; }
        public bool? IsPaid { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal? TotalDays { get; set; }
        public string? Reason { get; set; }
        public string? Status { get; set; }
        public string? LeaveApplicationNo { get; set; }
        public DateTime? AppliedOn { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedOn { get; set; }
        public string? Remarks { get; set; }
        public DateTime? RejectedOn { get; set; }
        public string? RejectedReason { get; set; }
        public int? RejectedBy { get; set; }
        public DateTime? CancelledOn { get; set; }
        public string? CancelledReason { get; set; }
        public int? CancelledBy { get; set; }
        public int LeaveYearId { get; set; }
        public string? YearName { get; set; }
    }

    public class LeaveResponse<T>
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    public class ApplyLeaveRequest
    {
        public int EmployeeId { get; set; }
        public int LeaveTypeId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalDays { get; set; }
        public string DayType { get; set; } = "FULL";
        public string Reason { get; set; } = string.Empty;
    }

    public class ApplyLeaveTypeModel
    {
        public int LeaveTypeId { get; set; }
        public string LeaveCode { get; set; }
        public string LeaveName { get; set; }
        public bool IsPaid { get; set; }
        public decimal Balance { get; set; }
        public decimal? MaxDaysPerApplication { get; set; }
        public int NoticeDays { get; set; }
        public bool IsHalfDayAllowed { get; set; }
        public bool IsHourlyAllowed { get; set; }
        public bool IncludeWeekends { get; set; }
        public bool IncludeHolidays { get; set; }
        public bool SandwichRule { get; set; }
        public bool RequiresDocument { get; set; }
    }

    public class CancelLeaveRequest
    {
        public int EmployeeId { get; set; }
        public int EmployeeLeaveId { get; set; }
        public string? Reason { get; set; }
    }

  
    public class TeamLeaveModel
    {
        public int EmployeeLeaveId { get; set; }

        public int EmployeeId { get; set; }

        public string? EmployeeCode { get; set; }

        public string? FirstName { get; set; }

        public string? MiddleName { get; set; }

        public string? LastName { get; set; }

        public string? EmployeeName { get; set; }

        public int LeaveTypeId { get; set; }

        public string? LeaveCode { get; set; }

        public string? LeaveName { get; set; }

        public bool? IsPaid { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public decimal TotalDays { get; set; }

        public string? Reason { get; set; }

        public string? Status { get; set; }

        public string? LeaveApplicationNo { get; set; }

        public DateTime? AppliedOn { get; set; }

        public int? ApprovedBy { get; set; }

        public DateTime? ApprovedOn { get; set; }

        public int? RejectedBy { get; set; }

        public DateTime? RejectedOn { get; set; }

        public string? RejectedReason { get; set; }

        public int? CancelledBy { get; set; }

        public DateTime? CancelledOn { get; set; }

        public string? CancelledReason { get; set; }

        public int LeaveYearId { get; set; }

        public string? YearName { get; set; }
    }

    public class ApproveLeaveRequest 
    { 
        public int EmployeeId { get; set; } 
        public int EmployeeLeaveId { get; set; }
    }
    
    public class RejectLeaveRequest
    {
        public int EmployeeId { get; set; }

        public int EmployeeLeaveId { get; set; }

        public string? Reason { get; set; }
    }

}
