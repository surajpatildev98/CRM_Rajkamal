namespace CRMWebServices.Models.Call_Log
{
    public class CallLogModels
    {
        public class CallLogCreateRequest
        {
            public int? Ref_LeadId { get; set; }
            public int? Ref_OpportunityId { get; set; }
            public int? Ref_LeadContactId { get; set; }

            public string? CallContext { get; set; }

            public string? ContactPersonName { get; set; }
            public string? ContactPersonNumber { get; set; }

            public string CallDirection { get; set; }
            public string CallStatus { get; set; }
            public string? CallOutcome { get; set; }

            public string? Remark { get; set; }

            public DateTime CallStartedOn { get; set; }
            public DateTime? CallEndedOn { get; set; }
            public int DurationInSeconds { get; set; }

            public string ExternalCallId { get; set; }

            public string? CallMatchStatus { get; set; }

            public int? CreatedBy { get; set; }
        }

        public class CallLogSummary
        {
            public int TotalCalls { get; set; }
            public int MissedCalls { get; set; }
            public int TotalTalkTimeInSeconds { get; set; }
        }

        public class CallLogInfo
        {
            public int LeadCallLogId { get; set; }

            public int? Ref_LeadId { get; set; }
            public string? LeadNo { get; set; }

            public int? Ref_OpportunityId { get; set; }
            public string? OpportunityNo { get; set; }

            public int? Ref_LeadContactId { get; set; }

            public string? CallContext { get; set; }

            public string? ContactPersonName { get; set; }
            public string? ContactPersonNumber { get; set; }

            public string? CallDirection { get; set; }
            public string? CallStatus { get; set; }
            public string? CallOutcome { get; set; }

            public string? Remark { get; set; }

            public DateTime CallStartedOn { get; set; }
            public DateTime? CallEndedOn { get; set; }

            public int DurationInSeconds { get; set; }

            public int CalledByEmployeeId { get; set; }
            public string? CalledByEmployeeName { get; set; }

            public string? ExternalCallId { get; set; }
            public string? CallMatchStatus { get; set; }

            public DateTime CreatedOn { get; set; }
        }

        public class CallLogResponse
        {
            public bool Status { get; set; }
            public string? Message { get; set; }
            public CallLogData? Data { get; set; }
        }

        public class CallLogData
        {
            public CallLogSummary Summary { get; set; } = new();
            public List<CallLogInfo> Calls { get; set; } = new();
        }
    }
}
