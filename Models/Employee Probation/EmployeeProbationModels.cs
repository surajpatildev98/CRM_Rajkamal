namespace CRMWebServices.Models.Employee_Probation
{
    public class EmployeeProbationModels
    {
        public class EmployeeProbationManagementRequest
        {
            public int? EmployeeId { get; set; }

            public int? EmployeeProbationId { get; set; }

            public int? ProbationPeriodValue { get; set; }

            public string? ProbationPeriodUnit { get; set; }

            public string? ExtensionReason { get; set; }

            public DateTime? ConfirmationDate { get; set; }

            public string? ConfirmationRemark { get; set; }
            public string? FailureReason { get; set; }
            public string? CancellationReason { get; set; }
        }


        // ============================================================
        // EMPLOYEE LIST
        // ============================================================

        public class EmployeeProbationListItem
        {
            public int EmployeeId { get; set; }

            public string? EmployeeCode { get; set; }

            public string? EmployeeName { get; set; }

            public DateTime? DOJ { get; set; }

            public bool ProbationRequired { get; set; }
            public int? RoleId { get; set; }
            public string? RoleName { get; set; }
            public string? RoleCode { get; set; }
            public bool? IsPrimaryRole { get; set; }

            // Designation
            public int? DesignationId { get; set; }
            public string? DesignationCode { get; set; }
            public string? DesignationName { get; set; }


            public int? ProbationStatusId { get; set; }

            public string? StatusCode { get; set; }

            public string? StatusName { get; set; }

            public int? EmployeeProbationId { get; set; }

            public int? ProbationPeriodValue { get; set; }

            public string? ProbationPeriodUnit { get; set; }

            public DateTime? ProbationStartDate { get; set; }

            public DateTime? ProbationEndDate { get; set; }

            public bool? IsCurrent { get; set; }
        }


        // ============================================================
        // EMPLOYEE PROBATION DETAILS
        // ============================================================

        public class EmployeeProbationDetails
        {
            public int EmployeeId { get; set; }
            public string? EmployeeCode { get; set; }
            public string? EmployeeName { get; set; }

            public string? OfficialEmail { get; set; }
            public string? Mobile { get; set; }

            public DateTime? DOJ { get; set; }
            public DateTime? DOB { get; set; }

            // Designation
            public int? RefDesignationId { get; set; }
            public int? DesignationId { get; set; }
            public string? DesignationCode { get; set; }
            public string? DesignationName { get; set; }

            // Role
            public int? RoleId { get; set; }
            public string? RoleName { get; set; }
            public string? RoleCode { get; set; }
            public bool? IsPrimaryRole { get; set; }

            public int? RefReportingManagerId { get; set; }
            public int? RefReportingHRManagerId { get; set; }

            public string? EmploymentStatus { get; set; }

            public bool ProbationRequired { get; set; }

            public int? RefProbationStatusId { get; set; }

            public string? StatusCode { get; set; }
            public string? StatusName { get; set; }

            public DateTime? ConfirmationDate { get; set; }

            // Current probation
            public int? EmployeeProbationId { get; set; }
            public int? ProbationPeriodValue { get; set; }
            public string? ProbationPeriodUnit { get; set; }
            public DateTime? ProbationStartDate { get; set; }
            public DateTime? ProbationEndDate { get; set; }
            public int? ProbationStatusId { get; set; }
            public bool? IsCurrent { get; set; }
            public int? PreviousProbationId { get; set; }
            public string? ExtensionReason { get; set; }
            public DateTime? ProbationConfirmationDate { get; set; }
            public int? ConfirmedBy { get; set; }
            public string? ConfirmationRemark { get; set; }
        }


        // ============================================================
        // PROBATION HISTORY
        // ============================================================

        public class EmployeeProbationHistory
        {
            public int EmployeeProbationId { get; set; }

            public int EmployeeId { get; set; }

            public int ProbationPeriodValue { get; set; }

            public string? ProbationPeriodUnit { get; set; }

            public DateTime ProbationStartDate { get; set; }

            public DateTime ProbationEndDate { get; set; }

            public int ProbationStatusId { get; set; }

            public string? StatusCode { get; set; }

            public string? StatusName { get; set; }

            public bool IsCurrent { get; set; }

            public int? PreviousProbationId { get; set; }

            public string? ExtensionReason { get; set; }

            public string? FailureReason { get; set; }

            public string? CancellationReason { get; set; }

            public DateTime? ConfirmationDate { get; set; }

            public int? ConfirmedBy { get; set; }

            public string? ConfirmationRemark { get; set; }

            public DateTime? CreatedOn { get; set; }

            public int? CreatedBy { get; set; }

            public DateTime? UpdatedOn { get; set; }

            public int? UpdatedBy { get; set; }
        }


        // ============================================================
        // OPERATION RESPONSE
        // ============================================================

        public class EmployeeProbationOperationResponse
        {
            public int EmployeeProbationId { get; set; }

            public int EmployeeId { get; set; }

            public int? ProbationPeriodValue { get; set; }

            public string? ProbationPeriodUnit { get; set; }

            public DateTime? ProbationStartDate { get; set; }

            public DateTime? ProbationEndDate { get; set; }

            public int? ProbationStatusId { get; set; }

            public string? StatusCode { get; set; }

            public string? StatusName { get; set; }

            public bool IsCurrent { get; set; }

            public int? PreviousProbationId { get; set; }

            public string? ExtensionReason { get; set; }

            public DateTime? ConfirmationDate { get; set; }

            public int? ConfirmedBy { get; set; }

            public string? ConfirmationRemark { get; set; }
            public string? FailureReason { get; set; }
            public string? CancellationReason { get; set; }
        }
    }
}
