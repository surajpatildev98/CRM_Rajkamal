namespace CRMWebServices.Models
{
    /// <summary>Request DTOs for the Ticket Support module (api/TicketSupport, USP_TKT_TicketManagement).</summary>
    public class TicketSupportModels
    {
        public class TicketFileItem
        {
            public string FileName { get; set; } = string.Empty;
            public string Base64Content { get; set; } = string.Empty;
            /// <summary>PHOTO | SIGNATURE | BILL | OTHER</summary>
            public string? AttachmentType { get; set; }
        }

        public class TicketExpenseItem
        {
            public int? TicketActivityId { get; set; }
            /// <summary>TRANSPORT | STAY | FOOD | OTHER (derived from TravelMode when empty)</summary>
            public string? Category { get; set; }
            /// <summary>BUS | TRAIN | AUTO | PERSONAL_BIKE | PERSONAL_CAR | STAY | OTHER</summary>
            public string TravelMode { get; set; } = string.Empty;
            public string? FromLocation { get; set; }
            public string? ToLocation { get; set; }
            public decimal? Km { get; set; }
            public decimal Amount { get; set; }
            public string? Remarks { get; set; }
            /// <summary>Optional receipt, saved under Uploads/TicketExpenseReceipts.</summary>
            public TicketFileItem? Receipt { get; set; }
        }

        public class CreateSupportTicketRequest
        {
            /// <summary>CRM | WMS, from api/Lead/search-customer-all.</summary>
            public string CustomerSource { get; set; } = string.Empty;
            /// <summary>CRM: LeadOrganizationId, WMS: CustomerMasterID.</summary>
            public int SourceCustomerId { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public string Issue { get; set; } = string.Empty;
            /// <summary>LOW | MEDIUM | HIGH</summary>
            public string? Priority { get; set; }
            /// <summary>ONLINE | VISIT</summary>
            public string ServiceMode { get; set; } = string.Empty;
            public DateTime ExpectedDate { get; set; }
            /// <summary>Support Manager only: mapped employee to assign. Ignored for others (self-assigned).</summary>
            public int? AssignedTo { get; set; }
            /// <summary>Travel request sent with an employee-raised Visit ticket.</summary>
            public List<TicketExpenseItem>? Travel { get; set; }
        }

        public class TicketTravelRequest
        {
            public int TicketId { get; set; }
            public List<TicketExpenseItem> Travel { get; set; } = new();
        }

        public class TicketDecisionRequest
        {
            public int TicketId { get; set; }
            /// <summary>Set to decide a single expense; empty decides the whole ticket.</summary>
            public int? TicketExpenseId { get; set; }
            /// <summary>APPROVE | REJECT</summary>
            public string Decision { get; set; } = string.Empty;
            public string? Remarks { get; set; }
        }

        public class TicketCheckInRequest
        {
            public int TicketId { get; set; }
            public decimal Latitude { get; set; }
            public decimal Longitude { get; set; }
            public string? Address { get; set; }
        }

        public class TicketActivityRequest
        {
            public int TicketId { get; set; }
            /// <summary>SITE_VISIT | CALL | ONLINE</summary>
            public string ActivityType { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public DateTime? ActivityOn { get; set; }
            public string? Location { get; set; }
            public decimal? Latitude { get; set; }
            public decimal? Longitude { get; set; }
            public string? ContactPerson { get; set; }
            public string? Remarks { get; set; }
            /// <summary>PLANNED | COMPLETED</summary>
            public string? ActivityStatus { get; set; }
        }

        public class TicketExpenseRequest
        {
            public int TicketId { get; set; }
            public List<TicketExpenseItem> Expenses { get; set; } = new();
        }

        public class TicketCompleteRequest
        {
            public int TicketId { get; set; }
            public string ResolutionRemarks { get; set; } = string.Empty;
            public List<TicketFileItem>? Attachments { get; set; }
        }

        public class TicketAttachmentRequest
        {
            public int TicketId { get; set; }
            public List<TicketFileItem> Attachments { get; set; } = new();
        }

        public class TicketServiceModeRequest
        {
            public int TicketId { get; set; }
            /// <summary>ONLINE | VISIT</summary>
            public string ServiceMode { get; set; } = string.Empty;
        }

        public class TicketCancelRequest
        {
            public int TicketId { get; set; }
            public string? Remarks { get; set; }
        }
    }
}
