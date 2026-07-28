namespace CRMWebServices.Models
{
    public class LeadRegisterRequest
    {
      
        public string CustomerName { get; set; }
        public string Address { get; set; }
        public int? Ref_CountryId { get; set; }
        public int? Ref_StateId { get; set; }
        public int? Ref_CityId { get; set; }
        public string PinCode { get; set; }
        public string Gst { get; set; }
        public string Pan { get; set; }

        
        public List<LeadContactRequest> Contacts { get; set; }

       
        public string Requirement { get; set; }
        public int Ref_LeadSourceId { get; set; }
        public string LeadRemark { get; set; }

       
        public int? AssignedToEmployeeId { get; set; }
        public string? AssignmentRemark { get; set; }

        public int? CreatedBy { get; set; }
      
        public List<LeadRequirementItem> Requirements { get; set; }

     
        public List<LeadAttachmentItem> Attachments { get; set; }
        public int? LeadOrganizationId { get; set; } // added for existing customer, if the lead is for an existing customer, this will be the id of that customer in the LeadOrganization table
    }
    public class LeadAttachmentItem
    {
        public string FileName { get; set; }
        public string Base64Content { get; set; }
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

        public int? Ref_ActivityStatusId { get; set; }   
        public int? Ref_NewLeadStatusId { get; set; }    

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public List<LeadActivityAttachmentItem> Attachments { get; set; }

        public int? CreatedBy { get; set; }
    }

    public class LeadFollowUpRequest
    {
        public int Ref_LeadId { get; set; }

        public DateTime NextFollowUpDate { get; set; }

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

        public int? LoggedInEmployeeId { get; set; }
        public bool ShowCreatedAndAssigned { get; set; }  // For PreSales
    }


    public class FollowUpStatusUpdateRequest
    {
        public int FollowUpId { get; set; }

        public int Ref_LeadId { get; set; }
        public int Ref_ActivityStatusId { get; set; }
        public DateTime? NewScheduledOn { get; set; }
        public string StatusRemark { get; set; }
        public int? CreatedBy { get; set; }
    }

    public class LeadCloseRequest
    {
        public int Ref_LeadId { get; set; }
        public int Ref_LeadStatusIdClose { get; set; } 
        public int? Ref_LostReasonId { get; set; }
        public string ClosureRemark { get; set; }
        public int? CreatedBy { get; set; }
    }


    public class LeadAccessInfo
    {
        public int LeadId { get; set; } 
        public int CreatedBy { get; set; }
        public int? AssignedEmployeeId { get; set; }
        //public int Ref_LeadStatusId { get; set; }


    }

    public class LeadAssignRequest
    {
        public int Ref_LeadId { get; set; }
        public int AssignedToEmployeeId { get; set; }
        public string AssignmentRemark { get; set; }
        public int? CreatedBy { get; set; }
    }


    //public class ExistingCustomerResponse
    //{
    //    public int LeadOrganizationId { get; set; }

    //    public string CustomerName { get; set; }

    //    public string Address { get; set; }

    //    public int? Ref_CountryId { get; set; }

    //    public int? Ref_StateId { get; set; }

    //    public int? Ref_CityId { get; set; }

    //    public int PinCode { get; set; }

    //    public string Gst { get; set; }

    //    public string Pan { get; set; }

    //    public List<ExistingCustomerContact> Contacts { get; set; } = new();
    //}

    public class ExistingCustomerResponse
    {
        public int CompanyId { get; set; }
        public string? CustomerName { get; set; }
        public string? Pan { get; set; }
        public List<CustomerBranchOption> Branches { get; set; } = new();
    }

    public class CustomerBranchOption
    {
        public int LeadOrganizationId { get; set; }
        public string? CustomerName { get; set; }
        public string? Address { get; set; }
        public int? Ref_CountryId { get; set; }
        public int? Ref_StateId { get; set; }
        public int? Ref_CityId { get; set; }
        public int PinCode { get; set; }
        public string? Gst { get; set; }
        public string? Pan { get; set; }
        public bool IsExactGstMatch { get; set; }
        public List<ExistingCustomerContact> Contacts { get; set; } = new();
    }
    public class ExistingCustomerContact
    {
        public int LeadContactId { get; set; }

        public int Ref_LeadOrganizationId { get; set; }

        public string ContactPerson { get; set; }

        public string Mobile { get; set; }

        public string Email { get; set; }

        public int? Ref_DesignationId { get; set; }
    }

    public class CustomerSearchResponse
    {
        public int LeadOrganizationId { get; set; }
        public int Ref_LeadCompanyId { get; set; }
        public string? CustomerName { get; set; }
        public string? CityName { get; set; }
        public string? DisplayName { get; set; }   // "ABC Company - Pune"
        public string? Address { get; set; }
        public string? Gst { get; set; }
        public string? Pan { get; set; }
    }

}