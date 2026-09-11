namespace CRMWebServices.Models
{
    public class OpportunityModels
    {

        public class WonLeadsRequest
        {
            public string? SearchText { get; set; }
        }        
        public class WonLeadDetailsRequest
        {
            public int LeadId { get; set; }
        }       
        public class ConvertLeadRequest
        {
            public int Ref_LeadId { get; set; }
            public string? OpportunityName { get; set; }
            public decimal ExpectedRevenue { get; set; }
            public DateTime ExpectedCloseDate { get; set; }
            public int Probability { get; set; }
            public int Ref_PriorityId { get; set; }
            public string? OpportunityRemark { get; set; }
            public int? CreatedBy { get; set; }
            public int? AssignedToEmployeeId { get; set; }

            // Hidden/Auto fields (sent by Android but not in form)
            public int? Ref_OpportunityTypeId { get; set; } = 1; // Default: NEW

            //public int Ref_OpportunityStageId { get; set; } = 1; // Default: QUAL
        }
        public class OpportunityListRequest
        {
            public string? SearchText { get; set; }
            public int? FilterStageId { get; set; }
            public int? FilterOwnerId { get; set; }
            public int? FilterTypeId { get; set; }

            public int? FilterAssignedToId { get; set; }   // NEW

            public int? LoggedInEmployeeId { get; set; }
            public bool ShowCreatedAndAssigned { get; set; }  // For PreSales
        }       
        public class UpdateOpportunityRequest
        {
            public int Ref_OpportunityId { get; set; }
            public string? OpportunityName { get; set; }
            public int? Ref_OpportunityTypeId { get; set; }
            public int? Ref_OpportunityStageId { get; set; }
            public decimal? ExpectedRevenue { get; set; }
            public DateTime? ExpectedCloseDate { get; set; }
            public int? Probability { get; set; }
            public int? Ref_PriorityId { get; set; }
            public string? OpportunityRemark { get; set; }
            public int? CreatedBy { get; set; }
        }      
        public class DeleteOpportunityRequest
        {
            public int Ref_OpportunityId { get; set; }
            public int? CreatedBy { get; set; }
        }       
        public class WonLeadResponse
        {
            public int LeadId { get; set; }
            public string LeadNo { get; set; }
            public string LeadTitle { get; set; }
            public string CompanyName { get; set; }
            public decimal? ExpectedValue { get; set; }
            public DateTime CreatedOn { get; set; }
            public int? AssignedToEmployeeId { get; set; }
            public string AssignedToEmployee { get; set; }
            public string LeadStatus { get; set; }
            public bool IsConverted { get; set; }
        }       
        public class WonLeadDetailResponse
        {
            // Lead Info (READ-ONLY)
            public LeadInfo Lead { get; set; }
            public List<LeadContactInfo> Contacts { get; set; }
            public List<LeadRequirementInfo> Requirements { get; set; }
            public List<LeadAttachmentInfo> Attachments { get; set; }
            public List<LeadActivityInfo> Activities { get; set; }
        }
        public class LeadInfo
        {
            public int LeadId { get; set; }
            public string LeadNo { get; set; }
            public string LeadTitle { get; set; }
            public decimal? ExpectedValue { get; set; }
            public string LeadRemark { get; set; }
            public DateTime CreatedOn { get; set; }
            public int? AssignedToEmployeeId { get; set; }
            public string AssignedToEmployee { get; set; }
            public string LeadStatus { get; set; }
            public string LeadSource { get; set; }
            public bool IsConverted { get; set; }

            // Organization (will become Customer)
            public LeadOrganizationInfo Organization { get; set; }
        }
        public class LeadOrganizationInfo
        {
            public int LeadOrganizationId { get; set; }
            public string CompanyName { get; set; }
            public string Address { get; set; }
            public string CountryName { get; set; }
            public string StateName { get; set; }
            public string CityName { get; set; }
            public string PinCode { get; set; }
            public string GstNo { get; set; }
            public string PanNo { get; set; }
        }
        public class LeadContactInfo
        {
            public int LeadContactId { get; set; }
            public string ContactPerson { get; set; }
            public string DesignationName { get; set; }
            public string Mobile { get; set; }
            public string Email { get; set; }
        }
        public class LeadRequirementInfo
        {
            public int LeadRequirementId { get; set; }
            public string OfferingName { get; set; }
            public string RequirementRemark { get; set; }
            public int? Quantity { get; set; }
            public string Unit { get; set; }
        }
        public class LeadAttachmentInfo
        {
            public int AttachmentId { get; set; }
            public string FileName { get; set; }
            public string FilePath { get; set; }
            public string FileType { get; set; }
            public long? FileSize { get; set; }
            public DateTime CreatedOn { get; set; }
        }
        public class LeadActivityInfo
        {
            public int ActivityId { get; set; }
            public string ActivityType { get; set; }
            public DateTime ActivityDate { get; set; }
            public string Subject { get; set; }
            public string Description { get; set; }
            public string Outcome { get; set; }
            public string CreatedBy { get; set; }
            public string ActivityStatus { get; set; }
            public string ActivityStatusColor { get; set; }
        }       
        public class ConvertLeadResponse
        {
            public int OpportunityId { get; set; }
            public int? CustomerId { get; set; }
            public string OpportunityNo { get; set; }
        }        
        public class OpportunityListResponse
        {
            public int OpportunityId { get; set; }
            public string OpportunityNo { get; set; }
            public string OpportunityName { get; set; }
            public int? Ref_LeadId { get; set; }
            public string CompanyName { get; set; }
            public string OpportunityTypeName { get; set; }
            public string OpportunityStage { get; set; }
            public bool IsClosed { get; set; }
            public bool IsWon { get; set; }
            public bool IsLost { get; set; }
            public decimal? ExpectedRevenue { get; set; }
            public DateTime? ExpectedCloseDate { get; set; }
            public int? Probability { get; set; }
            public string PriorityName { get; set; }
            //public string PriorityColor { get; set; }
            public int? OwnerEmployeeId { get; set; }
            public string OwnerEmployee { get; set; }
            public DateTime CreatedOn { get; set; }
        }   
        public class OpportunityDetailResponse
        {
            public OpportunityInfo Opportunity { get; set; }

            public List<CustomerContactInfo> Contacts { get; set; }

            public List<OpportunityRequirementInfo> Requirements { get; set; }

            public List<OpportunityActivityInfo> Activities { get; set; }

            public List<OpportunityAttachmentInfo> Files { get; set; }

            public List<OpportunityFollowUpInfo> FollowUps { get; set; }

            public List<object> Timeline { get; set; }


        }
        public class OpportunityInfo
        {
            public int OpportunityId { get; set; }
            public string OpportunityNo { get; set; }
            public int? Ref_LeadId { get; set; }
            public string OpportunityName { get; set; }
            public decimal? ExpectedRevenue { get; set; }
            public DateTime? ExpectedCloseDate { get; set; }
            public int? Probability { get; set; }
            public string CompetitorName { get; set; }
            public string OpportunityRemark { get; set; }
            public DateTime CreatedOn { get; set; }

            public CustomerInfo Customer { get; set; }
            public OpportunityTypeInfo OpportunityType { get; set; }
            public OpportunityStageInfo OpportunityStage { get; set; }
            public PriorityInfo Priority { get; set; }
            public OwnerInfo Owner { get; set; }
        }
        public class CustomerInfo
        {
            public int CustomerId { get; set; }
            public string CompanyName { get; set; }
            public string Address { get; set; }
            public string CountryName { get; set; }
            public string StateName { get; set; }
            public string CityName { get; set; }
            public string PinCode { get; set; }
            public string GstNo { get; set; }
            public string PanNo { get; set; }
        }
        public class OpportunityTypeInfo
        {
            public int? OpportunityTypeId { get; set; }
            public string OpportunityTypeName { get; set; }
        }
        public class OpportunityTransferRequest
        {
            public int Ref_OpportunityId { get; set; }
            public int AssignedToEmployeeId { get; set; }
            public string AssignmentRemark { get; set; }
            public int? CreatedBy { get; set; }
        }
        public class OpportunityStageInfo
        {
            public int? OpportunityStageId { get; set; }
            public string OpportunityStage { get; set; }
            public bool IsClosed { get; set; }
            public bool IsWon { get; set; }
            public bool IsLost { get; set; }
        }
        public class PriorityInfo
        {
            public int? PriorityId { get; set; }
            public string PriorityName { get; set; }
            public int? PriorityLevel { get; set; }
            //public string ColorCode { get; set; }
        }
        public class OwnerInfo
        {
            public int? OwnerEmployeeId { get; set; }
            public string OwnerEmployeeName { get; set; }
            public string OwnerEmail { get; set; }
            public string OwnerMobileNo { get; set; }
            public int? OwnerRoleId { get; set; }
            public string OwnerRoleName { get; set; }
        }
        public class CustomerContactInfo
        {
            public int CustomerContactId { get; set; }
            public string ContactPerson { get; set; }
            public string DesignationName { get; set; }
            public string Mobile { get; set; }
            public string Email { get; set; }
            public bool IsPrimary { get; set; }
        }       
        public class OpportunityTypeDropdown
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
        public class OpportunityStageDropdown
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool IsClosed { get; set; }
            public bool IsWon { get; set; }
            public bool IsLost { get; set; }
        }
        public class PriorityDropdown
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int PriorityLevel { get; set; }
            //public string ColorCode { get; set; }
        }
        //public class OpportunityRequirementInfo
        //{
        //    public int OpportunityRequirementId { get; set; }

        //    public int? LeadRequirementId { get; set; }

        //    public int OfferingId { get; set; }

        //    public string OfferingName { get; set; }

        //    public decimal Quantity { get; set; }

        //    public string Remark { get; set; }

        //    public string Unit { get; set; }

        //    public bool IsOpportunityRequirement { get; set; }
        //}

        public class OpportunityRequirementInfo
        {
            public int OpportunityRequirementId { get; set; }

            public int? LeadRequirementId { get; set; }

            public int? CategoryId { get; set; }
            public string CategoryName { get; set; }

            public int? OfferingId { get; set; }
            public string OfferingName { get; set; }

            public long? ProductId { get; set; }
            public string ProductName { get; set; }

            public decimal Quantity { get; set; }

            public string Remark { get; set; }

            public string Unit { get; set; }

            public bool IsOpportunityRequirement { get; set; }
        }
        public class OpportunityActivityInfo
        {
            public int ActivityId { get; set; }

            public string ActivityType { get; set; }

            public DateTime ActivityDate { get; set; }

            public string Subject { get; set; }

            public string Description { get; set; }

            public string Outcome { get; set; }

            public string? LocationAddress { get; set; }

            public string CreatedBy { get; set; }

            public int? ActivityStatusId { get; set; }

            public string ActivityStatus { get; set; }

            public string ActivityStatusColor { get; set; }

            public List<object>? Attachments { get; set; }   // NEW

            public List<object>? Expenses { get; set; }
        }


        public class OpportunityAttachmentInfo
        {
            public int AttachmentId { get; set; }

            public string FileName { get; set; }

            public string FileType { get; set; }

            public long? FileSize { get; set; }

            public string FileUrl { get; set; }

            public DateTime UploadedOn { get; set; }
        }
        public class OpportunityFollowUpInfo
        {
            public int FollowUpId { get; set; }

            public int? ActivityId { get; set; }

            public int? AssignedEmployeeId { get; set; }

            public string AssignedTo { get; set; }

            public DateTime FollowUpDate { get; set; }

            public int Priority { get; set; }

            public string Remarks { get; set; }

            public bool IsCompleted { get; set; }

            public DateTime? CompletedOn { get; set; }

            public string FollowUpStatus { get; set; }

            public string FollowUpStatusColor { get; set; }

            public DateTime CreatedOn { get; set; }

            public string CreatedByName { get; set; }
        }
        //public class AddOpportunityRequirementRequest
        //{
        //    public int Ref_OpportunityId { get; set; }
        //    public int Ref_ProductId { get; set; }
        //    public decimal Quantity { get; set; }
        //    public string? RequirementRemark { get; set; }
        //    public int? Ref_UnitId { get; set; }
        //    public int CreatedBy { get; set; }
        //}
        public class AddOpportunityRequirementRequest
        {
            public int Ref_OpportunityId { get; set; }
            public int Ref_ProductCategoryId { get; set; }
            public int Ref_OfferingId { get; set; }
            public long Ref_ProductId { get; set; }
            public decimal Quantity { get; set; }
            public string? RequirementRemark { get; set; }
            public int? Ref_UnitId { get; set; }
            public int CreatedBy { get; set; }
        }
        //public class UpdateOpportunityRequirementRequest
        //{
        //    public int Ref_OpportunityId { get; set; }
        //    public int? Ref_OpportunityRequirementId { get; set; }  // set if already has an Opportunity row
        //    public int? Ref_LeadRequirementId { get; set; }         // set if overriding a Lead item for the first time

        //    public int? Ref_ProductId { get; set; }
        //    public decimal Quantity { get; set; }
        //    public string? RequirementRemark { get; set; }
        //    public int? Ref_UnitId { get; set; }
        //    public int CreatedBy { get; set; }
        //}

        public class UpdateOpportunityRequirementRequest
        {
            public int Ref_OpportunityId { get; set; }
            public int? Ref_OpportunityRequirementId { get; set; }
            public int? Ref_LeadRequirementId { get; set; }

            public int? Ref_ProductCategoryId { get; set; }
            public int? Ref_OfferingId { get; set; }
            public long? Ref_ProductId { get; set; }

            public decimal Quantity { get; set; }
            public string? RequirementRemark { get; set; }
            public int? Ref_UnitId { get; set; }
            public int CreatedBy { get; set; }
        }
        public class CreateOpportunityActivityRequest
        {
            public int Ref_OpportunityId { get; set; }
            public int Ref_ActivityTypeId { get; set; }
            public string Subject { get; set; } = null!;
            public string Description { get; set; } = null!;
            public string? Outcome { get; set; }
            public DateTime ActivityDateTime { get; set; }
            public int? DurationInMinutes { get; set; }
            public decimal? Latitude { get; set; }
            public decimal? Longitude { get; set; }

            public string? LocationAddress { get; set; }
            public int? Ref_ActivityStatusId { get; set; }
            public int? Ref_NewOpportunityStageId { get; set; }
            public DateTime? NextFollowUpDate { get; set; }
            public List<AttachmentInput>? Attachments { get; set; }
            public int CreatedBy { get; set; }
        }
        public class AttachmentInput
        {
            public string FileName { get; set; } = null!;
            public string Base64Content { get; set; } = null!;
        }
        public class CreateOpportunityFollowUpRequest
        {
            public int Ref_OpportunityId { get; set; }
            public DateTime NextFollowUpDate { get; set; }
            public int? FollowUpAssignedEmployeeId { get; set; }
            public string? FollowUpRemarks { get; set; }
            public byte? FollowUpPriority { get; set; }
            public int CreatedBy { get; set; }
        }
        public class CloseOpportunityRequest
        {
            public int Ref_OpportunityId { get; set; }
            public int Ref_OpportunityStageIdClose { get; set; }  // Pass the OpportunityStageId where IsClosed=1 (e.g. CWON=6, CLOST=7)
            public string? ClosureRemark { get; set; }
            public int CreatedBy { get; set; }
        }
        public class OpportunityCloseStageDropdown
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public bool IsWon { get; set; }
            public bool IsLost { get; set; }
        }
        public class UpdateOpportunityFollowUpStatusRequest
        {
            public int FollowUpId { get; set; }
            public int Ref_OpportunityId { get; set; }
            public int Ref_ActivityStatusId { get; set; }
            public DateTime? NewScheduledOn { get; set; }
            public string? StatusRemark { get; set; }
            public int CreatedBy { get; set; }
        }
        public class DeleteOpportunityRequirementRequest
        {
            public int Ref_OpportunityId { get; set; }
            public int Ref_OpportunityRequirementId { get; set; }

            public string DeleteRemark { get; set; }
            public int? CreatedBy { get; set; }
        }

        public class AddOpportunityActivityExpenseRequest
        {
            public int Ref_OpportunityId { get; set; }

            public int Ref_ActivityId { get; set; }

            public List<OpportunityActivityExpenseItem> Expenses { get; set; } = new();

            public int? CreatedBy { get; set; }
        }

        public class OpportunityActivityExpenseItem
        {
            public int Ref_ExpenseCategoryId { get; set; }

            public int? Ref_TransportModeId { get; set; }

            public decimal Amount { get; set; }

            public string? Description { get; set; }

            public string? FromLocation { get; set; }

            public string? ToLocation { get; set; }

            public List<OpportunityActivityExpenseAttachmentItem> Attachments { get; set; } = new();
        }

        public class OpportunityActivityExpenseAttachmentItem
        {
            public string FileName { get; set; } = null!;

            public string Base64 { get; set; } = null!;
        }
    }
}
