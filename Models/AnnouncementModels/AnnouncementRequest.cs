namespace CRMWebServices.Models.AnnouncementModels
{

    public class AnnouncementRequest
    {
        public int? AnnouncementId { get; set; }

        public string? Title { get; set; }
        public string? Message { get; set; }

        public string? AttachmentPath { get; set; }
        public string? AttachmentName { get; set; }
        public string? AttachmentType { get; set; }
        public long? AttachmentSize { get; set; }

        public DateTime? ScheduledOn { get; set; }

        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }

        public List<AnnouncementTargetRequest>? Targets { get; set; }
    }

    public class AnnouncementTargetRequest
    {
        public int? AnnouncementTargetId { get; set; }

        public int Ref_CompanyId { get; set; }
        public int? Ref_RegionId { get; set; }
        public int? Ref_StateId { get; set; }
        public int? Ref_OfficeId { get; set; }
        public int? Ref_DepartmentId { get; set; }
        public bool SendPush { get; set; } = true;
        public bool SendEmail { get; set; } = true;
    }

}

