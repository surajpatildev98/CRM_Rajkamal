namespace CRMWebServices.Models
{
    public class ReportModels
    {

        // DASHBOARD FILTER REQUEST


        public class CRMReportFilterRequest
        {
            public string? SearchText { get; set; }

            public DateTime? FromDate { get; set; }

            public DateTime? ToDate { get; set; }

            public string? DateType { get; set; }

            public int? EmployeeId { get; set; }

            public int? CompanyId { get; set; }

            public int? PriorityId { get; set; }

            public int? ProductId { get; set; }

            public int? LeadStatusId { get; set; }

            public int? LeadSourceId { get; set; }

            public int? OpportunityStageId { get; set; }

            public int? OpportunityTypeId { get; set; }

            public decimal? MinProbability { get; set; }

            public decimal? MaxProbability { get; set; }

            public DateTime? ExpectedCloseFromDate { get; set; }

            public DateTime? ExpectedCloseToDate { get; set; }

            public int? ActivityTypeId { get; set; }

            public int? ActivityStatusId { get; set; }

            public int? FollowUpStatusId { get; set; }

            public string? BusinessState { get; set; }

            public bool? IsActive { get; set; }
        }



        // JOURNEY REQUEST


        public class CRMReportJourneyRequest
        {
            public string? SearchText { get; set; }

            public int? LeadId { get; set; }

            public int? OpportunityId { get; set; }
        }



        // DASHBOARD RESPONSE


        public class CRMReportDashboardResponse
        {
            public CRMReportKpiResponse? Dashboard { get; set; }

            public List<CRMReportStagePipelineResponse> StagePipeline { get; set; }
                = new List<CRMReportStagePipelineResponse>();

            public List<CRMReportOpportunityResponse> Opportunities { get; set; }
                = new List<CRMReportOpportunityResponse>();

            public List<CRMReportConversionResponse> Conversions { get; set; }
                = new List<CRMReportConversionResponse>();
        }



        // RESULT SET 1
        // DASHBOARD KPI


        public class CRMReportKpiResponse
        {
            public int TotalLeads { get; set; }

            //public int ConvertedLeads { get; set; }
            public int QualifiedLeads { get; set; }

            public int TotalOpportunities { get; set; }

            public int TotalQuotations { get; set; }

            public int NegotiationOpportunities { get; set; }

            public int ManagementReviewOpportunities { get; set; }

            public int WonOpportunities { get; set; }

            public int LostOpportunities { get; set; }

            public decimal OpenPipelineValue { get; set; }

            public decimal WonValue { get; set; }

            public decimal AverageProbability { get; set; }

            public decimal LeadToOpportunityConversionPercent { get; set; }
        }



        // RESULT SET 2
        // STAGE PIPELINE


        public class CRMReportStagePipelineResponse
        {
            public int OpportunityStageId { get; set; }

            public string? StageCode { get; set; }

            public string? StageName { get; set; }

            public int StageOrder { get; set; }

            public bool IsClosed { get; set; }

            public bool IsWon { get; set; }

            public bool IsLost { get; set; }

            public int OpportunityCount { get; set; }

            public decimal ExpectedRevenue { get; set; }

            public decimal AverageProbability { get; set; }
        }



        // RESULT SET 3
        // OPPORTUNITY GRID


        public class CRMReportOpportunityResponse
        {
            public int OpportunityId { get; set; }

            public string? OpportunityNo { get; set; }

            public int Ref_LeadId { get; set; }

            public string? LeadNo { get; set; }

            public string? OpportunityName { get; set; }

            public int? Ref_LeadOrganizationId { get; set; }

            public string? CompanyName { get; set; }

            public int Ref_OpportunityTypeId { get; set; }

            public string? OpportunityTypeCode { get; set; }

            public string? OpportunityTypeName { get; set; }

            public int Ref_OpportunityStageId { get; set; }

            public string? StageCode { get; set; }

            public string? StageName { get; set; }

            public int StageOrder { get; set; }

            public int Ref_OwnerEmployeeId { get; set; }

            public string? OwnerName { get; set; }

            public decimal? ExpectedRevenue { get; set; }

            public decimal? Probability { get; set; }

            public DateTime? ExpectedCloseDate { get; set; }

            public int? Ref_PriorityId { get; set; }

            public string? CompetitorName { get; set; }

            public string? OpportunityRemark { get; set; }

            public DateTime CreatedOn { get; set; }

            public DateTime? UpdatedOn { get; set; }

            public int QuotationCount { get; set; }

            public int NegotiationRoundCount { get; set; }

            public DateTime? LastActivityDate { get; set; }

            public DateTime? NextFollowUpDate { get; set; }
        }



        // RESULT SET 4
        // LEAD -> OPPORTUNITY CONVERSION


        public class CRMReportConversionResponse
        {
            public int LeadId { get; set; }

            public string? LeadNo { get; set; }

            public DateTime? LeadCreatedOn { get; set; }

            public int? Ref_LeadStatusId { get; set; }

            public int OpportunityId { get; set; }

            public string? OpportunityNo { get; set; }

            public DateTime? OpportunityCreatedOn { get; set; }

            public decimal? ExpectedRevenue { get; set; }

            public string? StageCode { get; set; }

            public string? StageName { get; set; }

            public string? OpportunityOwner { get; set; }

            public DateTime? ConversionDate { get; set; }

            public string? AssignmentRemark { get; set; }
        }



        // JOURNEY RESPONSE


        public class CRMReportJourneyResponse
        {
            public CRMReportJourneyHeaderResponse? Header { get; set; }

            public CRMReportLeadResponse? Lead { get; set; }

            public List<CRMReportLeadActivityResponse> LeadActivities { get; set; }
                = new List<CRMReportLeadActivityResponse>();

            public List<CRMReportLeadFollowUpResponse> LeadFollowUps { get; set; }
                = new List<CRMReportLeadFollowUpResponse>();

            public List<CRMReportOpportunityDetailsResponse> Opportunities { get; set; }
                = new List<CRMReportOpportunityDetailsResponse>();

            //public List<CRMReportOpportunityRequirementResponse> Requirements { get; set; }
            //    = new List<CRMReportOpportunityRequirementResponse>();

            public List<CRMReportRequirementResponse> Requirements { get; set; }
    = new List<CRMReportRequirementResponse>();

            public List<CRMReportQuotationResponse> Quotations { get; set; }
                = new List<CRMReportQuotationResponse>();

            public List<CRMReportQuotationItemResponse> QuotationItems { get; set; }
                = new List<CRMReportQuotationItemResponse>();

            public List<CRMReportNegotiationResponse> Negotiations { get; set; }
                = new List<CRMReportNegotiationResponse>();

            public List<CRMReportManagementReviewResponse> ManagementReviews { get; set; }
                = new List<CRMReportManagementReviewResponse>();

            public List<CRMReportOpportunityActivityResponse> OpportunityActivities { get; set; }
                = new List<CRMReportOpportunityActivityResponse>();

            public List<CRMReportOpportunityFollowUpResponse> OpportunityFollowUps { get; set; }
                = new List<CRMReportOpportunityFollowUpResponse>();

            public List<CRMReportOpportunityStatusHistoryResponse> OpportunityStatusHistory { get; set; }
                = new List<CRMReportOpportunityStatusHistoryResponse>();

            public List<CRMReportOpportunityAssignmentResponse> OpportunityAssignments { get; set; }
                = new List<CRMReportOpportunityAssignmentResponse>();

            //public List<CRMReportActivityAttachmentResponse> ActivityAttachments { get; set; }
            //    = new List<CRMReportActivityAttachmentResponse>();

            public List<CRMReportActivityExpenseResponse> Expenses { get; set; } = new();

        }



        // JOURNEY RESULT SET 1
        // HEADER


        public class CRMReportJourneyHeaderResponse
        {
            public int LeadId { get; set; }

            public string? LeadNo { get; set; }

            public string? LeadTitle { get; set; }

            public string? CompanyName { get; set; }

            public DateTime? LeadCreatedOn { get; set; }

            public int? Ref_LeadStatusId { get; set; }

            public int? OpportunityId { get; set; }

            public string? OpportunityNo { get; set; }

            public string? OpportunityName { get; set; }

            public DateTime? OpportunityCreatedOn { get; set; }

            public decimal? ExpectedRevenue { get; set; }

            public DateTime? ExpectedCloseDate { get; set; }

            public decimal? Probability { get; set; }

            public string? StageCode { get; set; }

            public string? StageName { get; set; }

            public string? OpportunityOwnerName { get; set; }

            public DateTime? ConversionDate { get; set; }

            public string? ConversionRemark { get; set; }
        }



        // JOURNEY RESULT SET 2
        // LEAD DETAILS


        public class CRMReportLeadResponse
        {
            public int LeadId { get; set; }

            public string? LeadNo { get; set; }

            public string? LeadTitle { get; set; }

            public string? Requirement { get; set; }

            public string? LeadRemark { get; set; }

            public int Ref_LeadOrganizationId { get; set; }

            public string? CompanyName { get; set; }

            public int Ref_LeadSourceId { get; set; }

            public int? Ref_LeadStatusId { get; set; }

            public int? Ref_CurrentAssignedEmployeeId { get; set; }

            public string? LeadOwnerName { get; set; }

            public int? Ref_PriorityId { get; set; }

            public decimal? ExpectedValue { get; set; }

            public DateTime? ExpectedClosureDate { get; set; }

            public decimal? Probability { get; set; }

            public DateTime? CreatedOn { get; set; }

            public DateTime? UpdatedOn { get; set; }

            public DateTime? ClosedOn { get; set; }
        }



        // JOURNEY RESULT SET 3
        // LEAD ACTIVITIES


        public class CRMReportLeadActivityResponse
        {
            public long ActivityId { get; set; }

            public int Ref_LeadId { get; set; }

            public long? Ref_LeadAssignmentId { get; set; }

            public long? Ref_ActivityTypeId { get; set; }

            public long? Ref_EmployeeId { get; set; }

            public string? EmployeeName { get; set; }

            public DateTime ActivityDateTime { get; set; }

            public string? Subject { get; set; }

            public string? Description { get; set; }

            public string? Outcome { get; set; }

            public DateTime? NextFollowUpDate { get; set; }

            public int? DurationInMinutes { get; set; }

            public decimal? Latitude { get; set; }

            public decimal? Longitude { get; set; }

            public string? LocationAddress { get; set; }

            public int? Ref_ActivityStatusId { get; set; }

            public int? Ref_PreviousLeadStatusId { get; set; }

            public int? Ref_NewLeadStatusId { get; set; }

            public DateTime? CreatedOn { get; set; }

            public List<CRMReportActivityAttachmentResponse> Attachments { get; set; } = new();
        }



        // JOURNEY RESULT SET 4
        // LEAD FOLLOW UPS


        public class CRMReportLeadFollowUpResponse
        {
            public int FollowUpId { get; set; }

            public int Ref_LeadId { get; set; }

            public int? Ref_AssignedEmployeeId { get; set; }

            public string? AssignedEmployeeName { get; set; }

            public DateTime ScheduledOn { get; set; }

            public int? Priority { get; set; }

            public string? Remarks { get; set; }

            public bool IsCompleted { get; set; }

            public DateTime? CompletedOn { get; set; }

            public int? Ref_CompletedByEmployeeId { get; set; }

            public string? CompletedByEmployeeName { get; set; }

            public int? Ref_ActivityStatusId { get; set; }

            public int? Ref_ActivityId { get; set; }

            public DateTime? CreatedOn { get; set; }
        }



        // JOURNEY RESULT SET 5
        // OPPORTUNITY DETAILS


        public class CRMReportOpportunityDetailsResponse
        {
            public int OpportunityId { get; set; }

            public string? OpportunityNo { get; set; }

            public int Ref_LeadId { get; set; }

            public string? LeadNo { get; set; }

            public int? Ref_CustomerId { get; set; }

            public string? CompanyName { get; set; }

            public string? OpportunityName { get; set; }

            public int Ref_OpportunityTypeId { get; set; }

            public string? OpportunityTypeCode { get; set; }

            public string? OpportunityTypeName { get; set; }

            public int Ref_OpportunityStageId { get; set; }

            public string? StageCode { get; set; }

            public string? StageName { get; set; }

            public int StageOrder { get; set; }

            public decimal? ExpectedRevenue { get; set; }

            public DateTime? ExpectedCloseDate { get; set; }

            public decimal? Probability { get; set; }

            public int Ref_OwnerEmployeeId { get; set; }

            public string? OwnerName { get; set; }

            public int? Ref_PriorityId { get; set; }

            public string? CompetitorName { get; set; }

            public string? OpportunityRemark { get; set; }

            public DateTime? CreatedOn { get; set; }

            public int CreatedBy { get; set; }

            public DateTime? UpdatedOn { get; set; }

            public int? UpdatedBy { get; set; }
        }



        // JOURNEY RESULT SET 6
        // OPPORTUNITY REQUIREMENTS


        //public class CRMReportOpportunityRequirementResponse
        //{
        //    public int OpportunityRequirementId { get; set; }

        //    public int Ref_OpportunityId { get; set; }

        //    public int? Ref_LeadRequirementId { get; set; }

        //    public int Ref_ProductId { get; set; }

        //    public string? ProductCode { get; set; }

        //    public string? ProductName { get; set; }

        //    public int? Ref_ProductCategoryId { get; set; }

        //    public string? CategoryCode { get; set; }

        //    public string? CategoryName { get; set; }

        //    public decimal Quantity { get; set; }

        //    public string? RequirementRemark { get; set; }

        //    public int Ref_UnitId { get; set; }

        //    public bool IsActive { get; set; }

        //    public bool IsDeleted { get; set; }

        //    public DateTime? CreatedOn { get; set; }

        //    public long CreatedBy { get; set; }

        //    public DateTime? UpdatedOn { get; set; }

        //    public long? UpdatedBy { get; set; }
        //}

        //public class CRMReportRequirementResponse
        //{
        //    public string? RequirementType { get; set; }
        //    public int RequirementId { get; set; }
        //    public int LeadId { get; set; }
        //    public int? OpportunityId { get; set; }
        //    public int? Ref_LeadRequirementId { get; set; }
        //    public int? Ref_ProductId { get; set; }
        //    public string? ProductCode { get; set; }
        //    public string? ProductName { get; set; }
        //    public int? Ref_ProductCategoryId { get; set; }
        //    public string? CategoryCode { get; set; }
        //    public string? CategoryName { get; set; }
        //    public decimal? Quantity { get; set; }
        //    public string? RequirementRemark { get; set; }
        //    public int? Ref_UnitId { get; set; }
        //    public bool IsActive { get; set; }
        //    public bool IsDeleted { get; set; }
        //    public DateTime? CreatedOn { get; set; }
        //    public long? CreatedBy { get; set; }
        //    public DateTime? UpdatedOn { get; set; }
        //    public long? UpdatedBy { get; set; }
        //}

        public class CRMReportRequirementResponse
        {
            public string RequirementType { get; set; }
            public int RequirementId { get; set; }
            public int LeadId { get; set; }
            public int? OpportunityId { get; set; }
            public int? Ref_LeadRequirementId { get; set; }

            public long? Ref_ProductId { get; set; }
            public string ProductCode { get; set; }
            public string ProductName { get; set; }

            public int? Ref_ProductCategoryId { get; set; }
            public string CategoryName { get; set; }

            public int? OfferingId { get; set; }
            public string OfferingName { get; set; }

            public decimal? Quantity { get; set; }
            public string RequirementRemark { get; set; }
            public int? Ref_UnitId { get; set; }

            public bool IsActive { get; set; }
            public bool IsDeleted { get; set; }

            public DateTime? CreatedOn { get; set; }
            public long? CreatedBy { get; set; }
            public DateTime? UpdatedOn { get; set; }
            public long? UpdatedBy { get; set; }
        }
        // JOURNEY RESULT SET 7
        // QUOTATIONS


        public class CRMReportQuotationResponse
        {
            public int OpportunityQuotationId { get; set; }

            public int Ref_OpportunityId { get; set; }

            public string? QuotationNo { get; set; }

            public int VersionNumber { get; set; }

            public int RevisionNumber { get; set; }

            public string? RootQuotationNo { get; set; }

            public int? Ref_PreviousQuotationId { get; set; }

            public bool IsCurrent { get; set; }

            public DateTime QuotationDate { get; set; }

            public DateTime? ValidUntil { get; set; }

            public decimal SubTotal { get; set; }

            public decimal? FreightCharge { get; set; }

            public decimal? FreightPercent { get; set; }

            public decimal? TaxPercent { get; set; }

            public decimal? TaxAmount { get; set; }

            public decimal TotalAmount { get; set; }

            public string? TermsRemark { get; set; }

            public int? SentByEmployeeId { get; set; }

            public string? SentByEmployeeName { get; set; }

            public DateTime? SentOn { get; set; }

            public string? CustomerResponseCode { get; set; }

            public int? CustomerResponseByEmployeeId { get; set; }

            public string? CustomerResponseByEmployeeName { get; set; }

            public DateTime? CustomerResponseOn { get; set; }

            public string? CustomerResponseRemark { get; set; }

            public bool IsActive { get; set; }

            public DateTime CreatedOn { get; set; }

            public int? CreatedBy { get; set; }

            public DateTime? UpdatedOn { get; set; }

            public int? UpdatedBy { get; set; }

            public List<CRMReportQuotationAttachmentResponse> Attachments { get; set; } = new();
        }

        public class CRMReportQuotationAttachmentResponse
        {
            public int AttachmentId { get; set; }
            public int Ref_OpportunityQuotationId { get; set; }
            public string? FileName { get; set; }
            public string? FilePath { get; set; }
            public string? FileType { get; set; }
            public long? FileSize { get; set; }
            public DateTime? CreatedOn { get; set; }
            public int? CreatedBy { get; set; }
        }


        // JOURNEY RESULT SET 8
        // QUOTATION ITEMS


        public class CRMReportQuotationItemResponse
        {
            public int OpportunityQuotationItemId { get; set; }

            public int Ref_OpportunityQuotationId { get; set; }

            public int Ref_OpportunityRequirementId { get; set; }

            public decimal Quantity { get; set; }

            public decimal Rate { get; set; }

            public decimal Amount { get; set; }

            public int? Ref_ProductId { get; set; }

            public string? ProductCode { get; set; }

            public string? ProductName { get; set; }
        }



        // JOURNEY RESULT SET 9
        // NEGOTIATIONS


        public class CRMReportNegotiationResponse
        {
            public int OpportunityNegotiationId { get; set; }

            public int Ref_OpportunityId { get; set; }

            public int Ref_OpportunityQuotationId { get; set; }

            public int RoundNumber { get; set; }

            public decimal OurOfferAmount { get; set; }

            public decimal? CustomerCounterAmount { get; set; }

            public decimal? OfferDifference { get; set; }

            public string? Remarks { get; set; }

            public DateTime RoundDate { get; set; }

            public DateTime? NextFollowUpDate { get; set; }

            public string? StatusCode { get; set; }

            public bool IsActive { get; set; }

            public DateTime CreatedOn { get; set; }

            public int? CreatedBy { get; set; }

            public DateTime? UpdatedOn { get; set; }

            public int? UpdatedBy { get; set; }
        }



        // JOURNEY RESULT SET 10
        // MANAGEMENT REVIEWS


        public class CRMReportManagementReviewResponse
        {
            public int ManagementReviewId { get; set; }

            public int Ref_OpportunityId { get; set; }

            public int? Ref_OpportunityQuotationId { get; set; }

            public int? Ref_OpportunityNegotiationId { get; set; }

            public decimal? Margin { get; set; }

            public string? Remarks { get; set; }

            public string? DecisionCode { get; set; }

            public int? ReviewedByEmployeeId { get; set; }

            public string? ReviewedByEmployeeName { get; set; }

            public DateTime? ReviewedOn { get; set; }

            public bool IsActive { get; set; }

            public DateTime CreatedOn { get; set; }

            public int? CreatedBy { get; set; }
        }



        // JOURNEY RESULT SET 11
        // OPPORTUNITY ACTIVITIES


        public class CRMReportOpportunityActivityResponse
        {
            public long ActivityId { get; set; }

            public long Ref_OpportunityId { get; set; }

            public long? Ref_OpportunityAssignmentId { get; set; }

            public long Ref_ActivityTypeId { get; set; }

            public long Ref_EmployeeId { get; set; }

            public string? EmployeeName { get; set; }

            public DateTime ActivityDateTime { get; set; }

            public string? Subject { get; set; }

            public string? Description { get; set; }

            public string? Outcome { get; set; }

            public DateTime? NextFollowUpDate { get; set; }

            public int? DurationInMinutes { get; set; }

            public decimal? Latitude { get; set; }

            public decimal? Longitude { get; set; }

            public string? LocationAddress { get; set; }

            public long? Ref_ActivityStatusId { get; set; }

            public long Ref_OpportunityStageId { get; set; }

            public long? Ref_PreviousOpportunityStageId { get; set; }

            public long? Ref_NewOpportunityStageId { get; set; }

            public int? Ref_SourceLeadActivityId { get; set; }

            public DateTime CreatedOn { get; set; }

            public long CreatedBy { get; set; }

            public DateTime? UpdatedOn { get; set; }

            public long? UpdatedBy { get; set; }

            public List<CRMReportActivityAttachmentResponse> Attachments { get; set; } = new();
        }



        // JOURNEY RESULT SET 12
        // OPPORTUNITY FOLLOW UPS


        public class CRMReportOpportunityFollowUpResponse
        {
            public int OpportunityFollowUpId { get; set; }

            public int Ref_OpportunityId { get; set; }

            public int Ref_OpportunityActivityId { get; set; }

            public int Ref_AssignedEmployeeId { get; set; }

            public string? AssignedEmployeeName { get; set; }

            public DateTime ScheduledOn { get; set; }

            public int Priority { get; set; }

            public string? Remarks { get; set; }

            public bool IsCompleted { get; set; }

            public DateTime? CompletedOn { get; set; }

            public int? Ref_CompletedByEmployeeId { get; set; }

            public string? CompletedByEmployeeName { get; set; }

            public int Ref_ActivityStatusId { get; set; }

            public int? Ref_SourceLeadFollowUpId { get; set; }

            public DateTime CreatedOn { get; set; }

            public int CreatedBy { get; set; }

            public DateTime? UpdatedOn { get; set; }

            public int? UpdatedBy { get; set; }
        }



        // JOURNEY RESULT SET 13
        // OPPORTUNITY STATUS HISTORY


        public class CRMReportOpportunityStatusHistoryResponse
        {
            public int OpportunityStatusHistoryId { get; set; }

            public int Ref_OpportunityId { get; set; }

            public int Ref_OpportunityStageId { get; set; }

            public string? StageCode { get; set; }

            public string? StageName { get; set; }

            public int StageOrder { get; set; }

            public int? Ref_PreviousOpportunityStageId { get; set; }

            public string? PreviousStageCode { get; set; }

            public string? PreviousStageName { get; set; }

            public int? Ref_ChangedByEmployeeId { get; set; }

            public string? ChangedByEmployeeName { get; set; }

            public string? StatusRemark { get; set; }

            public DateTime ChangedOn { get; set; }

            public DateTime CreatedOn { get; set; }
        }



        // JOURNEY RESULT SET 14
        // OPPORTUNITY ASSIGNMENT / CONVERSION


        public class CRMReportOpportunityAssignmentResponse
        {
            public int OpportunityAssignmentId { get; set; }

            public int Ref_OpportunityId { get; set; }

            public int? Ref_FromEmployeeId { get; set; }

            public string? FromEmployeeName { get; set; }

            public int Ref_ToEmployeeId { get; set; }

            public string? ToEmployeeName { get; set; }

            public int Ref_AssignedByEmployeeId { get; set; }

            public string? AssignedByEmployeeName { get; set; }

            public string? AssignmentType { get; set; }

            public string? AssignmentRemark { get; set; }

            public DateTime AssignedOn { get; set; }

            public bool IsCurrent { get; set; }

            public bool IsActive { get; set; }

            public DateTime CreatedOn { get; set; }
        }



        // JOURNEY RESULT SET 15
        // OPPORTUNITY ACTIVITY ATTACHMENTS


        public class CRMReportActivityAttachmentResponse
        {
            public int AttachmentId { get; set; }
            public long? Ref_ActivityId { get; set; }
            public long? Ref_OpportunityActivityId { get; set; }
            public long? Ref_OpportunityId { get; set; }
            public string? FileName { get; set; }
            public string? FilePath { get; set; }
            public string? FileType { get; set; }
            public long? FileSize { get; set; }
            public long? CreatedBy { get; set; }
            public DateTime? CreatedOn { get; set; }
            public int? Ref_ActivityExpenseId { get; set; }
        }
        public class CRMReportFilterOption
        {
            public int Id { get; set; }

            public string? Code { get; set; }

            public string? Name { get; set; }
        }

        public class CRMReportActivityExpenseResponse
        {
            public int ActivityExpenseId { get; set; }
            public string? ExpenseType { get; set; }
            public int? Ref_LeadId { get; set; }
            public long? Ref_LeadActivityId { get; set; }
            public int? Ref_OpportunityId { get; set; }
            public long? Ref_OpportunityActivityId { get; set; }
            public int? Ref_ExpenseCategoryId { get; set; }
            public string? CategoryCode { get; set; }
            public string? CategoryName { get; set; }
            public int? Ref_TransportModeId { get; set; }
            public string? ModeCode { get; set; }
            public string? ModeName { get; set; }
            public DateTime ExpenseDate { get; set; }
            public decimal Amount { get; set; }
            public string? Description { get; set; }
            public string? FromLocation { get; set; }
            public string? ToLocation { get; set; }
            public DateTime? CreatedOn { get; set; }
            public int? CreatedBy { get; set; }
            public DateTime? UpdatedOn { get; set; }
            public int? UpdatedBy { get; set; }

            public List<CRMReportActivityAttachmentResponse> Attachments { get; set; } = new();
        }

        public class CRMReportFilterOptionsResponse
        {
            public List<CRMReportFilterOption> Employees { get; set; }
                = new();

            public List<CRMReportFilterOption> Companies { get; set; }
                = new();

            public List<CRMReportFilterOption> Priorities { get; set; }
                = new();

            public List<CRMReportFilterOption> Products { get; set; }
                = new();

            public List<CRMReportFilterOption> LeadStatuses { get; set; }
                = new();

            public List<CRMReportFilterOption> LeadSources { get; set; }
                = new();

            public List<CRMReportFilterOption> OpportunityStages { get; set; }
                = new();

            public List<CRMReportFilterOption> OpportunityTypes { get; set; }
                = new();

            public List<CRMReportFilterOption> ActivityTypes { get; set; }
                = new();

            public List<CRMReportFilterOption> ActivityStatuses { get; set; }
                = new();

            public List<CRMReportFilterOption> FollowUpStatuses { get; set; }
                = new();

            public List<string> DateTypes { get; set; }
                = new();

            public List<string> BusinessStates { get; set; }
                = new();
        }

        public static List<string> ReportDateTypes =>
    new()
    {
        "CREATED",
        "EXPECTED_CLOSE",
        "ACTIVITY",
        "FOLLOWUP",
        "ASSIGNMENT",
        "STATUS"
    };

        public static List<string> ReportBusinessStates =>
            new()
            {
        "ALL",
        "LEAD",
        "QUALIFIED",
        "OPPORTUNITY",
        "QUOTE",
        "NEGOTIATION",
        "REVIEW",
        "WON",
        "LOST"
            };
    }
}