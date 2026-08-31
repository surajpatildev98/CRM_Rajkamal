using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Services;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using static CRMWebServices.Models.OpportunityModels;
using static CRMWebServices.Models.OpportunityQuotationModels;
using static CRMWebServices.Models.PermissionModels;

namespace CRMWebServices.Repository
{
    public class OpportunityQuotationRepository : IOpportunityQuotationRepository
    {
        private readonly IDatabaseService _database;
        private readonly ILogger<PermissionService> _logger;
        private readonly ISchedulerExecutionService _schedulerExecutionService;

        private const string SP_NAME = "USP_CRM_OpportunityQuotationManagement";

        public OpportunityQuotationRepository(IDatabaseService database, ILogger<PermissionService> logger,ISchedulerExecutionService schedulerExecutionService)
        {
            _database = database;
            _logger = logger;
            _schedulerExecutionService = schedulerExecutionService;
        }

        #region  Helper Methods
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

        #endregion

        #region// Get Data for Raise Quotation Page   
        public async Task<ApiResponse> GetRaiseQuotationData(int opportunityId)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync(SP_NAME,
                    new SqlParameter("@Status", "GET_RAISE_QUOTATION_DATA"),
                    new SqlParameter("@Ref_OpportunityId", opportunityId)
                );

                if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Opportunity not found."
                    };
                }

                var row = ds.Tables[0].Rows[0];

                var header = new RaiseQuotationHeader
                {
                    OpportunityId = Convert.ToInt32(row["OpportunityId"]),
                    OpportunityNo = row["OpportunityNo"].ToString(),
                    CompanyName = row["CompanyName"].ToString(),
                    OwnerId = Convert.ToInt32(row["OwnerId"]),
                    OwnerName = row["OwnerName"].ToString()
                };

                var items = ds.Tables[1].Rows.Cast<DataRow>()
                    .Select(r => new RaiseQuotationItemResponse
                    {
                        OpportunityRequirementId = Convert.ToInt32(r["OpportunityRequirementId"]),
                        //Ref_LeadRequirementId = Convert.ToInt32(r["Ref_LeadRequirementId"]),
                        Ref_LeadRequirementId = r["Ref_LeadRequirementId"] == DBNull.Value ? null : Convert.ToInt32(r["Ref_LeadRequirementId"]),
                        ProductId = Convert.ToInt32(r["Ref_ProductId"]),
                        ProductName = r["ProductName"].ToString(),
                        Quantity = Convert.ToDecimal(r["Quantity"]),
                        UnitId = r["Ref_UnitId"] == DBNull.Value ? null : Convert.ToInt32(r["Ref_UnitId"]),
                        UnitName = r["UnitName"] == DBNull.Value ? null : r["UnitName"].ToString(),
                        RequirementRemark = r["RequirementRemark"] == DBNull.Value ? null : r["RequirementRemark"].ToString()
                    }).ToList();

                return new ApiResponse
                {
                    Status = true,
                    Message = "Quotation data fetched successfully.",
                    Data = new RaiseQuotationPageResponse
                    {
                        Header = header,
                        Items = items
                    }
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
        #endregion

        #region // RAISE_QUOTATION      
        public async Task<ApiResponse> RaiseQuotation(RaiseQuotationRequest model)
        {
            try
            {
                List<object> savedAttachments = new List<object>();

                if (!string.IsNullOrWhiteSpace(model.QuotationPdfBase64) && !string.IsNullOrWhiteSpace(model.QuotationPdfFileName))
                {
                    try
                    {
                        string savedPath = SaveBase64Attachment(model.QuotationPdfBase64, model.QuotationPdfFileName, "OpportunityQuotationAttachments");

                        savedAttachments.Add(new
                        {
                            FileName = model.QuotationPdfFileName,
                            FilePath = savedPath,
                            FileType = Path.GetExtension(model.QuotationPdfFileName).TrimStart('.').ToLowerInvariant(),
                            FileSize = Convert.FromBase64String(model.QuotationPdfBase64.Contains(",")
                                    ? model.QuotationPdfBase64.Substring(model.QuotationPdfBase64.IndexOf(",") + 1)
                                    : model.QuotationPdfBase64).Length
                        });
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse { Status = false, Message = $"Error processing quotation PDF: {ex.Message}" };
                    }
                }

                string? attachmentsJson = savedAttachments.Count > 0 ? JsonConvert.SerializeObject(savedAttachments) : null;
                string itemsJson = JsonConvert.SerializeObject(model.Items);

                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status", "RAISE_QUOTATION"),
                    new SqlParameter("@Ref_OpportunityId", model.Ref_OpportunityId),
                    new SqlParameter("@QuotationDate", (object?)model.QuotationDate ?? DBNull.Value),
                    new SqlParameter("@ValidUntil", (object?)model.ValidUntil ?? DBNull.Value),
                    new SqlParameter("@FreightAmount", (object?)model.FreightAmount ?? DBNull.Value),
                    new SqlParameter("@FreightPercent", (object?)model.FreightPercent ?? DBNull.Value),
                    new SqlParameter("@TaxAmount", (object?)model.TaxAmount ?? DBNull.Value),
                    new SqlParameter("@TaxPercent", (object?)model.TaxPercent ?? DBNull.Value),
                    new SqlParameter("@TermsRemark", (object?)model.TermsRemark ?? DBNull.Value),
                    new SqlParameter("@ItemsJson", itemsJson),
                    new SqlParameter("@AttachmentsMeta", (object?)attachmentsJson ?? DBNull.Value),
                    new SqlParameter("@CreatedBy", model.CreatedBy)
                );

                var response = MapResponse(dt);
                if (response.Status)
                {
                    int opportunityId = model.Ref_OpportunityId;
                    string quotationNo = Convert.ToString(dt.Rows[0]["QuotationNo"]);

                    int managerEmployeeId =
                        Convert.ToInt32(dt.Rows[0]["ManagerEmployeeId"]);

                    if (managerEmployeeId > 0)
                    {
                        try
                        {
                            await _schedulerExecutionService.SendEmployeeNotificationAsync(
                                schedulerJobItemId: null,
                                employeeId: managerEmployeeId,
                                entityId: opportunityId,
                                title: "Quotation Raised",
                                message: $"Quotation {quotationNo} has been raised.",
                                entityType: "OPPORTUNITY",
                                recipientType: "MANAGER"
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                "Failed to send quotation notification to Manager. OpportunityId {OpportunityId}, ManagerEmployeeId {ManagerEmployeeId}",
                                opportunityId,
                                managerEmployeeId);
                        }
                    }
                }

                return response;
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
        #endregion

        #region // GET_QUOTATION_LIST
        public async Task<ApiResponse> GetQuotationList(QuotationListRequest request)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status", "GET_QUOTATION_LIST"),
                    new SqlParameter("@SearchText", (object?)request.SearchText ?? DBNull.Value),
                    new SqlParameter("@FilterOwnerId", (object?)request.FilterOwnerId ?? DBNull.Value),
                    new SqlParameter("@Ref_OpportunityId", (object?)request.Ref_OpportunityId ?? DBNull.Value),
                    new SqlParameter("@Ref_RaisedByEmployeeId", (object?)request.Ref_RaisedByEmployeeId ?? DBNull.Value),
                    new SqlParameter("@OnlyPendingReview", request.OnlyPendingReview)
                );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse { Status = true, Message = "No quotations found.", Data = new List<QuotationListResponse>() };
                }

                var result = new List<QuotationListResponse>();
                foreach (DataRow row in dt.Rows)
                {
                    result.Add(new QuotationListResponse
                    {
                        OpportunityQuotationId = Convert.ToInt32(row["OpportunityQuotationId"]),
                        Ref_OpportunityId = Convert.ToInt32(row["Ref_OpportunityId"]),
                        OpportunityNo = row["OpportunityNo"]?.ToString(),
                        OpportunityName = row["OpportunityName"]?.ToString(),
                        CompanyName = row["CompanyName"]?.ToString(),
                        QuotationNo = row["QuotationNo"]?.ToString(),
                        VersionNumber = Convert.ToInt32(row["VersionNumber"]),
                        RevisionNumber = Convert.ToInt32(row["RevisionNumber"]),
                        RootQuotationNo = row["RootQuotationNo"] == DBNull.Value ? null : row["RootQuotationNo"].ToString(),
                        Ref_PreviousQuotationId = row["Ref_PreviousQuotationId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_PreviousQuotationId"]),
                        QuotationDate = Convert.ToDateTime(row["QuotationDate"]),
                        ValidUntil = row["ValidUntil"] == DBNull.Value ? null : Convert.ToDateTime(row["ValidUntil"]),
                        SubTotal = Convert.ToDecimal(row["SubTotal"]),
                        FreightCharge = Convert.ToDecimal(row["FreightCharge"]),
                        TaxPercent = Convert.ToDecimal(row["TaxPercent"]),
                        TotalAmount = Convert.ToDecimal(row["TotalAmount"]),
                        CustomerResponseCode = row["CustomerResponseCode"]?.ToString(),
                        IsCurrent = Convert.ToBoolean(row["IsCurrent"]),
                        CreatedOn = Convert.ToDateTime(row["CreatedOn"]),
                        RaisedBy = row["RaisedBy"]?.ToString(),
                        OwnerEmployeeId = row["OwnerEmployeeId"] == DBNull.Value ? null : Convert.ToInt32(row["OwnerEmployeeId"]),
                        SentByEmployeeId = row["SentByEmployeeId"] == DBNull.Value ? null : Convert.ToInt32(row["SentByEmployeeId"]),
                        SentByEmployeeName = row["SentByEmployeeName"] == DBNull.Value ? null : row["SentByEmployeeName"]?.ToString(),
                        OpportunityStageId = row["OpportunityStageId"] == DBNull.Value ? null : Convert.ToInt32(row["OpportunityStageId"]),
                        OpportunityStageCode = row["OpportunityStageCode"] == DBNull.Value ? null : row["OpportunityStageCode"].ToString(),
                        OpportunityStageName = row["OpportunityStageName"] == DBNull.Value ? null : row["OpportunityStageName"].ToString(),
                    });
                }

                return new ApiResponse { Status = true, Message = "Quotations fetched successfully.", Data = result };
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
        #endregion

        #region // GET_QUOTATION_DETAILS    
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
                    Ref_PreviousQuotationId = qRow["Ref_PreviousQuotationId"] == DBNull.Value ? null : Convert.ToInt32(qRow["Ref_PreviousQuotationId"]),
                    RootQuotationNo = qRow["RootQuotationNo"] == DBNull.Value ? null : qRow["RootQuotationNo"].ToString(),
                    RevisionNumber = Convert.ToInt32(qRow["RevisionNumber"]),
                    QuotationNo = qRow["QuotationNo"]?.ToString(),
                    VersionNumber = Convert.ToInt32(qRow["VersionNumber"]),
                    IsCurrent = Convert.ToBoolean(qRow["IsCurrent"]),
                    QuotationDate = Convert.ToDateTime(qRow["QuotationDate"]),
                    ValidUntil = qRow["ValidUntil"] == DBNull.Value ? null : Convert.ToDateTime(qRow["ValidUntil"]),
                    FreightCharge = qRow["FreightCharge"] == DBNull.Value ? 0 : Convert.ToDecimal(qRow["FreightCharge"]),
                    FreightPercent = qRow["FreightPercent"] == DBNull.Value ? 0 : Convert.ToDecimal(qRow["FreightPercent"]),
                    TaxPercent = qRow["TaxPercent"] == DBNull.Value ? 0 : Convert.ToDecimal(qRow["TaxPercent"]),
                    TaxAmount = qRow["TaxAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(qRow["TaxAmount"]),
                    SubTotal = Convert.ToDecimal(qRow["SubTotal"]),
                    TotalAmount = Convert.ToDecimal(qRow["TotalAmount"]),
                    TermsRemark = qRow["TermsRemark"] == DBNull.Value ? null : qRow["TermsRemark"].ToString(),
                    SentByEmployeeId = qRow["SentByEmployeeId"] == DBNull.Value ? null : Convert.ToInt32(qRow["SentByEmployeeId"]),
                    SentByEmployeeName = qRow["SentByEmployeeName"] == DBNull.Value ? null : qRow["SentByEmployeeName"]?.ToString(),
                    SentOn = qRow["SentOn"] == DBNull.Value ? null : Convert.ToDateTime(qRow["SentOn"]),
                    // OWNER
                    Owner = new OwnerInfo
                    {
                        OwnerEmployeeId = qRow["OwnerEmployeeId"] == DBNull.Value ? null : Convert.ToInt32(qRow["OwnerEmployeeId"]),
                        OwnerEmployeeName = qRow["OwnerEmployeeName"] == DBNull.Value ? null : qRow["OwnerEmployeeName"]?.ToString(),
                        OwnerEmail = qRow["OwnerEmail"] == DBNull.Value ? null : qRow["OwnerEmail"]?.ToString(),
                        OwnerMobileNo = qRow["OwnerMobileNo"] == DBNull.Value ? null : qRow["OwnerMobileNo"]?.ToString(),
                        OwnerRoleId = qRow["OwnerRoleId"] == DBNull.Value ? null : Convert.ToInt32(qRow["OwnerRoleId"]),
                        OwnerRoleName = qRow["OwnerRoleName"] == DBNull.Value ? null : qRow["OwnerRoleName"]?.ToString()
                    },
                    CustomerResponseCode = qRow["CustomerResponseCode"]?.ToString(),
                    CustomerResponseByEmployeeId = qRow["CustomerResponseByEmployeeId"] == DBNull.Value ? null : Convert.ToInt32(qRow["CustomerResponseByEmployeeId"]),
                    CustomerResponseOn = qRow["CustomerResponseOn"] == DBNull.Value ? null : Convert.ToDateTime(qRow["CustomerResponseOn"]),
                    CustomerResponseRemark = qRow["CustomerResponseRemark"] == DBNull.Value ? null : qRow["CustomerResponseRemark"].ToString(),

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

                var history = ds.Tables.Count > 3
                  ? ds.Tables[3].Rows.Cast<DataRow>()
                      .Select(r => new QuotationHistoryInfo
                      {
                          OpportunityQuotationId =
                              Convert.ToInt32(r["OpportunityQuotationId"]),

                          Ref_OpportunityId =
                              Convert.ToInt32(r["Ref_OpportunityId"]),

                          Ref_PreviousQuotationId =
                              r["Ref_PreviousQuotationId"] == DBNull.Value
                                  ? null
                                  : Convert.ToInt32(r["Ref_PreviousQuotationId"]),

                          QuotationNo =
                              r["QuotationNo"]?.ToString(),

                          VersionNumber =
                              Convert.ToInt32(r["VersionNumber"]),

                          RevisionNumber =
                              Convert.ToInt32(r["RevisionNumber"]),

                          RootQuotationNo =
                              r["RootQuotationNo"] == DBNull.Value
                                  ? null
                                  : r["RootQuotationNo"].ToString(),

                          IsCurrent =
                              Convert.ToBoolean(r["IsCurrent"]),

                          QuotationDate =
                              Convert.ToDateTime(r["QuotationDate"]),

                          ValidUntil =
                              r["ValidUntil"] == DBNull.Value
                                  ? null
                                  : Convert.ToDateTime(r["ValidUntil"]),

                          SubTotal =
                              r["SubTotal"] == DBNull.Value
                                  ? 0
                                  : Convert.ToDecimal(r["SubTotal"]),

                          FreightCharge =
                              r["FreightCharge"] == DBNull.Value
                                  ? 0
                                  : Convert.ToDecimal(r["FreightCharge"]),

                          FreightPercent =
                              r["FreightPercent"] == DBNull.Value
                                  ? 0
                                  : Convert.ToDecimal(r["FreightPercent"]),

                          TaxAmount =
                              r["TaxAmount"] == DBNull.Value
                                  ? 0
                                  : Convert.ToDecimal(r["TaxAmount"]),

                          TaxPercent =
                              r["TaxPercent"] == DBNull.Value
                                  ? 0
                                  : Convert.ToDecimal(r["TaxPercent"]),

                          TotalAmount =
                              r["TotalAmount"] == DBNull.Value
                                  ? 0
                                  : Convert.ToDecimal(r["TotalAmount"]),

                          TermsRemark =
                              r["TermsRemark"] == DBNull.Value
                                  ? null
                                  : r["TermsRemark"].ToString(),

                          SentByEmployeeId =
                              r["SentByEmployeeId"] == DBNull.Value
                                  ? null
                                  : Convert.ToInt32(r["SentByEmployeeId"]),

                          SentOn =
                              r["SentOn"] == DBNull.Value
                                  ? null
                                  : Convert.ToDateTime(r["SentOn"]),

                          CustomerResponseCode =
                              r["CustomerResponseCode"]?.ToString(),


                          CustomerResponseByEmployeeId =
                              r["CustomerResponseByEmployeeId"] == DBNull.Value
                                  ? null
                                  : Convert.ToInt32(r["CustomerResponseByEmployeeId"]),

                          CustomerResponseOn =
                              r["CustomerResponseOn"] == DBNull.Value
                                  ? null
                                  : Convert.ToDateTime(r["CustomerResponseOn"]),

                          CustomerResponseRemark =
                              r["CustomerResponseRemark"] == DBNull.Value
                                  ? null
                                  : r["CustomerResponseRemark"].ToString(),

                          CreatedOn =
                              Convert.ToDateTime(r["CreatedOn"]),

                          CreatedByName =
                              r["CreatedByName"] == DBNull.Value
                                  ? null
                                  : r["CreatedByName"].ToString(),

                          OpportunityStageId =
                            r["OpportunityStageId"] == DBNull.Value
                                ? null
                                : Convert.ToInt32(r["OpportunityStageId"]),

                          OpportunityStageCode =
                            r["OpportunityStageCode"] == DBNull.Value
                                ? null
                                : r["OpportunityStageCode"].ToString(),

                          OpportunityStageName =
                            r["OpportunityStageName"] == DBNull.Value
                                ? null
                                : r["OpportunityStageName"].ToString()

                      }).ToList()
                  : new List<QuotationHistoryInfo>();

                var historyAttachments = ds.Tables.Count > 5
                        ? ds.Tables[5].Rows.Cast<DataRow>()
                            .Select(r => new
                            {
                                Attachment = new QuotationAttachmentInfo
                                {
                                    AttachmentId =
                                        Convert.ToInt32(r["AttachmentId"]),

                                    Ref_OpportunityQuotationId =
                                        Convert.ToInt32(r["Ref_OpportunityQuotationId"]),

                                    FileName =
                                        r["FileName"]?.ToString()!,

                                    FileType =
                                        r["FileType"] == DBNull.Value
                                            ? null
                                            : r["FileType"].ToString(),

                                    FileSize =
                                        r["FileSize"] == DBNull.Value
                                            ? null
                                            : Convert.ToInt64(r["FileSize"]),

                                    FileUrl =
                                        r["FilePath"] == DBNull.Value
                                            ? null
                                            : BuildFileUrl(r["FilePath"]?.ToString()),

                                    CreatedOn =
                                        Convert.ToDateTime(r["CreatedOn"])
                                },

                                QuotationId =
                                    Convert.ToInt32(r["Ref_OpportunityQuotationId"])
                            })
                            .ToList()
                        : new();

                foreach (var item in history)
                {
                    item.Attachments = historyAttachments
                        .Where(x => x.QuotationId == item.OpportunityQuotationId)
                        .Select(x => x.Attachment)
                        .ToList();
                }

                //var reviews = ds.Tables.Count > 4
                //    ? ds.Tables[4].Rows.Cast<DataRow>().Select(r => new ManagementReviewInfo
                //    {
                //        ManagementReviewId = Convert.ToInt32(r["ManagementReviewId"]),
                //        Ref_OpportunityQuotationId = r["Ref_OpportunityQuotationId"] == DBNull.Value ? null : Convert.ToInt32(r["Ref_OpportunityQuotationId"]),
                //        Ref_OpportunityNegotiationId = r["Ref_OpportunityNegotiationId"] == DBNull.Value ? null : Convert.ToInt32(r["Ref_OpportunityNegotiationId"]),
                //        Margin = r["Margin"] == DBNull.Value ? null : Convert.ToDecimal(r["Margin"]),
                //        Remarks = r["Remarks"] == DBNull.Value ? null : r["Remarks"].ToString(),
                //        DecisionCode = r["DecisionCode"]?.ToString(),
                //        ReviewedByEmployeeId = r["ReviewedByEmployeeId"] == DBNull.Value ? null : Convert.ToInt32(r["ReviewedByEmployeeId"]),
                //        ReviewedOn = r["ReviewedOn"] == DBNull.Value ? null : Convert.ToDateTime(r["ReviewedOn"]),
                //        CreatedOn = Convert.ToDateTime(r["CreatedOn"])
                //    }).ToList()
                //    : new List<ManagementReviewInfo>();

                var reviews = ds.Tables.Count > 4
                    ? ds.Tables[4].Rows.Cast<DataRow>()
                        .Select(r => new ManagementReviewInfo
                        {
                            ManagementReviewId =
                                Convert.ToInt32(r["ManagementReviewId"]),

                            Ref_OpportunityQuotationId =
                                r["Ref_OpportunityQuotationId"] == DBNull.Value
                                    ? null
                                    : Convert.ToInt32(r["Ref_OpportunityQuotationId"]),

                            Margin =
                                r["Margin"] == DBNull.Value
                                    ? null
                                    : Convert.ToDecimal(r["Margin"]),

                            Remarks =
                                r["Remarks"] == DBNull.Value
                                    ? null
                                    : r["Remarks"].ToString(),

                            DecisionCode =
                                r["DecisionCode"]?.ToString(),

                            ReviewedByEmployeeId =
                                r["ReviewedByEmployeeId"] == DBNull.Value
                                    ? null
                                    : Convert.ToInt32(r["ReviewedByEmployeeId"]),

                            ReviewedOn =
                                r["ReviewedOn"] == DBNull.Value
                                    ? null
                                    : Convert.ToDateTime(r["ReviewedOn"]),

                            CreatedOn =
                                Convert.ToDateTime(r["CreatedOn"])
                        })
                        .ToList()
                    : new List<ManagementReviewInfo>();

                return new ApiResponse
                {
                    Status = true,
                    Message = "Quotation details fetched successfully.",
                    //Data = new QuotationDetailResponse
                    //{
                    //    Quotation = quotation,
                    //    Items = items,
                    //    Attachments = attachments,
                    //    NegotiationRounds = rounds,
                    //    Reviews = reviews
                    //}
                    Data = new QuotationDetailResponse
                    {
                        Quotation = quotation,
                        Items = items,
                        Attachments = attachments,
                        History = history,
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
        #endregion

        #region // LOG_CUSTOMER_RESPONSE        
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
        #endregion

        #region // LOG_NEGOTIATION_ROUND    
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
        #endregion

        #region // DECIDE_MANAGEMENT_REVIEW        
        //public async Task<ApiResponse> DecideManagementReview(DecideManagementReviewRequest model)
        //{
        //    try
        //    {
        //        var dt = await _database.ExecuteDataTableAsync(SP_NAME,
        //            new SqlParameter("@Status", "DECIDE_MANAGEMENT_REVIEW"),
        //            new SqlParameter("@Ref_ManagementReviewId", model.Ref_ManagementReviewId),
        //            new SqlParameter("@Margin", (object?)model.Margin ?? DBNull.Value),
        //            new SqlParameter("@DecisionCode", model.DecisionCode),
        //            new SqlParameter("@Remarks", (object?)model.Remarks ?? DBNull.Value),
        //            new SqlParameter("@ReviewedByEmployeeId", (object?)model.ReviewedByEmployeeId ?? DBNull.Value),
        //            new SqlParameter("@CreatedBy", model.CreatedBy)
        //        );

        //        return MapResponse(dt);
        //    }
        //    catch (SqlException ex)
        //    {
        //        return new ApiResponse { Status = false, Message = $"Database Error: {ex.Message}" };
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ApiResponse { Status = false, Message = $"Error: {ex.Message}" };
        //    }
        //}
        #endregion

        #region // DECIDE_MANAGEMENT_REVIEW

        public async Task<ApiResponse> DecideManagementReview(DecideManagementReviewRequest model)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status", "DECIDE_MANAGEMENT_REVIEW"),
                    new SqlParameter("@QuotationId", model.QuotationId),
                    new SqlParameter("@DecisionCode", model.DecisionCode),
                    new SqlParameter("@Remarks", (object?)model.Remarks ?? DBNull.Value),
                    new SqlParameter("@ReviewedByEmployeeId", (object?)model.ReviewedByEmployeeId ?? DBNull.Value),
                    new SqlParameter("@CreatedBy", model.CreatedBy)
                );

                // return MapResponse(dt);
                var response = MapResponse(dt);

                if (response.Status && dt.Rows.Count > 0)
                {
                    int quotationRaisedByEmployeeId =
                        Convert.ToInt32(
                            dt.Rows[0]["QuotationRaisedByEmployeeId"]);

                    int opportunityId =
                        Convert.ToInt32(
                            dt.Rows[0]["OpportunityId"]);

                    string quotationNo =
                        Convert.ToString(
                            dt.Rows[0]["QuotationNo"]);

                    string decisionCode =
                        Convert.ToString(
                            dt.Rows[0]["DecisionCode"]);

                    if (quotationRaisedByEmployeeId > 0)
                    {
                        try
                        {
                            string title;
                            string message;

                            if (decisionCode == "APPROVED")
                            {
                                title = "Quotation Approved";

                                message =
                                    $"Quotation {quotationNo} has been approved by Management.";
                            }
                            else
                            {
                                title = "Quotation Rejected";

                                message =
                                    $"Quotation {quotationNo} has been rejected by Management.";
                            }

                            await _schedulerExecutionService
                                .SendEmployeeNotificationAsync(
                                    schedulerJobItemId: null,
                                    employeeId: quotationRaisedByEmployeeId,
                                    entityId: opportunityId,
                                    title: title,
                                    message: message,
                                    entityType: "OPPORTUNITY",
                                    recipientType: "QUOTATION_RAISED_BY"
                                );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                "Failed to send Management Review notification. " +
                                "QuotationId {QuotationId}, " +
                                "QuotationNo {QuotationNo}, " +
                                "EmployeeId {EmployeeId}, " +
                                "Decision {DecisionCode}",
                                model.QuotationId,
                                quotationNo,
                                quotationRaisedByEmployeeId,
                                decisionCode);
                        }
                    }
                }

                return response;

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

        #endregion

        #region // REVISE_QUOTATION

        public async Task<ApiResponse> ReviseQuotation(ReviseQuotationRequest model)
        {
            try
            {
                List<object> savedAttachments = new List<object>();

        
                // Save quotation PDF if provided
        
                if (!string.IsNullOrWhiteSpace(model.QuotationPdfBase64) &&
                    !string.IsNullOrWhiteSpace(model.QuotationPdfFileName))
                {
                    try
                    {
                        string savedPath = SaveBase64Attachment(
                            model.QuotationPdfBase64,
                            model.QuotationPdfFileName,
                            "OpportunityQuotationAttachments"
                        );

                        string base64Data = model.QuotationPdfBase64.Contains(",")
                            ? model.QuotationPdfBase64.Substring(
                                model.QuotationPdfBase64.IndexOf(",") + 1)
                            : model.QuotationPdfBase64;

                        savedAttachments.Add(new
                        {
                            FileName = model.QuotationPdfFileName,
                            FilePath = savedPath,
                            FileType = Path.GetExtension(
                                model.QuotationPdfFileName)
                                .TrimStart('.')
                                .ToLowerInvariant(),

                            FileSize = Convert.FromBase64String(base64Data).Length
                        });
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse
                        {
                            Status = false,
                            Message =
                                $"Error processing quotation PDF: {ex.Message}"
                        };
                    }
                }

          
                // Serialize request data
         
                string? attachmentsJson =
                    savedAttachments.Count > 0
                        ? JsonConvert.SerializeObject(savedAttachments)
                        : null;

                string itemsJson =
                    JsonConvert.SerializeObject(model.Items);


  
                // Call SP
          
                var dt = await _database.ExecuteDataTableAsync(
                    SP_NAME,

                    new SqlParameter(
                        "@Status",
                        "REVISE_QUOTATION"
                    ),

                    new SqlParameter(
                        "@Ref_OpportunityQuotationId",
                        model.Ref_OpportunityQuotationId
                    ),

                    new SqlParameter(
                        "@QuotationDate",
                        (object?)model.QuotationDate ?? DBNull.Value
                    ),

                    new SqlParameter(
                        "@ValidUntil",
                        (object?)model.ValidUntil ?? DBNull.Value
                    ),

                    new SqlParameter(
                        "@FreightAmount",
                        (object?)model.FreightAmount ?? DBNull.Value
                    ),

                    new SqlParameter(
                        "@FreightPercent",
                        (object?)model.FreightPercent ?? DBNull.Value
                    ),

                    new SqlParameter(
                        "@TaxAmount",
                        (object?)model.TaxAmount ?? DBNull.Value
                    ),

                    new SqlParameter(
                        "@TaxPercent",
                        (object?)model.TaxPercent ?? DBNull.Value
                    ),

                    new SqlParameter(
                        "@TermsRemark",
                        (object?)model.TermsRemark ?? DBNull.Value
                    ),

                    new SqlParameter(
                        "@ItemsJson",
                        itemsJson
                    ),

                    new SqlParameter(
                        "@AttachmentsMeta",
                        (object?)attachmentsJson ?? DBNull.Value
                    ),

                    new SqlParameter(
                        "@CreatedBy",
                        model.CreatedBy
                    )
                );

                var response = MapResponse(dt);

                if (response.Status)
                {
                    int opportunityId =
                        Convert.ToInt32(dt.Rows[0]["OpportunityId"]);
                    string quotationNo = Convert.ToString(dt.Rows[0]["QuotationNo"]);

                    int managerEmployeeId =
                        Convert.ToInt32(dt.Rows[0]["ManagerEmployeeId"]);

                    // ============================================
                    // NOTIFY MANAGER - QUOTATION REVISED
                    // ============================================

                    if (managerEmployeeId > 0)
                    {
                        try
                        {
                            await _schedulerExecutionService.SendEmployeeNotificationAsync(
                                schedulerJobItemId: null,
                                employeeId: managerEmployeeId,
                                entityId: opportunityId,
                                title: "Quotation Revised",
                                message: $"Quotation {quotationNo} has been revised.",
                                entityType: "OPPORTUNITY",
                                recipientType: "MANAGER"
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                "Failed to send revised quotation notification to Manager. OpportunityId {OpportunityId}, ManagerEmployeeId {ManagerEmployeeId}",
                                opportunityId,
                                managerEmployeeId);
                        }
                    }
                }

                return response;
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

        #endregion
    }
}