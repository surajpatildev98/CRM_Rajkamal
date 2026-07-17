namespace CRMWebServices.Models
{
    public class LeadRegisterRequest
    {
        // Organization / Company Details
        public string CustomerName { get; set; }
        public string Address { get; set; }
        public int? Ref_CountryId { get; set; }
        public int? Ref_StateId { get; set; }
        public int? Ref_CityId { get; set; }
        public string PinCode { get; set; }
        public string Gst { get; set; }
        public string Pan { get; set; }

        // Contacts — multiple
        public List<LeadContactRequest> Contacts { get; set; }

        // Lead Details
        public string Requirement { get; set; }
        public int Ref_LeadSourceId { get; set; }
        public string LeadRemark { get; set; }

        // Assignment — if null, lead self-assigns to CreatedBy
        public int? AssignedToEmployeeId { get; set; }
        public string AssignmentRemark { get; set; }

        public int? CreatedBy { get; set; }
        //public string AttachmentPath { get; set; }
        public List<LeadRequirementItem> Requirements { get; set; }

        public string AttachmentBase64 { get; set; }   // raw base64 string, no data-URI prefix
        public string AttachmentFileName { get; set; } // original filename, needed to preserve extension
    }

    public class LeadContactRequest
    {
        public string ContactPerson { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public int? Ref_DesignationId { get; set; }
    }

    public class LeadTransferRequest
    {
        public int Ref_LeadId { get; set; }
        public int AssignedToEmployeeId { get; set; }
        public string AssignmentRemark { get; set; }
        public int? CreatedBy { get; set; }
    }

    public class LeadStatusUpdateRequest
    {
        public int Ref_LeadId { get; set; }
        public int Ref_LeadStatusId { get; set; }
        public string StatusRemark { get; set; }
        public int? CreatedBy { get; set; }
    }

    public class LeadRequirementItem
    {
        public int Ref_ProductId { get; set; }
        public decimal Quantity { get; set; }
        public int? Ref_UnitId { get; set; }
        public string RequirementRemark { get; set; }
    }

   
    public class LeadActivityRequest
    {
        public int Ref_LeadId { get; set; }
        public int Ref_ActivityTypeId { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public string? Outcome { get; set; }
        public DateTime ActivityDateTime { get; set; }
        public int? DurationInMinutes { get; set; }

        public int? Ref_ActivityStatusId { get; set; }   // defaults to Completed if null
        public int? Ref_NewLeadStatusId { get; set; }    // optional — changes lead stage if provided

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public List<LeadActivityAttachmentItem> Attachments { get; set; }

        public int? CreatedBy { get; set; }
    }

    public class LeadFollowUpRequest
    {
        public int Ref_LeadId { get; set; }
        public int? Ref_ActivityId { get; set; }         // optional link to a specific activity
        public DateTime NextFollowUpDate { get; set; }
        //public byte? FollowUpPriority { get; set; } // 1=LOW, 2=MEDIUM, 3=HIGH, 4=URGENT
        public string FollowUpRemarks { get; set; }
        public int? FollowUpAssignedEmployeeId { get; set; }
        public int? CreatedBy { get; set; }
    }

    public class LeadActivityAttachmentItem
    {
        public string FileName { get; set; }
        public string Base64 { get; set; }
    }

    public class LeadListFilterRequest
    {
        public string? SearchText { get; set; }
        public int? FilterStatusId { get; set; }
        public int? FilterAssignedToId { get; set; }
    }
    //public enum FollowUpPriority
    //{
    //    LOW = 1,
    //    MEDIUM = 2,
    //    HIGH = 3,
    //    URGENT = 4
    //}


}