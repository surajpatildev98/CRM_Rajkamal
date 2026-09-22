namespace CRMWebServices.Models
{
    public class TicketModels
    {
        public class TicketListResponse
        {
            public int TicketId { get; set; }
            public string TicketNo { get; set; }
            public int OpportunityId { get; set; }
            public string OpportunityNo { get; set; }
            public string OpportunityName { get; set; }
            public int OpportunityRequirementId { get; set; }
            public long ProductId { get; set; }
            public string ProductCode { get; set; }
            public string ProductName { get; set; }
            public int AssignedEmployeeId { get; set; }
            public string AssignedEmployeeName { get; set; }
            public int TicketStatusId { get; set; }
            public string TicketStatusCode { get; set; }
            public string TicketStatusName { get; set; }
            public string Priority { get; set; }
            public DateTime? ScheduledOn { get; set; }
            public string Description { get; set; }
            public long? ActivityId { get; set; }
            public long? ActivityTypeId { get; set; }
            public string ActivityCode { get; set; }
            public string ActivityName { get; set; }
            public long? ActivityStatusId { get; set; }
            public string ActivityStatusCode { get; set; }
            public string ActivityStatusName { get; set; }
            public DateTime? ActivityDateTime { get; set; }
        }

        public class TicketDetailResponse
        {
            public TicketInfo Ticket { get; set; }
            public List<TicketActivityInfo> Activities { get; set; }
            public TicketQuotationEligibility QuotationEligibility { get; set; }
        }

        public class TicketInfo
        {
            public int TicketId { get; set; }
            public string TicketNo { get; set; }
            public int OpportunityId { get; set; }
            public string OpportunityNo { get; set; }
            public string OpportunityName { get; set; }
            public int? LeadId { get; set; }
            public int OpportunityRequirementId { get; set; }
            public long ProductId { get; set; }
            public string ProductCode { get; set; }
            public string ProductName { get; set; }
            public string ProductDescription { get; set; }
            public string BrandName { get; set; }
            public string ModelNo { get; set; }
            public int? ProductCategoryId { get; set; }
            public string ProductCategoryCode { get; set; }
            public string ProductCategoryName { get; set; }
            public int? OfferingId { get; set; }
            public string OfferingCode { get; set; }
            public string OfferingName { get; set; }
            public decimal Quantity { get; set; }
            public int? UnitId { get; set; }
            public int AssignedEmployeeId { get; set; }
            public string AssignedEmployeeName { get; set; }
            public string EmployeeCode { get; set; }
            public string AssignedEmployeeEmail { get; set; }
            public int TicketStatusId { get; set; }
            public string TicketStatusCode { get; set; }
            public string TicketStatusName { get; set; }
            public bool IsClosed { get; set; }
            public string Priority { get; set; }
            public DateTime? ScheduledOn { get; set; }
            public string TicketDescription { get; set; }
            public DateTime? CompletedOn { get; set; }
            public int? CompletedBy { get; set; }
            public string CompletedByEmployeeName { get; set; }
            public DateTime CreatedOn { get; set; }
            public long? CreatedBy { get; set; }
        }

        public class TicketActivityInfo
        {
            public long ActivityId { get; set; }
            public int TicketId { get; set; }
            public long OpportunityId { get; set; }
            public long ActivityTypeId { get; set; }
            public string ActivityCode { get; set; }
            public string ActivityName { get; set; }
            public long EmployeeId { get; set; }
            public string EmployeeName { get; set; }
            public DateTime ActivityDateTime { get; set; }
            public string Subject { get; set; }
            public string Description { get; set; }
            public string Outcome { get; set; }
            public DateTime? NextFollowUpDate { get; set; }
            public int? DurationInMinutes { get; set; }
            public decimal? Latitude { get; set; }
            public decimal? Longitude { get; set; }
            public string LocationAddress { get; set; }
            public long ActivityStatusId { get; set; }
            public string ActivityStatusCode { get; set; }
            public string ActivityStatusName { get; set; }
            public DateTime CreatedOn { get; set; }
            public long CreatedBy { get; set; }
            public DateTime? UpdatedOn { get; set; }
            public long? UpdatedBy { get; set; }
        }

        public class TicketQuotationEligibility
        {
            public bool IsQuotationEligible { get; set; }
            public int TicketId { get; set; }
            public string TicketNo { get; set; }
            public int OpportunityRequirementId { get; set; }
            public string TicketStatusCode { get; set; }
            public string TicketStatusName { get; set; }
        }

        public class CreateTicketRequest
        {
            public int OpportunityId { get; set; }
            public int OpportunityRequirementId { get; set; }
            public long? AssignedEmployeeId { get; set; }
            public long ActivityTypeId { get; set; }
            public DateTime ScheduledOn { get; set; }
            public string Description { get; set; }
            public string Priority { get; set; }
        }

        public class CreateTicketResponse
        {
            public int TicketId { get; set; }
            public string TicketNo { get; set; }
            public int OpportunityId { get; set; }
            public int OpportunityRequirementId { get; set; }
            public long AssignedEmployeeId { get; set; }
            public int TicketStatusId { get; set; }
            public string TicketStatusCode { get; set; }
            public string TicketStatusName { get; set; }
            public string Priority { get; set; }
            public DateTime? ScheduledOn { get; set; }
            public string Description { get; set; }
            public long ActivityId { get; set; }
        }
        public class GetMyTicketsRequest
        {
            public int EmployeeId { get; set; }
        }

        public class GetTicketDetailRequest
        {
            public int TicketId { get; set; }
        }

        public class UpdateTicketStatusRequest
        {
            public int TicketId { get; set; }
            public int TicketStatusId { get; set; }
            public string Description { get; set; }
            public int EmployeeId { get; set; }
        }

        public class TicketStatusInfo
        {
            public int TicketStatusId { get; set; }
            public string StatusCode { get; set; }
            public string StatusName { get; set; }
            public int DisplayOrder { get; set; }
            public bool IsClosed { get; set; }
            public bool IsActive { get; set; }
        }
        public class UpdateTicketActivityRequest
        {
            public int TicketId { get; set; }
            public long ActivityId { get; set; }
            public int ActivityStatusId { get; set; }
            public int EmployeeId { get; set; }
            public DateTime? ScheduledOn { get; set; }
            public string Description { get; set; }
        }
    }
}
