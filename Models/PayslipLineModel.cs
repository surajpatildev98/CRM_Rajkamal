namespace CRMWebServices.Models
{

    public class PayslipLineModel
    {
        public int PayslipLineId { get; set; }
        public string LineType { get; set; } = string.Empty;   // "E" earning | "D" deduction
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class PayslipModel
    {
        public int PayslipId { get; set; }
        public int EmployeeId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int FinancialYear { get; set; }
        public decimal GrossEarnings { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetPay { get; set; }
        public DateTime? PaidOn { get; set; }

        /// Draft | Published (only filled for HR month list)
        public string? PayslipStatus { get; set; }

        // filled only by GetPayslip (detail)
        public List<PayslipLineModel> Earnings { get; set; } = new();
        public List<PayslipLineModel> Deductions { get; set; } = new();
        public bool HasBreakdown => Earnings.Count > 0 || Deductions.Count > 0;
    }

    // --------------------------- requests -------------------------

    public class PayslipDetailRequest
    {
        public int PayslipId { get; set; }
    }

    public class PayslipMonthRequest
    {
        public int PayMonth { get; set; }
        public int PayYear { get; set; }
        public DateTime? PaidOn { get; set; }     // used by publish only
    }

    public class PayslipLineInput
    {
        public string LineType { get; set; } = string.Empty;   // "E" | "D"
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int SortOrder { get; set; }
    }

    public class SavePayslipRequest
    {
        public int EmployeeId { get; set; }
        public int PayMonth { get; set; }
        public int PayYear { get; set; }
        public DateTime? PaidOn { get; set; }
        public List<PayslipLineInput> Lines { get; set; } = new();
    }

    // -------------------------- responses -------------------------

    public class PayslipListResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<PayslipModel> Data { get; set; } = new();
    }

    public class PayslipDetailResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public PayslipModel? Data { get; set; }
    }

    /// Save / publish / unpublish / delete result
    public class PayslipActionResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? PayslipId { get; set; }
        public int? Count { get; set; }
    }
}
