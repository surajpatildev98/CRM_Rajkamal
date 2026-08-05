using System;
using System.Collections.Generic;
using static CRMWebServices.Models.OpportunityModels;

namespace CRMWebServices.Models
{
    public class OpportunityQuotationModels
    {
        // ==========================================================
        // REQUESTS
        // ==========================================================

        public class RaiseQuotationRequest
        {
            public int Ref_OpportunityId { get; set; }
            public DateTime? QuotationDate { get; set; }
            public DateTime? ValidUntil { get; set; }
            public decimal? FreightCharge { get; set; }
            public decimal? TaxPercent { get; set; }
            public string? TermsRemark { get; set; }
            public List<QuotationItemInput> Items { get; set; } = new();
            public List<AttachmentInput>? Attachments { get; set; }
            public int CreatedBy { get; set; }
        }

        public class QuotationItemInput
        {
            public int Ref_OpportunityRequirementId { get; set; }
            public decimal Quantity { get; set; }
            public decimal Rate { get; set; }
        }

        public class LogCustomerResponseRequest
        {
            public int Ref_OpportunityId { get; set; }
            public int Ref_OpportunityQuotationId { get; set; }
            public string CustomerResponseCode { get; set; } = null!; // ACCEPTED | NEEDS_CHANGES | REJECTED
            public string? CustomerResponseRemark { get; set; }
            public int CreatedBy { get; set; }
        }

        public class LogNegotiationRoundRequest
        {
            public int Ref_OpportunityId { get; set; }
            public int Ref_OpportunityQuotationId { get; set; }
            public decimal? OurOfferAmount { get; set; }
            public decimal? CustomerCounterAmount { get; set; }
            public string? Remarks { get; set; }
            public DateTime? NextFollowUpDate { get; set; }
            public int CreatedBy { get; set; }
        }

        public class DecideManagementReviewRequest
        {
            public int Ref_ManagementReviewId { get; set; }
            public decimal? Margin { get; set; }
            public string DecisionCode { get; set; } = null!; // APPROVED | REJECTED
            public string? Remarks { get; set; }
            public int? ReviewedByEmployeeId { get; set; }
            public int CreatedBy { get; set; }
        }

        // ==========================================================
        // RESPONSES
        // ==========================================================

        public class QuotationDetailResponse
        {
            public QuotationInfo Quotation { get; set; } = null!;
            public List<QuotationItemInfo> Items { get; set; } = new();
            public List<QuotationAttachmentInfo> Attachments { get; set; } = new();
            public List<NegotiationRoundInfo> NegotiationRounds { get; set; } = new();
            public List<ManagementReviewInfo> Reviews { get; set; } = new();
        }

        public class QuotationInfo
        {
            public int OpportunityQuotationId { get; set; }
            public int Ref_OpportunityId { get; set; }
            public string QuotationNo { get; set; } = null!;
            public int VersionNumber { get; set; }
            public bool IsCurrent { get; set; }
            public DateTime QuotationDate { get; set; }
            public DateTime? ValidUntil { get; set; }
            public decimal FreightCharge { get; set; }
            public decimal TaxPercent { get; set; }
            public decimal SubTotal { get; set; }
            public decimal TotalAmount { get; set; }
            public string? TermsRemark { get; set; }
            public int? SentByEmployeeId { get; set; }
            public DateTime? SentOn { get; set; }
            public string CustomerResponseCode { get; set; } = null!;
            public int? CustomerResponseByEmployeeId { get; set; }
            public DateTime? CustomerResponseOn { get; set; }
            public string? CustomerResponseRemark { get; set; }
        }

        public class QuotationItemInfo
        {
            public int OpportunityQuotationItemId { get; set; }
            public int Ref_OpportunityRequirementId { get; set; }
            public string ProductName { get; set; } = null!;
            public string? Unit { get; set; }
            public decimal Quantity { get; set; }
            public decimal Rate { get; set; }
            public decimal Amount { get; set; }
        }

        public class QuotationAttachmentInfo
        {
            public int AttachmentId { get; set; }
            public string FileName { get; set; } = null!;
            public string? FileType { get; set; }
            public long? FileSize { get; set; }
            public string? FileUrl { get; set; }
            public DateTime CreatedOn { get; set; }
        }

        public class NegotiationRoundInfo
        {
            public int OpportunityNegotiationId { get; set; }
            public int RoundNumber { get; set; }
            public decimal OurOfferAmount { get; set; }
            public decimal? CustomerCounterAmount { get; set; }
            public string? Remarks { get; set; }
            public DateTime RoundDate { get; set; }
            public DateTime? NextFollowUpDate { get; set; }
            public string StatusCode { get; set; } = null!;
        }

        public class ManagementReviewInfo
        {
            public int ManagementReviewId { get; set; }
            public int? Ref_OpportunityQuotationId { get; set; }
            public int? Ref_OpportunityNegotiationId { get; set; }
            public decimal? Margin { get; set; }
            public string? Remarks { get; set; }
            public string DecisionCode { get; set; } = null!;
            public int? ReviewedByEmployeeId { get; set; }
            public DateTime? ReviewedOn { get; set; }
            public DateTime CreatedOn { get; set; }
        }


        public class RaiseQuotationItemResponse
        {
            public int OpportunityRequirementId { get; set; }
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public decimal Quantity { get; set; }
            public int? UnitId { get; set; }
            public string UnitName { get; set; }
            public decimal Rate { get; set; }
        }
    }
}