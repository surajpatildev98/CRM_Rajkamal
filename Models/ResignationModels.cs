using System.ComponentModel.DataAnnotations;

namespace ResignationApi.Models;

/// <summary>Envelope used by the other APIs: { status, message, data }.</summary>
public class ApiResponse<T>
{
    public bool Status { get; set; }
    public string Message { get; set; } = "";
    public T? Data { get; set; }
}

public class ReasonDto
{
    public int ReasonId { get; set; }
    public string ReasonName { get; set; } = "";
}

public class NoticePolicyDto
{
    public int NoticePeriodDays { get; set; }
    public bool IsOnProbation { get; set; }
}

public class TimelineDto
{
    public string Title { get; set; } = "";
    public string? Remarks { get; set; }
    public string? ActionBy { get; set; }
    public DateTime Date { get; set; }
}

public class ClearanceItemDto
{
    public int ClearanceItemId { get; set; }
    public string Department { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string Status { get; set; } = "PENDING";   // PENDING | CLEARED | NA
    public string? Remarks { get; set; }
    public string? ClearedBy { get; set; }
    public DateTime? ClearedOn { get; set; }
}

/// <summary>Same field names as the Flutter ResignationModel.fromJson.</summary>
public class ResignationDto
{
    public int ResignationId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public string? EmployeeCode { get; set; }
    public string? DepartmentName { get; set; }
    public string? DesignationName { get; set; }
    public int ReasonId { get; set; }
    public string ReasonName { get; set; } = "";
    public string ReasonDetails { get; set; } = "";
    public string? HandoverNotes { get; set; }
    public DateTime ResignationDate { get; set; }
    public int NoticePeriodDays { get; set; }
    public DateTime RequestedLastWorkingDate { get; set; }
    public DateTime? ApprovedLastWorkingDate { get; set; }
    public string Status { get; set; } = "PENDING";
    public string? ApproverRemarks { get; set; }
    public int CurrentApprovalLevel { get; set; }
    public List<TimelineDto> Timeline { get; set; } = new();
    public List<ClearanceItemDto> Clearance { get; set; } = new();
}

/* ---------------- Requests (employeeId is NEVER taken from the body) ---------------- */

public class ApplyResignationRequest
{
    public int EmployeeId { get; set; }

    public int ReasonId { get; set; }

    public string ReasonDetails { get; set; } = "";

    public string? HandoverNotes { get; set; }

    public DateTime RequestedLastWorkingDate { get; set; }
}

public class WithdrawRequest
{
    public int EmployeeId { get; set; }

    public int ResignationId { get; set; }

    public string? Reason { get; set; }
}

public class ApproveRequest
{
    public int EmployeeId { get; set; }

    public int ResignationId { get; set; }

    public DateTime ApprovedLastWorkingDate { get; set; }

    public string? Remarks { get; set; }
}

public class RejectRequest
{
    public int EmployeeId { get; set; }

    public int ResignationId { get; set; }

    public string Reason { get; set; } = "";
}

public class UpdateClearanceRequest
{
    public int EmployeeId { get; set; }

    public int ResignationId { get; set; }

    public int ClearanceItemId { get; set; }

    public string Status { get; set; } = "";

    public string? Remarks { get; set; }
}
