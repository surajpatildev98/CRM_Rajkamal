namespace CRMWebServices.Models.AnnouncementModels
{
    public class AnnouncementResponse
    {
        public class AnnouncementInfo
        {
            public int AnnouncementId { get; set; }
            public string? Title { get; set; }
            public string? Message { get; set; }

            public string? AttachmentPath { get; set; }
            public string? AttachmentName { get; set; }
            public string? AttachmentType { get; set; }
            public long? AttachmentSize { get; set; }

            public string? Status { get; set; }

            public DateTime? ScheduledOn { get; set; }
            public DateTime? SentOn { get; set; }

            public int? CreatedBy { get; set; }
            public DateTime? CreatedOn { get; set; }

            public int? UpdatedBy { get; set; }
            public DateTime? UpdatedOn { get; set; }

            public List<AnnouncementTargetInfo>? Targets { get; set; }
        }

        public class AnnouncementTargetInfo
        {
            public int AnnouncementTargetId { get; set; }
            public int Ref_AnnouncementId { get; set; }

            public int Ref_CompanyId { get; set; }
            public string? CompanyName { get; set; }

            public int? Ref_RegionId { get; set; }
            public string? RegionName { get; set; }

            public int? Ref_StateId { get; set; }
            public string? StateName { get; set; }

            public int? Ref_OfficeId { get; set; }
            public string? OfficeName { get; set; }

            public int? Ref_DepartmentId { get; set; }
            public string? DepartmentName { get; set; }
            public bool SendPush { get; set; } = true;
            public bool SendEmail { get; set; } = true;
        }

        public class AnnouncementRecipient
        {
            public int EmployeeId { get; set; }
            public string? EmployeeCode { get; set; }
            public string? FirstName { get; set; }
            public string? MiddleName { get; set; }
            public string? LastName { get; set; }
            public string? OfficialEmail { get; set; }

            public int CompanyId { get; set; }
            public string? CompanyName { get; set; }

            public int RegionId { get; set; }
            public string? RegionName { get; set; }

            public int StateId { get; set; }
            public string? StateName { get; set; }

            public int OfficeId { get; set; }
            public string? OfficeName { get; set; }

            public int? DepartmentId { get; set; }
            public string? DepartmentName { get; set; }

            public bool SendPush { get; set; }
            public bool SendEmail { get; set; }
        }

        public class AnnouncementRecipientResponse
        {
            public int RecipientCount { get; set; }

            public List<AnnouncementRecipient> Recipients { get; set; }
                = new List<AnnouncementRecipient>();
        }

        public class OfficeLookupInfo
        {
            public int OfficeId { get; set; }
            public string? OfficeName { get; set; }

            public int RegionId { get; set; }
            public string? RegionName { get; set; }

            public int StateId { get; set; }
            public string? StateName { get; set; }
        }

        public class DepartmentLookupInfo
        {
            public int DepartmentId { get; set; }
            public string? DepartmentName { get; set; }
        }
    }
}
