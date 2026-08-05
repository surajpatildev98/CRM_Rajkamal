using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Services;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using static CRMWebServices.Models.OpportunityQuotationModels;
using static CRMWebServices.Models.PermissionModels;

namespace CRMWebServices.Repository
{
    public class OpportunityQuotationRepository : IOpportunityQuotationRepository
    {
        private readonly IDatabaseService _database;
        private readonly ILogger<PermissionService> _logger;

        private const string SP_NAME = "USP_CRM_OpportunityQuotationManagement";

        public OpportunityQuotationRepository(IDatabaseService database, ILogger<PermissionService> logger)
        {
            _database = database;
            _logger = logger;
        }

        
        // Shared helpers — same pattern as OpportunityRepository
        

        private string? BuildFileUrl(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            var relative = filePath.Replace("Uploads/", "").Replace("Uploads\\", "");
            var segments = relative.Split('/').Select(Uri.EscapeDataString);

            return "/files/" + string.Join("/", segments);
        }

        private string SaveBase64Attachment(string base64Content, string fileName, string subFolder)
        {
            var base64Data = base64Content.Contains(",")
                ? base64Content.Substring(base64Content.IndexOf(",") + 1)
                : base64Content;

            byte[] fileBytes = Convert.FromBase64String(base64Data);

            string uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", subFolder);

            if (!Directory.Exists(uploadsRoot))
                Directory.CreateDirectory(uploadsRoot);

            string uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            string fullPath = Path.Combine(uploadsRoot, uniqueFileName);

            File.WriteAllBytes(fullPath, fileBytes);

            return Path.Combine("Uploads", subFolder, uniqueFileName).Replace("\\", "/");
        }

        private ApiResponse MapResponse(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0)
            {
                return new ApiResponse { Status = false, Message = "No response returned from database." };
            }

            var row = dt.Rows[0];
            bool status = Convert.ToInt32(row["Status"]) == 1;
            string message = row["Message"]?.ToString();

            if (!status)
            {
                return new ApiResponse { Status = false, Message = message };
            }

            var extraData = new Dictionary<string, object>();
            foreach (DataColumn col in dt.Columns)
            {
                if (col.ColumnName == "Status" || col.ColumnName == "Message")
                    continue;

                extraData[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
            }

            return new ApiResponse
            {
                Status = true,
                Message = message,
                Data = extraData.Count > 0 ? extraData : null
            };
        }

        
        // RAISE_QUOTATION
        
        public async Task<ApiResponse> RaiseQuotation(RaiseQuotationRequest model)
        {
            try
            {
                // Save attachments first (same pattern as CreateOpportunityActivity)
                List<object> savedAttachments = new List<object>();

                if (model.Attachments != null && model.Attachments.Count > 0)
                {
                    foreach (var att in model.Attachments)
                    {
                        if (string.IsNullOrWhiteSpace(att.Base64Content) || string.IsNullOrWhiteSpace(att.FileName))
                            continue;

                        try
                        {
                            string savedPath = SaveBase64Attachment(att.Base64Content, att.FileName, "OpportunityQuotationAttachments");

                            savedAttachments.Add(new
                            {
                                FileName = att.FileName,
                                FilePath = savedPath,
                                FileType = Path.GetExtension(att.FileName).TrimStart('.').ToLowerInvariant(),
                                FileSize = Convert.FromBase64String(att.Base64Content.Contains(",")
                                        ? att.Base64Content.Substring(att.Base64Content.IndexOf(",") + 1)
                                        : att.Base64Content).Length
                            });
                        }
                        catch (Exception ex)
                        {
                            return new ApiResponse { Status = false, Message = $"Error processing attachment '{att.FileName}': {ex.Message}" };
                        }
                    }
                }

                string? attachmentsJson = savedAttachments.Count > 0 ? JsonConvert.SerializeObject(savedAttachments) : null;
                string itemsJson = JsonConvert.SerializeObject(model.Items);

                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status", "RAISE_QUOTATION"),
                    new SqlParameter("@Ref_OpportunityId", model.Ref_OpportunityId),
                    new SqlParameter("@QuotationDate", (object?)model.QuotationDate ?? DBNull.Value),
                    new SqlParameter("@ValidUntil", (object?)model.ValidUntil ?? DBNull.Value),
                    new SqlParameter("@FreightCharge", (object?)model.FreightCharge ?? DBNull.Value),
                    new SqlParameter("@TaxPercent", (object?)model.TaxPercent ?? DBNull.Value),
                    new SqlParameter("@TermsRemark", (object?)model.TermsRemark ?? DBNull.Value),
                    new SqlParameter("@ItemsJson", itemsJson),
                    new SqlParameter("@AttachmentsMeta", (object?)attachmentsJson ?? DBNull.Value),
                    new SqlParameter("@CreatedBy", model.CreatedBy)
                );

                return MapResponse(dt);
            }
            catch (SqlException ex)
            {
                return new ApiResponse { Status = false, Message = $"Database Error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Status = false, Message = $"Error: {ex.Message}" };
            }
        }

        
        // GET_QUOTATION_DETAILS
        
        public async Task<ApiResponse> GetQuotationDetails(int opportunityQuotationId)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync(SP_NAME,
                    new SqlParameter("@Status", "GET_QUOTATION_DETAILS"),
                    new SqlParameter("@Ref_OpportunityQuotationId", opportunityQuotationId)
                );

                if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    return new ApiResponse { Status = false, Message = "Quotation not found." };
                }

                var qRow = ds.Tables[0].Rows[0];

                var quotation = new QuotationInfo
                {
                    OpportunityQuotationId = Convert.ToInt32(qRow["OpportunityQuotationId"]),
                    Ref_OpportunityId = Convert.ToInt32(qRow["Ref_OpportunityId"]),
                    QuotationNo = qRow["QuotationNo"]?.ToString(),
                    VersionNumber = Convert.ToInt32(qRow["VersionNumber"]),
                    IsCurrent = Convert.ToBoolean(qRow["IsCurrent"]),
                    QuotationDate = Convert.ToDateTime(qRow["QuotationDate"]),
                    ValidUntil = qRow["ValidUntil"] == DBNull.Value ? null : Convert.ToDateTime(qRow["ValidUntil"]),
                    FreightCharge = qRow["FreightCharge"] == DBNull.Value ? 0 : Convert.ToDecimal(qRow["FreightCharge"]),
                    TaxPercent = qRow["TaxPercent"] == DBNull.Value ? 0 : Convert.ToDecimal(qRow["TaxPercent"]),
                    SubTotal = Convert.ToDecimal(qRow["SubTotal"]),
                    TotalAmount = Convert.ToDecimal(qRow["TotalAmount"]),
                    TermsRemark = qRow["TermsRemark"] == DBNull.Value ? null : qRow["TermsRemark"].ToString(),
                    SentByEmployeeId = qRow["SentByEmployeeId"] == DBNull.Value ? null : Convert.ToInt32(qRow["SentByEmployeeId"]),
                    SentOn = qRow["SentOn"] == DBNull.Value ? null : Convert.ToDateTime(qRow["SentOn"]),
                    CustomerResponseCode = qRow["CustomerResponseCode"]?.ToString(),
                    CustomerResponseByEmployeeId = qRow["CustomerResponseByEmployeeId"] == DBNull.Value ? null : Convert.ToInt32(qRow["CustomerResponseByEmployeeId"]),
                    CustomerResponseOn = qRow["CustomerResponseOn"] == DBNull.Value ? null : Convert.ToDateTime(qRow["CustomerResponseOn"]),
                    CustomerResponseRemark = qRow["CustomerResponseRemark"] == DBNull.Value ? null : qRow["CustomerResponseRemark"].ToString()
                };

                var items = ds.Tables.Count > 1
                    ? ds.Tables[1].Rows.Cast<DataRow>().Select(r => new QuotationItemInfo
                    {
                        OpportunityQuotationItemId = Convert.ToInt32(r["OpportunityQuotationItemId"]),
                        Ref_OpportunityRequirementId = Convert.ToInt32(r["Ref_OpportunityRequirementId"]),
                        ProductName = r["ProductName"]?.ToString(),
                        Unit = r["Unit"] == DBNull.Value ? null : r["Unit"].ToString(),
                        Quantity = Convert.ToDecimal(r["Quantity"]),
                        Rate = Convert.ToDecimal(r["Rate"]),
                        Amount = Convert.ToDecimal(r["Amount"])
                    }).ToList()
                    : new List<QuotationItemInfo>();

                var attachments = ds.Tables.Count > 2
                    ? ds.Tables[2].Rows.Cast<DataRow>().Select(r => new QuotationAttachmentInfo
                    {
                        AttachmentId = Convert.ToInt32(r["AttachmentId"]),
                        FileName = r["FileName"]?.ToString(),
                        FileType = r["FileType"] == DBNull.Value ? null : r["FileType"].ToString(),
                        FileSize = r["FileSize"] == DBNull.Value ? (long?)null : Convert.ToInt64(r["FileSize"]),
                        FileUrl = BuildFileUrl(r["FilePath"]?.ToString()),
                        CreatedOn = Convert.ToDateTime(r["CreatedOn"])
                    }).ToList()
                    : new List<QuotationAttachmentInfo>();

                var rounds = ds.Tables.Count > 3
                    ? ds.Tables[3].Rows.Cast<DataRow>().Select(r => new NegotiationRoundInfo
                    {
                        OpportunityNegotiationId = Convert.ToInt32(r["OpportunityNegotiationId"]),
                        RoundNumber = Convert.ToInt32(r["RoundNumber"]),
                        OurOfferAmount = Convert.ToDecimal(r["OurOfferAmount"]),
                        CustomerCounterAmount = r["CustomerCounterAmount"] == DBNull.Value ? null : Convert.ToDecimal(r["CustomerCounterAmount"]),
                        Remarks = r["Remarks"] == DBNull.Value ? null : r["Remarks"].ToString(),
                        RoundDate = Convert.ToDateTime(r["RoundDate"]),
                        NextFollowUpDate = r["NextFollowUpDate"] == DBNull.Value ? null : Convert.ToDateTime(r["NextFollowUpDate"]),
                        StatusCode = r["StatusCode"]?.ToString()
                    }).ToList()
                    : new List<NegotiationRoundInfo>();

                var reviews = ds.Tables.Count > 4
                    ? ds.Tables[4].Rows.Cast<DataRow>().Select(r => new ManagementReviewInfo
                    {
                        ManagementReviewId = Convert.ToInt32(r["ManagementReviewId"]),
                        Ref_OpportunityQuotationId = r["Ref_OpportunityQuotationId"] == DBNull.Value ? null : Convert.ToInt32(r["Ref_OpportunityQuotationId"]),
                        Ref_OpportunityNegotiationId = r["Ref_OpportunityNegotiationId"] == DBNull.Value ? null : Convert.ToInt32(r["Ref_OpportunityNegotiationId"]),
                        Margin = r["Margin"] == DBNull.Value ? null : Convert.ToDecimal(r["Margin"]),
                        Remarks = r["Remarks"] == DBNull.Value ? null : r["Remarks"].ToString(),
                        DecisionCode = r["DecisionCode"]?.ToString(),
                        ReviewedByEmployeeId = r["ReviewedByEmployeeId"] == DBNull.Value ? null : Convert.ToInt32(r["ReviewedByEmployeeId"]),
                        ReviewedOn = r["ReviewedOn"] == DBNull.Value ? null : Convert.ToDateTime(r["ReviewedOn"]),
                        CreatedOn = Convert.ToDateTime(r["CreatedOn"])
                    }).ToList()
                    : new List<ManagementReviewInfo>();

                return new ApiResponse
                {
                    Status = true,
                    Message = "Quotation details fetched successfully.",
                    Data = new QuotationDetailResponse
                    {
                        Quotation = quotation,
                        Items = items,
                        Attachments = attachments,
                        NegotiationRounds = rounds,
                        Reviews = reviews
                    }
                };
            }
            catch (SqlException ex)
            {
                return new ApiResponse { Status = false, Message = $"Database Error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Status = false, Message = $"Error: {ex.Message}" };
            }
        }

        
        // LOG_CUSTOMER_RESPONSE
        
        public async Task<ApiResponse> LogCustomerResponse(LogCustomerResponseRequest model)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status", "LOG_CUSTOMER_RESPONSE"),
                    new SqlParameter("@Ref_OpportunityId", model.Ref_OpportunityId),
                    new SqlParameter("@Ref_OpportunityQuotationId", model.Ref_OpportunityQuotationId),
                    new SqlParameter("@CustomerResponseCode", model.CustomerResponseCode),
                    new SqlParameter("@CustomerResponseRemark", (object?)model.CustomerResponseRemark ?? DBNull.Value),
                    new SqlParameter("@CreatedBy", model.CreatedBy)
                );

                return MapResponse(dt);
            }
            catch (SqlException ex)
            {
                return new ApiResponse { Status = false, Message = $"Database Error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Status = false, Message = $"Error: {ex.Message}" };
            }
        }

        
        // LOG_NEGOTIATION_ROUND
        
        public async Task<ApiResponse> LogNegotiationRound(LogNegotiationRoundRequest model)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status", "LOG_NEGOTIATION_ROUND"),
                    new SqlParameter("@Ref_OpportunityId", model.Ref_OpportunityId),
                    new SqlParameter("@Ref_OpportunityQuotationId", model.Ref_OpportunityQuotationId),
                    new SqlParameter("@OurOfferAmount", (object?)model.OurOfferAmount ?? DBNull.Value),
                    new SqlParameter("@CustomerCounterAmount", (object?)model.CustomerCounterAmount ?? DBNull.Value),
                    new SqlParameter("@Remarks", (object?)model.Remarks ?? DBNull.Value),
                    new SqlParameter("@NextFollowUpDate", (object?)model.NextFollowUpDate ?? DBNull.Value),
                    new SqlParameter("@CreatedBy", model.CreatedBy)
                );

                return MapResponse(dt);
            }
            catch (SqlException ex)
            {
                return new ApiResponse { Status = false, Message = $"Database Error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Status = false, Message = $"Error: {ex.Message}" };
            }
        }

        
        // DECIDE_MANAGEMENT_REVIEW
        
        public async Task<ApiResponse> DecideManagementReview(DecideManagementReviewRequest model)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status", "DECIDE_MANAGEMENT_REVIEW"),
                    new SqlParameter("@Ref_ManagementReviewId", model.Ref_ManagementReviewId),
                    new SqlParameter("@Margin", (object?)model.Margin ?? DBNull.Value),
                    new SqlParameter("@DecisionCode", model.DecisionCode),
                    new SqlParameter("@Remarks", (object?)model.Remarks ?? DBNull.Value),
                    new SqlParameter("@ReviewedByEmployeeId", (object?)model.ReviewedByEmployeeId ?? DBNull.Value),
                    new SqlParameter("@CreatedBy", model.CreatedBy)
                );

                return MapResponse(dt);
            }
            catch (SqlException ex)
            {
                return new ApiResponse { Status = false, Message = $"Database Error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Status = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<ApiResponse> GetRaiseQuotationData(int opportunityId)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status", "GET_RAISE_QUOTATION_DATA"),
                    new SqlParameter("@Ref_OpportunityId", opportunityId)
                );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "No requirements found for this opportunity."
                    };
                }

                //var items = dt.AsEnumerable().Select(r => new
                //{
                //    OpportunityRequirementId = Convert.ToInt32(r["OpportunityRequirementId"]),
                //    ProductId = Convert.ToInt32(r["Ref_ProductId"]),
                //    ProductName = r["ProductName"]?.ToString(),
                //    Quantity = Convert.ToDecimal(r["Quantity"]),
                //    UnitId = r["Ref_UnitId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["Ref_UnitId"]),
                //    UnitName = r["UnitName"]?.ToString(),
                //    Rate = 0m
                //}).ToList();

                var items = dt.AsEnumerable()
                  .Select(r => new RaiseQuotationItemResponse
                  {
                      OpportunityRequirementId = Convert.ToInt32(r["OpportunityRequirementId"]),
                      ProductId = Convert.ToInt32(r["Ref_ProductId"]),
                      ProductName = r["ProductName"]?.ToString(),
                      Quantity = Convert.ToDecimal(r["Quantity"]),
                      UnitId = r["Ref_UnitId"] == DBNull.Value ? null : Convert.ToInt32(r["Ref_UnitId"]),
                      UnitName = r["UnitName"]?.ToString(),
                      Rate = 0
                  }).ToList();

                return new ApiResponse
                {
                    Status = true,
                    Message = "Quotation data fetched successfully.",
                    Data = items
                };
            }
            catch (SqlException ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
    }
}