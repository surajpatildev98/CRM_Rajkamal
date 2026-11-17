namespace CRMWebServices.Models
{
    public class AmcModels
    {
        /// <summary>One PDF as Base64 (invoice).</summary>
        public class AmcFile
        {
            public string FileName { get; set; } = "";
            public string Base64Content { get; set; } = "";
        }

        public class AmcItem
        {
            public int? CategoryId { get; set; }
            public string? CategoryName { get; set; }
            public int? OfferingId { get; set; }
            public string? OfferingName { get; set; }
            public long? ProductId { get; set; }
            public string? ProductName { get; set; }
            public string? SerialNo { get; set; }
            public string? LicenseKey { get; set; }
            public string? SoftwareVersion { get; set; }
            public int? NoOfUsers { get; set; }
            public string? Remark { get; set; }
        }

        /// <summary>Create (AmcContractId = 0), Edit, and Renew (AmcContractId = contract being renewed).</summary>
        public class AmcSaveRequest
        {
            public int AmcContractId { get; set; }
            public string? AmcType { get; set; }            // HARDWARE | SOFTWARE
            public string? CoverageType { get; set; }       // COMPREHENSIVE | NON_COMPREHENSIVE
            public string? CustomerSource { get; set; }     // CRM | WMS (same as Ticket)
            public int SourceCustomerId { get; set; }
            public string? CustomerName { get; set; }
            public string? ContactMobile { get; set; }
            public string? ContactEmail { get; set; }
            public int DurationMonths { get; set; }
            public DateTime? StartDate { get; set; }
            public int? TotalVisits { get; set; }
            public string? InvoiceNo { get; set; }
            public DateTime? InvoiceDate { get; set; }
            public AmcFile? InvoiceFile { get; set; }
            public decimal? Amount { get; set; }
            public decimal? GstPercent { get; set; }
            public string? PaymentStatus { get; set; }      // PENDING | PARTIAL | PAID
            public string? PaymentMode { get; set; }
            public DateTime? PaymentDate { get; set; }
            public string? PaymentRemark { get; set; }
            public string? Remarks { get; set; }
            public List<int>? CoverageServiceIds { get; set; }
            public List<AmcItem>? Items { get; set; }
        }

        public class AmcInvoiceRequest
        {
            public int AmcContractId { get; set; }
            public string? InvoiceNo { get; set; }
            public DateTime? InvoiceDate { get; set; }
            public AmcFile? InvoiceFile { get; set; }
        }

        public class AmcCancelRequest
        {
            public int AmcContractId { get; set; }
            public string? Reason { get; set; }
        }

        /// <summary>Excel upload: the app reads the sheet and sends one row per item.
        /// Rows with the same Customer + Invoice No become one AMC.</summary>
        public class AmcUploadRow : AmcItem
        {
            public int RowNo { get; set; }
            public string? AmcType { get; set; }
            public string? CoverageType { get; set; }
            public string? CustomerSource { get; set; }
            public int SourceCustomerId { get; set; }
            public string? CustomerName { get; set; }
            public string? ContactMobile { get; set; }
            public string? ContactEmail { get; set; }
            public int DurationMonths { get; set; }
            public DateTime? StartDate { get; set; }
            public int? TotalVisits { get; set; }
            public string? InvoiceNo { get; set; }
            public DateTime? InvoiceDate { get; set; }
            public decimal? Amount { get; set; }
            public decimal? GstPercent { get; set; }
            public string? PaymentStatus { get; set; }
            public string? PaymentMode { get; set; }
            public DateTime? PaymentDate { get; set; }
            public string? Remarks { get; set; }
            public string? CoverageServiceIds { get; set; }  // "1,3"
        }

        public class AmcUploadRequest
        {
            public List<AmcUploadRow>? Rows { get; set; }
        }

        public class AmcCoverageSaveRequest
        {
            public int CoverageServiceId { get; set; }
            public string? ServiceName { get; set; }
            public string? AmcType { get; set; }            // HARDWARE | SOFTWARE | BOTH
            public bool? IsActive { get; set; }
        }
    }
}
