namespace CRMWebServices.Models
{
    public class LeavePolicyModels
    {
     

        public class LeavePolicySaveRequest
        {
            public int? LeavePolicyId { get; set; }

            public string? PolicyName { get; set; }

            public int? Ref_CompanyId { get; set; }

            public bool IsActive { get; set; } = true;  

            public List<LeavePolicyDetailRequest> Details { get; set; }= new List<LeavePolicyDetailRequest>();
        }

        public class LeavePolicyUpdateRequest
        {
            public int LeavePolicyId { get; set; }
            public string? PolicyName { get; set; }
            public int? Ref_CompanyId { get; set; }
            public bool IsActive { get; set; } = true;

            public List<LeavePolicyDetailRequest> Details { get; set; } = new();
        }

        public class LeavePolicyDetailRequest
        {
            public int? LeavePolicyDetailId { get; set; }

            public int Ref_LeaveTypeId { get; set; }

            public decimal AnnualEntitlement { get; set; }

            public string? AccrualType { get; set; }

            public decimal AccrualValue { get; set; }

            public bool CarryForwardAllowed { get; set; }

            public decimal MaxCarryForward { get; set; }

            public bool EncashmentAllowed { get; set; }

            public decimal MaxEncashment { get; set; }

            public bool AllowNegativeBalance { get; set; }

            public decimal MaxDaysPerApplication { get; set; }

            public int NoticeDays { get; set; }

            public bool IncludeWeekends { get; set; }

            public bool IncludeHolidays { get; set; }

            public bool SandwichRule { get; set; }

            public bool IsHalfDayAllowed { get; set; }

            public bool RequiresDocument { get; set; }

            public bool IsActive { get; set; } = true;

            public bool IsHourlyAllowed { get; set; }
        }


        public class LeavePolicyListRequest
        {
            public string? SearchText { get; set; }

            public int? FilterCompanyId { get; set; }

            public bool? FilterIsActive { get; set; }
        }



        public class LeavePolicyListResponse
        {
            public int LeavePolicyId { get; set; }

            public string? PolicyName { get; set; }

            public int Ref_CompanyId { get; set; }

            public bool IsActive { get; set; }

            public int LeaveTypeCount { get; set; }

            public DateTime CreatedOn { get; set; }

            public int? CreatedBy { get; set; }

            public DateTime? UpdatedOn { get; set; }

            public int? UpdatedBy { get; set; }
        }


      

        public class LeavePolicyDetailsResponse
        {
            public LeavePolicyInfo? Policy { get; set; }

            public List<LeavePolicyDetailInfo> Details { get; set; }
                = new List<LeavePolicyDetailInfo>();
        }


      

        public class LeavePolicyInfo
        {
            public int LeavePolicyId { get; set; }

            public string? PolicyName { get; set; }

            public int Ref_CompanyId { get; set; }

            public bool IsActive { get; set; }

            public DateTime CreatedOn { get; set; }

            public int? CreatedBy { get; set; }

            public DateTime? UpdatedOn { get; set; }

            public int? UpdatedBy { get; set; }
        }


      

        public class LeavePolicyDetailInfo
        {
            public int LeavePolicyDetailId { get; set; }

            public int Ref_LeavePolicyId { get; set; }

            public int Ref_LeaveTypeId { get; set; }

            public string? LeaveCode { get; set; }

            public string? LeaveName { get; set; }

            public decimal AnnualEntitlement { get; set; }

            public string? AccrualType { get; set; }

            public decimal AccrualValue { get; set; }

            public bool CarryForwardAllowed { get; set; }

            public decimal MaxCarryForward { get; set; }

            public bool EncashmentAllowed { get; set; }

            public decimal MaxEncashment { get; set; }

            public bool AllowNegativeBalance { get; set; }

            public decimal MaxDaysPerApplication { get; set; }

            public int NoticeDays { get; set; }

            public bool IncludeWeekends { get; set; }

            public bool IncludeHolidays { get; set; }

            public bool SandwichRule { get; set; }

            public bool IsHalfDayAllowed { get; set; }

            public bool RequiresDocument { get; set; }

            public bool IsActive { get; set; }

            public bool IsHourlyAllowed { get; set; }

            public DateTime CreatedOn { get; set; }

            public int? CreatedBy { get; set; }

            public DateTime? UpdatedOn { get; set; }

            public int? UpdatedBy { get; set; }
        }



        public class LeaveTypeDropdown
        {
            public int LeaveTypeId { get; set; }

            public string? LeaveCode { get; set; }

            public string? LeaveName { get; set; }

            public bool IsPaid { get; set; }

            public bool IsActive { get; set; }

            public bool IsHourlyAllowed { get; set; }
        }
    }
}