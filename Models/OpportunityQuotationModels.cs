using System;
using System.Collections.Generic;
using static CRMWebServices.Models.OpportunityModels;

namespace CRMWebServices.Models
{
    public class OpportunityQuotationModels
    {        
        public class RaiseQuotationRequest
        {
            public int Ref_OpportunityId { get; set; }

            //public int Ref_OpportunityQuotationId { get; set; }
            public DateTime? QuotationDate { get; set; }
            public DateTime? ValidUntil { get; set; }

            // Send EITHER the amount OR the percent for each — whichever the user typed in
            //public decimal? FreightAmount { get; set; }
            //public decimal? FreightPercent { get; set; }
            public decimal? TaxAmount { get; set; }
            public decimal? TaxPercent { get; set; }

            public string? TermsRemark { get; set; }
            public List<QuotationItemInput> Items { get; set; } = new();

            public string? QuotationPdfFileName { get; set; }
            public string? QuotationPdfBase64 { get; set; }
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
            public string CustomerResponseCode { get; set; } = null!; // ACCEPTED  | REJECTED
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
            public int QuotationId { get; set; }
            //public int Ref_ManagementReviewId { get; set; }
            //public decimal? Margin { get; set; }
            public string DecisionCode { get; set; } = null!; // APPROVED | REJECTED
            public string? Remarks { get; set; }
            public int? ReviewedByEmployeeId { get; set; }
            public int CreatedBy { get; set; }
        }
        //public class QuotationDetailResponse
        //{
        //    public QuotationInfo Quotation { get; set; } = null!;
        //    public List<QuotationItemInfo> Items { get; set; } = new();
        //    public List<QuotationAttachmentInfo> Attachments { get; set; } = new();
        //    public List<NegotiationRoundInfo> NegotiationRounds { get; set; } = new();
        //    public List<ManagementReviewInfo> Reviews { get; set; } = new();
        //}

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
            public decimal FreightPercent { get; set; }

            public int? Ref_FreightPolicyId { get; set; }
            public int? Ref_FreightPolicySlabId { get; set; }
            public string? FreightPolicyName { get; set; }
            public int? FreightPolicyVersion { get; set; }
            public decimal? FreightPolicyMinOrderValue { get; set; }
            public decimal? FreightPolicyMaxOrderValue { get; set; }
            public decimal? FreightPolicyAmount { get; set; }
            public decimal TaxAmount { get; set; }
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
            public string? SentByEmployeeName { get; set; }
            public int? Ref_PreviousQuotationId { get; set; }
            public int RevisionNumber { get; set; }
            public string? RootQuotationNo { get; set; }
            public OwnerInfo? Owner { get; set; }
        }
        //public class QuotationItemInfo
        //{
        //    public int OpportunityQuotationItemId { get; set; }
        //    public int Ref_OpportunityRequirementId { get; set; }
        //    public string ProductName { get; set; } = null!;
        //    public string? Unit { get; set; }
        //    public decimal Quantity { get; set; }
        //    public decimal Rate { get; set; }
        //    public decimal Amount { get; set; }
        //}

        public class QuotationItemInfo
        {
            public int OpportunityQuotationItemId { get; set; }
            public int Ref_OpportunityRequirementId { get; set; }
            public int? CategoryId { get; set; }
            public string? CategoryName { get; set; }
            public int? OfferingId { get; set; }
            public string? OfferingName { get; set; }
            public long? ProductId { get; set; }
            public string? ProductCode { get; set; }
            public string? ProductName { get; set; }
            public string? Unit { get; set; }
            public decimal Quantity { get; set; }
            public decimal Rate { get; set; }
            public decimal Amount { get; set; }
        }
        public class QuotationAttachmentInfo
        {
            public int AttachmentId { get; set; }
            public int Ref_OpportunityQuotationId { get; set; }
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
        //public class RaiseQuotationItemResponse
        //{           

        //    public int OpportunityRequirementId { get; set; }
        //    public int? Ref_LeadRequirementId { get; set; }
        //    public int ProductId { get; set; }
        //    public string ProductName { get; set; } = string.Empty;
        //    public decimal Quantity { get; set; }
        //    public int? UnitId { get; set; }
        //    public string? UnitName { get; set; }
        //    public string? RequirementRemark { get; set; }
        //}

        public class RaiseQuotationItemResponse
        {
            public int OpportunityRequirementId { get; set; }
            public int? Ref_LeadRequirementId { get; set; }
            public int? Ref_ProductCategoryId { get; set; }
            public string? CategoryName { get; set; }
            public int? Ref_OfferingId { get; set; }
            public string? OfferingName { get; set; }
            public long? ProductId { get; set; }
            public string? ProductCode { get; set; }
            public string? ProductName { get; set; }
            public decimal CurrentRate { get; set; }
            public decimal Quantity { get; set; }
            public int? UnitId { get; set; }
            public string? UnitName { get; set; }            public string? RequirementRemark { get; set; }
        }
        public class RaiseQuotationPageResponse
        {
            public RaiseQuotationHeader Header { get; set; } = new();
            public List<RaiseQuotationItemResponse> Items { get; set; } = new();
            public List<FreightPolicySlabQuotationResponse> FreightPolicySlabs { get; set; } = new();
        }
        public class RaiseQuotationHeader
        {
            public int OpportunityId { get; set; }
            public string OpportunityNo { get; set; } = string.Empty;
            public string CompanyName { get; set; } = string.Empty;
            public int OwnerId { get; set; }
            public string OwnerName { get; set; } = string.Empty;
        }
        public class QuotationListRequest
        {
            public string? SearchText { get; set; }
            public int? FilterOwnerId { get; set; }
            public int? Ref_OpportunityId { get; set; }   // optional drill-down filter
            public bool ShowCreatedAndAssigned { get; set; }   // for PreSales, same as OpportunityListRequest

            public int? Ref_RaisedByEmployeeId { get; set; }   // NEW
            public bool OnlyPendingReview { get; set; }
        }
        public class QuotationListResponse
        {
            public int OpportunityQuotationId { get; set; }
            public int Ref_OpportunityId { get; set; }
            public string OpportunityNo { get; set; } = null!;
            public string? OpportunityName { get; set; }
            public string? CompanyName { get; set; }
            public string QuotationNo { get; set; } = null!;
            public int VersionNumber { get; set; }
            public DateTime QuotationDate { get; set; }
            public DateTime? ValidUntil { get; set; }
            public decimal SubTotal { get; set; }
            public decimal FreightCharge { get; set; }
            public decimal FreightPercent { get; set; }

            public int? Ref_FreightPolicyId { get; set; }
            public int? Ref_FreightPolicySlabId { get; set; }
            public string? FreightPolicyName { get; set; }
            public int? FreightPolicyVersion { get; set; }

            public decimal TaxAmount { get; set; }
            public decimal TaxPercent { get; set; }
            public decimal TotalAmount { get; set; }
            public string CustomerResponseCode { get; set; } = null!;
            public bool IsCurrent { get; set; }
            public DateTime CreatedOn { get; set; }
            public string? RaisedBy { get; set; }
            public int? OwnerEmployeeId { get; set; }

            // NEW
            public int? SentByEmployeeId { get; set; }
            public string? SentByEmployeeName { get; set; }

            public int RevisionNumber { get; set; }
            public string? RootQuotationNo { get; set; }
            public int? Ref_PreviousQuotationId { get; set; }

            public int? OpportunityStageId { get; set; }
            public string? OpportunityStageCode { get; set; }
            public string? OpportunityStageName { get; set; }
        }
        public class ReviseQuotationRequest
        {
            public int Ref_OpportunityId { get; set; }

            public int Ref_OpportunityQuotationId { get; set; }

            public DateTime? QuotationDate { get; set; }
            public DateTime? ValidUntil { get; set; }

            //public decimal? FreightAmount { get; set; }
            //public decimal? FreightPercent { get; set; }

            public decimal? TaxAmount { get; set; }
            public decimal? TaxPercent { get; set; }

            public string? TermsRemark { get; set; }

            public List<QuotationItemInput> Items { get; set; } = new();

            public string? QuotationPdfFileName { get; set; }
            public string? QuotationPdfBase64 { get; set; }

            public int CreatedBy { get; set; }
        }

        public class QuotationHistoryInfo
        {
            public int OpportunityQuotationId { get; set; }

            public int Ref_OpportunityId { get; set; }

            public int? Ref_PreviousQuotationId { get; set; }

            public string QuotationNo { get; set; } = null!;

            public string? RootQuotationNo { get; set; }

            public int VersionNumber { get; set; }

            public int RevisionNumber { get; set; }

            public bool IsCurrent { get; set; }

            public DateTime QuotationDate { get; set; }

            public DateTime? ValidUntil { get; set; }

            public decimal SubTotal { get; set; }

            public decimal FreightCharge { get; set; }

            public decimal FreightPercent { get; set; }

            public int? Ref_FreightPolicyId { get; set; }
            public int? Ref_FreightPolicySlabId { get; set; }
            public string? FreightPolicyName { get; set; }
            public int? FreightPolicyVersion { get; set; }

            public decimal TaxAmount { get; set; }

            public decimal TaxPercent { get; set; }

            public decimal TotalAmount { get; set; }

            public string? TermsRemark { get; set; }

            public int? SentByEmployeeId { get; set; }

            public DateTime? SentOn { get; set; }

            public string? SentByEmployeeName { get; set; }

            public string CustomerResponseCode { get; set; } = null!;

            public int? OpportunityStageId { get; set; }

            public string? OpportunityStageCode { get; set; }

            public string? OpportunityStageName { get; set; }

            public int? CustomerResponseByEmployeeId { get; set; }

            public DateTime? CustomerResponseOn { get; set; }

            public string? CustomerResponseRemark { get; set; }

            public DateTime CreatedOn { get; set; }

            public string? CreatedByName { get; set; }

            // NEW
            public List<QuotationAttachmentInfo> Attachments { get; set; } = new();

        }
        public class FreightPolicySlabQuotationResponse
        {
            public int FreightPolicyId { get; set; }
            public string PolicyName { get; set; } = string.Empty;
            public int VersionNo { get; set; }
            public int FreightPolicySlabId { get; set; }
            public decimal MinOrderValue { get; set; }
            public decimal? MaxOrderValue { get; set; }
            public decimal FreightAmount { get; set; }
        }
        public class QuotationDetailResponse
        {
            public QuotationInfo Quotation { get; set; } = null!;

            public List<QuotationItemInfo> Items { get; set; } = new();

            public List<QuotationAttachmentInfo> Attachments { get; set; } = new();

            public List<QuotationHistoryInfo> History { get; set; } = new();

            public List<ManagementReviewInfo> Reviews { get; set; } = new();

            public List<FreightPolicySlabQuotationResponse> FreightPolicySlabs { get; set; } = new();
        }
    }
}