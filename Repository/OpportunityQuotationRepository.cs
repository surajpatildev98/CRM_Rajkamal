using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using CRMWebServices.Services;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
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
        private readonly IConfiguration _config;
        private readonly ILogger<PermissionService> _logger;
        private readonly ISchedulerExecutionService _schedulerExecutionService;
        private readonly IEmailService _emailService;
        private readonly IEmailTemplateRenderer _templateRenderer;
        private const string SP_NAME = "USP_CRM_OpportunityQuotationManagement";

        public OpportunityQuotationRepository(IDatabaseService database, IConfiguration config, ILogger<PermissionService> logger, ISchedulerExecutionService schedulerExecutionService, IEmailService emailService, IEmailTemplateRenderer templateRenderer)
        {
            _database = database;
            _config = config;
            _logger = logger;
            _schedulerExecutionService = schedulerExecutionService;
            _emailService = emailService;
            _templateRenderer = templateRenderer;
        }

        #region  Helper Methods
        private string? BuildFileUrl(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            var relative = filePath.Replace("Uploads/", "").Replace("Uploads\\", "");
            var segments = relative.Split('/').Select(Uri.EscapeDataString);

           return "/Uploads/" + string.Join("/", segments);
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

        private static string Money(decimal value) => value.ToString("N2", CultureInfo.GetCultureInfo("en-IN"));
        #endregion

        #region Get Data for Raise Quotation Page           
        public async Task<ApiResponse> GetRaiseQuotationData(int opportunityId, int employeeId)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync(
                    SP_NAME,
                    new SqlParameter("@Status", "GET_RAISE_QUOTATION_DATA"),
                    new SqlParameter("@Ref_OpportunityId", opportunityId),
                    new SqlParameter("@CreatedBy", employeeId)
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
                    OpportunityNo = row["OpportunityNo"]?.ToString() ?? string.Empty,
                    CompanyName = row["CompanyName"]?.ToString() ?? string.Empty,
                    OwnerId = Convert.ToInt32(row["OwnerId"]),
                    OwnerName = row["OwnerName"]?.ToString() ?? string.Empty,
                    DeliveryPinCode = row["DeliveryPinCode"] == DBNull.Value? null: row["DeliveryPinCode"]?.ToString(),
                    DeliveryAddress = row["DeliveryAddress"] == DBNull.Value? null: row["DeliveryAddress"]?.ToString()
                };

                var items = ds.Tables.Count > 1 ? ds.Tables[1].Rows.Cast<DataRow>()
                        .Select(r => new RaiseQuotationItemResponse
                        {
                            OpportunityRequirementId = Convert.ToInt32(r["OpportunityRequirementId"]),
                            Ref_LeadRequirementId =r["Ref_LeadRequirementId"] == DBNull.Value? null: Convert.ToInt32(r["Ref_LeadRequirementId"]),
                            Ref_ProductCategoryId =r["Ref_ProductCategoryId"] == DBNull.Value? null: Convert.ToInt32(r["Ref_ProductCategoryId"]),
                            CategoryName =r["CategoryName"] == DBNull.Value? null: r["CategoryName"]?.ToString(),
                            Ref_OfferingId =r["Ref_OfferingId"] == DBNull.Value? null: Convert.ToInt32(r["Ref_OfferingId"]),
                            OfferingName =r["OfferingName"] == DBNull.Value? null: r["OfferingName"]?.ToString(),
                            ProductId =r["Ref_ProductId"] == DBNull.Value? null: Convert.ToInt64(r["Ref_ProductId"]),
                            ProductCode =r["ProductCode"] == DBNull.Value? null: r["ProductCode"]?.ToString(),
                            ProductName =r["ProductName"] == DBNull.Value? null: r["ProductName"]?.ToString(),
                            CurrentRate =r["CurrentRate"] == DBNull.Value? 0: Convert.ToDecimal(r["CurrentRate"]),
                            Quantity = Convert.ToDecimal(r["Quantity"]),
                            UnitId =r["Ref_UnitId"] == DBNull.Value? null: Convert.ToInt32(r["Ref_UnitId"]),
                            UnitName =r["UnitName"] == DBNull.Value? null: r["UnitName"]?.ToString(),
                            RequirementRemark =r["RequirementRemark"] == DBNull.Value? null: r["RequirementRemark"]?.ToString()
                        }).ToList(): new List<RaiseQuotationItemResponse>();

                var freightPolicySlabs = ds.Tables.Count > 2? ds.Tables[2].Rows.Cast<DataRow>()
                        .Select(r => new FreightPolicySlabQuotationResponse
                        {
                            FreightPolicyId = Convert.ToInt32(r["FreightPolicyId"]),
                            PolicyName =r["PolicyName"] == DBNull.Value? string.Empty: r["PolicyName"]?.ToString() ?? string.Empty,
                            VersionNo = Convert.ToInt32(r["VersionNo"]),
                            ODACharge =r["ODACharge"] == DBNull.Value? (decimal?)null: Convert.ToDecimal(r["ODACharge"]),
                            ODAMinOrderValue =r["ODAMinOrderValue"] == DBNull.Value? (decimal?)null: Convert.ToDecimal(r["ODAMinOrderValue"]),
                            FreightPolicySlabId =Convert.ToInt32(r["FreightPolicySlabId"]),
                            MinOrderValue =Convert.ToDecimal(r["MinOrderValue"]),
                            MaxOrderValue =r["MaxOrderValue"] == DBNull.Value? (decimal?)null: Convert.ToDecimal(r["MaxOrderValue"]),
                            FreightAmount =Convert.ToDecimal(r["FreightAmount"])
                        }).ToList(): new List<FreightPolicySlabQuotationResponse>();

                var categoryTermsAndConditions = ds.Tables.Count > 4? ds.Tables[4].Rows.Cast<DataRow>()
                        .Select(r => new RaiseQuotationCategoryTermsResponse
                        {
                            ProductCategoryId =Convert.ToInt32(r["ProductCategoryId"]),
                            CategoryCode =r["CategoryCode"] == DBNull.Value? null: r["CategoryCode"]?.ToString(),
                            CategoryName =r["CategoryName"] == DBNull.Value? null: r["CategoryName"]?.ToString(),
                            TermsAndConditions =r["TermsAndConditions"] == DBNull.Value? null: r["TermsAndConditions"]?.ToString()
                        }).ToList(): new List<RaiseQuotationCategoryTermsResponse>();

                RaiseQuotationRaiserOfficeResponse? raiserOffice = null;

                if (ds.Tables.Count > 5 && ds.Tables[5].Rows.Count > 0)
                {
                    var r = ds.Tables[5].Rows[0];

                    raiserOffice = new RaiseQuotationRaiserOfficeResponse
                    {
                        RaiserEmployeeId = Convert.ToInt32(r["RaiserEmployeeId"]),
                        OfficeId = Convert.ToInt32(r["OfficeId"]),
                        OfficeCode = r["OfficeCode"] == DBNull.Value? null: r["OfficeCode"]?.ToString(),
                        OfficeName = r["OfficeName"] == DBNull.Value? null: r["OfficeName"]?.ToString(),
                        OfficeShortName = r["OfficeShortName"] == DBNull.Value? null: r["OfficeShortName"]?.ToString(),
                        OfficeAddress = r["OfficeAddress"] == DBNull.Value? null: r["OfficeAddress"]?.ToString(),
                        OfficePincode = r["OfficePincode"] == DBNull.Value? null: r["OfficePincode"]?.ToString(),
                        OfficePhone = r["OfficePhone"] == DBNull.Value? null: r["OfficePhone"]?.ToString(),
                        OfficeEmail = r["OfficeEmail"] == DBNull.Value? null: r["OfficeEmail"]?.ToString(),
                        CountryName = r["CountryName"] == DBNull.Value? null: r["CountryName"]?.ToString(),
                        StateName = r["StateName"] == DBNull.Value? null: r["StateName"]?.ToString(),
                        CityName = r["CityName"] == DBNull.Value? null: r["CityName"]?.ToString()
                    };
                }

                return new ApiResponse
                {
                    Status = true,
                    Message = "Quotation data fetched successfully.",
                    Data = new RaiseQuotationPageResponse
                    {
                        Header = header,
                        Items = items,
                        FreightPolicySlabs = freightPolicySlabs,
                        CategoryTermsAndConditions = categoryTermsAndConditions,
                        RaiserOffice = raiserOffice
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

        #region  RAISE_QUOTATION NEW 230926
        public async Task<ApiResponse> RaiseQuotation(RaiseQuotationRequest model)
        {
            try
            {
                List<object> savedAttachments = new List<object>();

                if (!string.IsNullOrWhiteSpace(model.QuotationPdfBase64) &&
                    !string.IsNullOrWhiteSpace(model.QuotationPdfFileName))
                {
                    try
                    {
                        string savedPath = SaveBase64Attachment(model.QuotationPdfBase64,model.QuotationPdfFileName,"OpportunityQuotationAttachments");

                        savedAttachments.Add(new
                        {
                            FileName = model.QuotationPdfFileName,
                            FilePath = savedPath,
                            FileType = Path.GetExtension(model.QuotationPdfFileName).TrimStart('.').ToLowerInvariant(),
                            FileSize = Convert.FromBase64String(model.QuotationPdfBase64.Contains(",")? model.QuotationPdfBase64.Substring(model.QuotationPdfBase64.IndexOf(",") + 1): model.QuotationPdfBase64).Length
                        });
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse
                        {
                            Status = false,
                            Message = $"Error processing quotation PDF: {ex.Message}"
                        };
                    }
                }

                string? attachmentsJson =savedAttachments.Count > 0 ? JsonConvert.SerializeObject(savedAttachments): null;

                string itemsJson = JsonConvert.SerializeObject(model.Items);

                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status", "RAISE_QUOTATION"),
                    new SqlParameter("@Ref_OpportunityId", model.Ref_OpportunityId),
                    new SqlParameter("@DeliveryPartnerId",(object?)model.DeliveryPartnerId ?? DBNull.Value),
                    new SqlParameter("@DeliveryPinCode",(object?)model.DeliveryPinCode ?? DBNull.Value),
                    new SqlParameter("@QuotationDate",(object?)model.QuotationDate ?? DBNull.Value),
                    new SqlParameter("@ValidUntil",(object?)model.ValidUntil ?? DBNull.Value),
                    new SqlParameter("@TaxAmount",(object?)model.TaxAmount ?? DBNull.Value),
                    new SqlParameter("@TaxPercent",(object?)model.TaxPercent ?? DBNull.Value),
                    new SqlParameter("@TermsRemark",(object?)model.TermsRemark ?? DBNull.Value),
                    new SqlParameter("@ItemsJson", itemsJson),
                    new SqlParameter("@AttachmentsMeta",(object?)attachmentsJson ?? DBNull.Value),
                    new SqlParameter("@CreatedBy", model.CreatedBy)
                );

                var response = MapResponse(dt);

                //if (response.Status)
                //{
                //    int opportunityId = model.Ref_OpportunityId;

                //    string quotationNo =Convert.ToString(dt.Rows[0]["QuotationNo"]);
                //    int managerEmployeeId =Convert.ToInt32(dt.Rows[0]["ManagerEmployeeId"]);

                //    if (managerEmployeeId > 0)
                //    {
                //        try
                //        {
                //            await _schedulerExecutionService.SendEmployeeNotificationAsync(
                //                schedulerJobItemId: null,
                //                employeeId: managerEmployeeId,
                //                entityId: opportunityId,
                //                title: "Quotation Raised",
                //                message: $"Quotation {quotationNo} has been raised.",
                //                entityType: "OPPORTUNITY",
                //                recipientType: "MANAGER"
                //            );
                //        }
                //        catch (Exception ex)
                //        {
                //            _logger.LogError(
                //                ex,
                //                "Failed to send quotation notification to Manager. OpportunityId {OpportunityId}, ManagerEmployeeId {ManagerEmployeeId}",
                //                opportunityId,
                //                managerEmployeeId);
                //        }
                //    }
                //}

                if (response.Status)
                {
                    int opportunityId = model.Ref_OpportunityId;
                    int opportunityQuotationId =Convert.ToInt32(dt.Rows[0]["OpportunityQuotationId"]);
                    bool odaApprovalRequired =Convert.ToBoolean(dt.Rows[0]["ODAApprovalRequired"]);
                    string quotationNo =Convert.ToString(dt.Rows[0]["QuotationNo"]);
                    int managerEmployeeId =Convert.ToInt32(dt.Rows[0]["ManagerEmployeeId"]);

                    // ODA APPROVAL EMAIL

                    if (odaApprovalRequired)
                    {
                        try
                        {
                            await SendODAApprovalEmailAsync(opportunityQuotationId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,"Failed to send ODA approval email. QuotationId {QuotationId}",opportunityQuotationId);
                        }
                    }

                    // EXISTING MANAGER NOTIFICATION

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
                            _logger.LogError(ex,"Failed to send quotation notification to Manager. OpportunityId {OpportunityId}, ManagerEmployeeId {ManagerEmployeeId}",opportunityId,managerEmployeeId);
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

        #region  GET_QUOTATION_LIST
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

        #region  GET_QUOTATION_DETAILS    
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
                    Ref_FreightPolicyId = qRow["Ref_FreightPolicyId"] == DBNull.Value ? null : Convert.ToInt32(qRow["Ref_FreightPolicyId"]),
                    Ref_FreightPolicySlabId = qRow["Ref_FreightPolicySlabId"] == DBNull.Value ? null : Convert.ToInt32(qRow["Ref_FreightPolicySlabId"]),
                    FreightPolicyName = qRow["FreightPolicyName"] == DBNull.Value ? null : qRow["FreightPolicyName"].ToString(),
                    FreightPolicyVersion = qRow["FreightPolicyVersion"] == DBNull.Value ? null : Convert.ToInt32(qRow["FreightPolicyVersion"]),
                    FreightPolicyMinOrderValue = qRow["FreightSlabMinOrderValue"] == DBNull.Value ? null : Convert.ToDecimal(qRow["FreightSlabMinOrderValue"]),
                    FreightPolicyMaxOrderValue = qRow["FreightSlabMaxOrderValue"] == DBNull.Value ? null : Convert.ToDecimal(qRow["FreightSlabMaxOrderValue"]),
                    FreightPolicyAmount = qRow["FreightPolicyAmount"] == DBNull.Value ? null : Convert.ToDecimal(qRow["FreightPolicyAmount"]),

                    IsODA = qRow["IsODA"] == DBNull.Value? false: Convert.ToBoolean(qRow["IsODA"]),
                    ODACharge = qRow["ODACharge"] == DBNull.Value? 0: Convert.ToDecimal(qRow["ODACharge"]),
                    ODAIncluded = qRow["ODAIncluded"] == DBNull.Value? false: Convert.ToBoolean(qRow["ODAIncluded"]),
                    ODAMinOrderValue = qRow["ODAMinOrderValue"] == DBNull.Value? 0: Convert.ToDecimal(qRow["ODAMinOrderValue"]),
                    ODAApprovalRequired = qRow["ODAApprovalRequired"] == DBNull.Value? false: Convert.ToBoolean(qRow["ODAApprovalRequired"]),
                    ODAIncludedAutomatically = qRow["ODAIncludedAutomatically"] == DBNull.Value? false: Convert.ToBoolean(qRow["ODAIncludedAutomatically"]),
                    //IsODAManagementReviewPending = qRow["IsODAManagementReviewPending"] == DBNull.Value? false: Convert.ToBoolean(qRow["IsODAManagementReviewPending"]),

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

                var items = ds.Tables.Count > 1 ? ds.Tables[1].Rows.Cast<DataRow>().Select(r => new QuotationItemInfo
                    {
                        OpportunityQuotationItemId = Convert.ToInt32(r["OpportunityQuotationItemId"]),
                        Ref_OpportunityRequirementId = Convert.ToInt32(r["Ref_OpportunityRequirementId"]),
                        CategoryId = r["CategoryId"] == DBNull.Value ? null : Convert.ToInt32(r["CategoryId"]),
                        CategoryName = r["CategoryName"] == DBNull.Value ? null : r["CategoryName"].ToString(),
                        OfferingId = r["OfferingId"] == DBNull.Value ? null : Convert.ToInt32(r["OfferingId"]),
                        OfferingName = r["OfferingName"] == DBNull.Value ? null : r["OfferingName"].ToString(),
                        ProductId = r["ProductId"] == DBNull.Value ? null : Convert.ToInt64(r["ProductId"]),
                        ProductCode = r["ProductCode"] == DBNull.Value ? null : r["ProductCode"].ToString(),
                        ProductName = r["ProductName"] == DBNull.Value ? null : r["ProductName"].ToString(),
                        Unit = r["Unit"] == DBNull.Value ? null : r["Unit"].ToString(),
                        Quantity = Convert.ToDecimal(r["Quantity"]),
                        Rate = Convert.ToDecimal(r["Rate"]),
                        Amount = Convert.ToDecimal(r["Amount"])
                    }).ToList(): new List<QuotationItemInfo>();

                var attachments = ds.Tables.Count > 2 ? ds.Tables[2].Rows.Cast<DataRow>().Select(r => new QuotationAttachmentInfo
                    {
                        AttachmentId = Convert.ToInt32(r["AttachmentId"]),
                        FileName = r["FileName"]?.ToString(),
                        FileType = r["FileType"] == DBNull.Value ? null : r["FileType"].ToString(),
                        FileSize = r["FileSize"] == DBNull.Value ? (long?)null : Convert.ToInt64(r["FileSize"]),
                        FileUrl = BuildFileUrl(r["FilePath"]?.ToString()),
                        CreatedOn = Convert.ToDateTime(r["CreatedOn"])
                    }).ToList(): new List<QuotationAttachmentInfo>();

                var history = ds.Tables.Count > 3 ? ds.Tables[3].Rows.Cast<DataRow>()
                      .Select(r => new QuotationHistoryInfo
                      {
                          OpportunityQuotationId =Convert.ToInt32(r["OpportunityQuotationId"]),
                          Ref_OpportunityId =Convert.ToInt32(r["Ref_OpportunityId"]),
                          Ref_PreviousQuotationId =r["Ref_PreviousQuotationId"] == DBNull.Value? null: Convert.ToInt32(r["Ref_PreviousQuotationId"]),
                          QuotationNo =r["QuotationNo"]?.ToString(),
                          VersionNumber =Convert.ToInt32(r["VersionNumber"]),
                          RevisionNumber =Convert.ToInt32(r["RevisionNumber"]),
                          RootQuotationNo =r["RootQuotationNo"] == DBNull.Value? null: r["RootQuotationNo"].ToString(),
                          IsCurrent =Convert.ToBoolean(r["IsCurrent"]),
                          QuotationDate =Convert.ToDateTime(r["QuotationDate"]),
                          ValidUntil =r["ValidUntil"] == DBNull.Value? null: Convert.ToDateTime(r["ValidUntil"]),
                          SubTotal =r["SubTotal"] == DBNull.Value? 0: Convert.ToDecimal(r["SubTotal"]),
                          FreightCharge =r["FreightCharge"] == DBNull.Value? 0: Convert.ToDecimal(r["FreightCharge"]),
                          FreightPercent =r["FreightPercent"] == DBNull.Value? 0: Convert.ToDecimal(r["FreightPercent"]),
                          TaxAmount =r["TaxAmount"] == DBNull.Value? 0: Convert.ToDecimal(r["TaxAmount"]),
                          TaxPercent =r["TaxPercent"] == DBNull.Value? 0: Convert.ToDecimal(r["TaxPercent"]),
                          TotalAmount =r["TotalAmount"] == DBNull.Value? 0: Convert.ToDecimal(r["TotalAmount"]),
                          TermsRemark =r["TermsRemark"] == DBNull.Value? null: r["TermsRemark"].ToString(),
                          SentByEmployeeId =r["SentByEmployeeId"] == DBNull.Value? null: Convert.ToInt32(r["SentByEmployeeId"]),
                          SentOn =r["SentOn"] == DBNull.Value? null: Convert.ToDateTime(r["SentOn"]),
                          CustomerResponseCode =r["CustomerResponseCode"]?.ToString(),
                          CustomerResponseByEmployeeId =r["CustomerResponseByEmployeeId"] == DBNull.Value? null: Convert.ToInt32(r["CustomerResponseByEmployeeId"]),
                          CustomerResponseOn =r["CustomerResponseOn"] == DBNull.Value? null: Convert.ToDateTime(r["CustomerResponseOn"]),
                          CustomerResponseRemark =r["CustomerResponseRemark"] == DBNull.Value? null: r["CustomerResponseRemark"].ToString(),
                          CreatedOn =Convert.ToDateTime(r["CreatedOn"]),
                          CreatedByName =r["CreatedByName"] == DBNull.Value? null: r["CreatedByName"].ToString(),
                          OpportunityStageId =r["OpportunityStageId"] == DBNull.Value? null: Convert.ToInt32(r["OpportunityStageId"]),
                          OpportunityStageCode =r["OpportunityStageCode"] == DBNull.Value? null: r["OpportunityStageCode"].ToString(),
                          OpportunityStageName =r["OpportunityStageName"] == DBNull.Value? null: r["OpportunityStageName"].ToString()

                      }).ToList(): new List<QuotationHistoryInfo>();

                var historyAttachments = ds.Tables.Count > 5? ds.Tables[5].Rows.Cast<DataRow>()
                    .Select(r => new
                    {
                        Attachment = new QuotationAttachmentInfo
                        {
                            AttachmentId =Convert.ToInt32(r["AttachmentId"]),
                            Ref_OpportunityQuotationId =Convert.ToInt32(r["Ref_OpportunityQuotationId"]),
                            FileName =r["FileName"]?.ToString()!,
                            FileType =r["FileType"] == DBNull.Value? null: r["FileType"].ToString(),
                            FileSize =r["FileSize"] == DBNull.Value? null: Convert.ToInt64(r["FileSize"]),
                            FileUrl =r["FilePath"] == DBNull.Value? null: BuildFileUrl(r["FilePath"]?.ToString()),
                            CreatedOn =Convert.ToDateTime(r["CreatedOn"])
                        },
                        QuotationId =Convert.ToInt32(r["Ref_OpportunityQuotationId"])
                    }).ToList(): new();

                    foreach (var item in history)
                    {
                        item.Attachments = historyAttachments
                            .Where(x => x.QuotationId == item.OpportunityQuotationId)
                            .Select(x => x.Attachment)
                            .ToList();
                    }              

                var reviews = ds.Tables.Count > 4? ds.Tables[4].Rows.Cast<DataRow>()
                    .Select(r => new ManagementReviewInfo
                    {
                        ManagementReviewId =Convert.ToInt32(r["ManagementReviewId"]),
                        Ref_OpportunityQuotationId =r["Ref_OpportunityQuotationId"] == DBNull.Value? null: Convert.ToInt32(r["Ref_OpportunityQuotationId"]),
                        Margin =r["Margin"] == DBNull.Value? null: Convert.ToDecimal(r["Margin"]),
                        Remarks =r["Remarks"] == DBNull.Value? null: r["Remarks"].ToString(),
                        DecisionCode =r["DecisionCode"]?.ToString(),
                        ReviewedByEmployeeId =r["ReviewedByEmployeeId"] == DBNull.Value? null: Convert.ToInt32(r["ReviewedByEmployeeId"]),
                        ReviewedOn =r["ReviewedOn"] == DBNull.Value? null: Convert.ToDateTime(r["ReviewedOn"]),
                        CreatedOn =Convert.ToDateTime(r["CreatedOn"])
                    }).ToList(): new List<ManagementReviewInfo>();

                var freightPolicySlabs = ds.Tables.Count > 6 ? ds.Tables[6].Rows.Cast<DataRow>()
                    .Select(r => new FreightPolicySlabQuotationResponse
                    {
                        FreightPolicyId = Convert.ToInt32(r["FreightPolicyId"]),
                        PolicyName = r["PolicyName"]?.ToString() ?? string.Empty,
                        VersionNo = Convert.ToInt32(r["VersionNo"]),
                        FreightPolicySlabId = Convert.ToInt32(r["FreightPolicySlabId"]),
                        MinOrderValue = Convert.ToDecimal(r["MinOrderValue"]),
                        MaxOrderValue = r["MaxOrderValue"] == DBNull.Value ? null : Convert.ToDecimal(r["MaxOrderValue"]),
                        FreightAmount = Convert.ToDecimal(r["FreightAmount"])
                    }).ToList() : new List<FreightPolicySlabQuotationResponse>();

                var categoryTermsAndConditions =ds.Tables.Count > 7 ? ds.Tables[7].Rows.Cast<DataRow>()
                    .Select(r => new RaiseQuotationCategoryTermsResponse
                    {
                        ProductCategoryId = Convert.ToInt32(r["ProductCategoryId"]),
                        CategoryCode = r["CategoryCode"] == DBNull.Value? null: r["CategoryCode"]?.ToString(),
                        CategoryName = r["CategoryName"] == DBNull.Value? null: r["CategoryName"]?.ToString(),
                        TermsAndConditions = r["TermsAndConditions"] == DBNull.Value? null: r["TermsAndConditions"]?.ToString()
                    }).ToList(): new List<RaiseQuotationCategoryTermsResponse>();

                return new ApiResponse
                {
                    Status = true,
                    Message = "Quotation details fetched successfully.",
                   
                    Data = new QuotationDetailResponse
                    {
                        Quotation = quotation,
                        Items = items,
                        Attachments = attachments,
                        History = history,
                        Reviews = reviews,
                        FreightPolicySlabs = freightPolicySlabs,
                        CategoryTermsAndConditions = categoryTermsAndConditions
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

        #region  LOG_CUSTOMER_RESPONSE        
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

        #region LOG_NEGOTIATION_ROUND    
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

        #region  DECIDE_MANAGEMENT_REVIEW        
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

        #region  DECIDE_MANAGEMENT_REVIEW
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
                     new SqlParameter("@IncludeODAFreight", (object?)model.IncludeODAFreight ?? DBNull.Value),
                    new SqlParameter("@CreatedBy", model.CreatedBy)
                );

                // return MapResponse(dt);
                var response = MapResponse(dt);

                if (response.Status && dt.Rows.Count > 0)
                {
                    int quotationRaisedByEmployeeId =Convert.ToInt32(dt.Rows[0]["QuotationRaisedByEmployeeId"]);
                    int opportunityId =Convert.ToInt32(dt.Rows[0]["OpportunityId"]);
                    string quotationNo =Convert.ToString(dt.Rows[0]["QuotationNo"]);
                    string decisionCode =Convert.ToString(dt.Rows[0]["DecisionCode"]);

                    if (quotationRaisedByEmployeeId > 0)
                    {
                        try
                        {
                            string title;
                            string message;

                            if (decisionCode == "APPROVED")
                            {
                                title = "Quotation Approved";
                                message =$"Quotation {quotationNo} has been approved by Management.";
                            }
                            else
                            {
                                title = "Quotation Rejected";
                                message =$"Quotation {quotationNo} has been rejected by Management.";
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

        #region  REVISE_QUOTATION OLD
        //public async Task<ApiResponse> ReviseQuotation(ReviseQuotationRequest model)
        //{
        //    try
        //    {
        //        List<object> savedAttachments = new List<object>();


        //        // Save quotation PDF if provided

        //        if (!string.IsNullOrWhiteSpace(model.QuotationPdfBase64) &&
        //            !string.IsNullOrWhiteSpace(model.QuotationPdfFileName))
        //        {
        //            try
        //            {
        //                string savedPath = SaveBase64Attachment(model.QuotationPdfBase64,model.QuotationPdfFileName,"OpportunityQuotationAttachments");
        //                string base64Data = model.QuotationPdfBase64.Contains(",")? model.QuotationPdfBase64.Substring(model.QuotationPdfBase64.IndexOf(",") + 1): model.QuotationPdfBase64;

        //                savedAttachments.Add(new
        //                {
        //                    FileName = model.QuotationPdfFileName,
        //                    FilePath = savedPath,
        //                    FileType = Path.GetExtension(model.QuotationPdfFileName).TrimStart('.').ToLowerInvariant(),
        //                    FileSize = Convert.FromBase64String(base64Data).Length
        //                });
        //            }
        //            catch (Exception ex)
        //            {
        //                return new ApiResponse
        //                {
        //                    Status = false,
        //                    Message =$"Error processing quotation PDF: {ex.Message}"
        //                };
        //            }
        //        }

        //        // Serialize request data         
        //        string? attachmentsJson =savedAttachments.Count > 0? JsonConvert.SerializeObject(savedAttachments): null;
        //        string itemsJson =JsonConvert.SerializeObject(model.Items); 


        //        var dt = await _database.ExecuteDataTableAsync(SP_NAME,
        //            new SqlParameter("@Status","REVISE_QUOTATION"),
        //            new SqlParameter("@Ref_OpportunityQuotationId",model.Ref_OpportunityQuotationId),
        //            new SqlParameter("@QuotationDate",(object?)model.QuotationDate ?? DBNull.Value),
        //            new SqlParameter("@ValidUntil",(object?)model.ValidUntil ?? DBNull.Value),                
        //            new SqlParameter("@TaxAmount",(object?)model.TaxAmount ?? DBNull.Value),
        //            new SqlParameter("@TaxPercent",(object?)model.TaxPercent ?? DBNull.Value),
        //            new SqlParameter("@TermsRemark",(object?)model.TermsRemark ?? DBNull.Value),
        //            new SqlParameter("@ItemsJson",itemsJson),
        //            new SqlParameter("@AttachmentsMeta",(object?)attachmentsJson ?? DBNull.Value),
        //            new SqlParameter("@CreatedBy",model.CreatedBy
        //            )
        //        );

        //        var response = MapResponse(dt);

        //        if (response.Status)
        //        {
        //            int opportunityId =Convert.ToInt32(dt.Rows[0]["OpportunityId"]);
        //            string quotationNo = Convert.ToString(dt.Rows[0]["QuotationNo"]);
        //            int managerEmployeeId =Convert.ToInt32(dt.Rows[0]["ManagerEmployeeId"]);

        //            if (managerEmployeeId > 0)
        //            {
        //                try
        //                {
        //                    await _schedulerExecutionService.SendEmployeeNotificationAsync(
        //                        schedulerJobItemId: null,
        //                        employeeId: managerEmployeeId,
        //                        entityId: opportunityId,
        //                        title: "Quotation Revised",
        //                        message: $"Quotation {quotationNo} has been revised.",
        //                        entityType: "OPPORTUNITY",
        //                        recipientType: "MANAGER"
        //                    );
        //                }
        //                catch (Exception ex)
        //                {
        //                    _logger.LogError(ex,"Failed to send revised quotation notification to Manager. OpportunityId {OpportunityId}, ManagerEmployeeId {ManagerEmployeeId}",opportunityId,managerEmployeeId);
        //                }
        //            }
        //        }

        //        return response;
        //    }
        //    catch (SqlException ex)
        //    {
        //        return new ApiResponse
        //        {
        //            Status = false,
        //            Message = ex.Message
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ApiResponse
        //        {
        //            Status = false,
        //            Message = ex.Message
        //        };
        //    }
        //}
        #endregion

        #region  REVISE_QUOTATION //NEW 230926
        public async Task<ApiResponse> ReviseQuotation(ReviseQuotationRequest model)
        {
            try
            {
                List<object> savedAttachments = new List<object>();

                if (!string.IsNullOrWhiteSpace(model.QuotationPdfBase64) &&
                    !string.IsNullOrWhiteSpace(model.QuotationPdfFileName))
                {
                    try
                    {
                        string savedPath = SaveBase64Attachment(model.QuotationPdfBase64,model.QuotationPdfFileName,"OpportunityQuotationAttachments");

                        string base64Data =model.QuotationPdfBase64.Contains(",")? model.QuotationPdfBase64.Substring(model.QuotationPdfBase64.IndexOf(",") + 1): model.QuotationPdfBase64;

                        savedAttachments.Add(new
                        {
                            FileName = model.QuotationPdfFileName,
                            FilePath = savedPath,
                            FileType = Path.GetExtension(model.QuotationPdfFileName).TrimStart('.').ToLowerInvariant(),
                            FileSize = Convert.FromBase64String(base64Data).Length
                        });
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse
                        {
                            Status = false,
                            Message = $"Error processing quotation PDF: {ex.Message}"
                        };
                    }
                }

                string? attachmentsJson =savedAttachments.Count > 0? JsonConvert.SerializeObject(savedAttachments): null;
                string itemsJson =JsonConvert.SerializeObject(model.Items);

                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status","REVISE_QUOTATION"),
                    new SqlParameter("@Ref_OpportunityQuotationId",model.Ref_OpportunityQuotationId),
                    new SqlParameter("@DeliveryPartnerId",(object?)model.DeliveryPartnerId ?? DBNull.Value),
                    new SqlParameter("@DeliveryPinCode",(object?)model.DeliveryPinCode ?? DBNull.Value),
                    new SqlParameter("@QuotationDate",(object?)model.QuotationDate ?? DBNull.Value),
                    new SqlParameter("@ValidUntil",(object?)model.ValidUntil ?? DBNull.Value),
                    new SqlParameter("@TaxAmount",(object?)model.TaxAmount ?? DBNull.Value),
                    new SqlParameter("@TaxPercent",(object?)model.TaxPercent ?? DBNull.Value),
                    new SqlParameter("@TermsRemark",(object?)model.TermsRemark ?? DBNull.Value),
                    new SqlParameter("@ItemsJson",itemsJson),
                    new SqlParameter("@AttachmentsMeta",(object?)attachmentsJson ?? DBNull.Value),
                    new SqlParameter("@CreatedBy",model.CreatedBy)
                );

                var response = MapResponse(dt);

                if (response.Status && dt.Rows.Count > 0)
                {
                    int opportunityId =Convert.ToInt32(dt.Rows[0]["OpportunityId"]);
                    string quotationNo =Convert.ToString(dt.Rows[0]["QuotationNo"]);
                    int managerEmployeeId =Convert.ToInt32(dt.Rows[0]["ManagerEmployeeId"]);

                    if (managerEmployeeId > 0)
                    {
                        try
                        {
                            await _schedulerExecutionService
                                .SendEmployeeNotificationAsync(
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
                            _logger.LogError(ex,"Failed to send revised quotation notification to Manager. OpportunityId {OpportunityId}, ManagerEmployeeId {ManagerEmployeeId}",opportunityId,managerEmployeeId);
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

        #region  CHECK ODA
        public async Task<ApiResponse> CheckODA(int deliveryPartnerId, string pinCode)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status", "CHECK_ODA"),
                    new SqlParameter("@DeliveryPartnerId", deliveryPartnerId),
                    new SqlParameter("@PinCode", pinCode)
                );

                if (dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Unable to fetch ODA details."
                    };
                }

                var row = dt.Rows[0];

                if (Convert.ToInt32(row["Status"]) == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = row["Message"]?.ToString()?? "Unable to fetch ODA details."
                    };
                }

                var data = new CheckODAResponse
                {
                    DeliveryPartnerId =Convert.ToInt32(row["DeliveryPartnerId"]),
                    PinCode =row["PinCode"]?.ToString() ?? string.Empty,
                    IsODA =Convert.ToBoolean(row["IsODA"]),
                    ODACharge =row["ODACharge"] == DBNull.Value? 0: Convert.ToDecimal(row["ODACharge"]),
                    ODAMinOrderValue =row["ODAMinOrderValue"] == DBNull.Value? 0: Convert.ToDecimal(row["ODAMinOrderValue"])
                };

                return new ApiResponse
                {
                    Status = true,
                    Message = row["Message"]?.ToString()?? "ODA details fetched successfully.",
                    Data = data
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error checking ODA details.");

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking ODA details.");

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion

        #region  ODA APPROVAL LIST

        public async Task<ApiResponse> GetODAApprovalList(ODAApprovalListRequest request,int employeeId)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status","GET_ODA_APPROVAL_LIST"),
                    new SqlParameter("@LoggedInEmployeeId",employeeId),
                    new SqlParameter("@SearchText",string.IsNullOrWhiteSpace(request.SearchText)? DBNull.Value: request.SearchText.Trim()),
                    new SqlParameter("@DecisionCode",string.IsNullOrWhiteSpace(request.DecisionCode)? DBNull.Value: request.DecisionCode.Trim().ToUpper())
                );

                var list = dt.Rows.Cast<DataRow>()
                    .Select(r => new ODAApprovalListResponse
                    {
                        ODAApprovalId =Convert.ToInt64(r["ODAApprovalId"]),
                        OpportunityQuotationId =Convert.ToInt32(r["OpportunityQuotationId"]),
                        QuotationNo =r["QuotationNo"]?.ToString() ?? string.Empty,
                        VersionNumber =r["VersionNumber"] == DBNull.Value? 0: Convert.ToInt32(r["VersionNumber"]),
                        RevisionNumber =r["RevisionNumber"] == DBNull.Value? 0: Convert.ToInt32(r["RevisionNumber"]),
                        QuotationDate =Convert.ToDateTime(r["QuotationDate"]),
                        OpportunityId =Convert.ToInt32(r["OpportunityId"]),
                        OpportunityNo =r["OpportunityNo"]?.ToString() ?? string.Empty,
                        CustomerName =r["CustomerName"] == DBNull.Value? null: r["CustomerName"]?.ToString(),
                        DeliveryPinCode =r["DeliveryPinCode"] == DBNull.Value? null: r["DeliveryPinCode"]?.ToString(),
                        IsODA =r["IsODA"] != DBNull.Value && Convert.ToBoolean(r["IsODA"]),
                        ODACharge =r["ODACharge"] == DBNull.Value? 0: Convert.ToDecimal(r["ODACharge"]),
                        ODAMinOrderValue =r["ODAMinOrderValue"] == DBNull.Value? 0: Convert.ToDecimal(r["ODAMinOrderValue"]),
                        SubTotal =r["SubTotal"] == DBNull.Value? 0: Convert.ToDecimal(r["SubTotal"]),
                        DecisionCode =r["DecisionCode"]?.ToString() ?? string.Empty,
                        CreatedOn =Convert.ToDateTime(r["CreatedOn"])
                    }).ToList();

                return new ApiResponse
                {
                    Status = true,
                    Message = "ODA approval list fetched successfully.",
                    Data = list
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

        #region  ODA APPROVAL DETAILS
        public async Task<ApiResponse> GetODAApprovalDetails(long quotationId,int employeeId)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync(SP_NAME,
                    new SqlParameter("@Status","GET_ODA_APPROVAL_DETAILS"),
                    new SqlParameter("@Ref_OpportunityQuotationId",quotationId),
                    new SqlParameter("@LoggedInEmployeeId",employeeId)
                );

                if (ds == null || ds.Tables.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "ODA approval details not found."
                    };
                }
            
              
                ODAApprovalDetailResponse approval;

                if (ds.Tables[0].Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "ODA approval details not found."
                    };
                }

                var r = ds.Tables[0].Rows[0];

                approval = new ODAApprovalDetailResponse
                {
                    ODAApprovalId =Convert.ToInt64(r["ODAApprovalId"]),
                    OpportunityQuotationId =Convert.ToInt32(r["OpportunityQuotationId"]),
                    QuotationNo =r["QuotationNo"]?.ToString() ?? string.Empty,
                    VersionNumber =r["VersionNumber"] == DBNull.Value? 0: Convert.ToInt32(r["VersionNumber"]),
                    RevisionNumber =r["RevisionNumber"] == DBNull.Value? 0: Convert.ToInt32(r["RevisionNumber"]),
                    QuotationDate =Convert.ToDateTime(r["QuotationDate"]),
                    ValidUntil =r["ValidUntil"] == DBNull.Value? null: Convert.ToDateTime(r["ValidUntil"]),
                    OpportunityId =Convert.ToInt32(r["OpportunityId"]),
                    OpportunityNo =r["OpportunityNo"]?.ToString() ?? string.Empty,
                    CustomerName =r["CustomerName"] == DBNull.Value? null: r["CustomerName"]?.ToString(),
                    DeliveryAddress =r["DeliveryAddress"] == DBNull.Value? null: r["DeliveryAddress"]?.ToString(),
                    DeliveryPinCode =r["DeliveryPinCode"] == DBNull.Value? null: r["DeliveryPinCode"]?.ToString(),
                    IsODA =r["IsODA"] != DBNull.Value && Convert.ToBoolean(r["IsODA"]),
                    ODACharge =r["ODACharge"] == DBNull.Value? 0: Convert.ToDecimal(r["ODACharge"]),
                    ODAMinOrderValue =r["ODAMinOrderValue"] == DBNull.Value? 0: Convert.ToDecimal(r["ODAMinOrderValue"]),
                    SubTotal =r["SubTotal"] == DBNull.Value? 0: Convert.ToDecimal(r["SubTotal"]),
                    FreightCharge =r["FreightCharge"] == DBNull.Value? 0: Convert.ToDecimal(r["FreightCharge"]),
                    TaxAmount =r["TaxAmount"] == DBNull.Value? 0: Convert.ToDecimal(r["TaxAmount"]),
                    TaxPercent =r["TaxPercent"] == DBNull.Value? 0: Convert.ToDecimal(r["TaxPercent"]),
                    TotalAmount =r["TotalAmount"] == DBNull.Value? 0: Convert.ToDecimal(r["TotalAmount"]),
                    TermsRemark =r["TermsRemark"] == DBNull.Value? null: r["TermsRemark"]?.ToString(),
                    DecisionCode =r["DecisionCode"]?.ToString() ?? string.Empty,
                    IncludeODAFreight =r["IncludeODAFreight"] == DBNull.Value? null: Convert.ToBoolean(r["IncludeODAFreight"]),
                    Remarks =r["Remarks"] == DBNull.Value? null: r["Remarks"]?.ToString(),   
                    ReviewedByEmployeeId =r["ReviewedByEmployeeId"] == DBNull.Value? null: Convert.ToInt32(r["ReviewedByEmployeeId"]),
                    ReviewedOn =r["ReviewedOn"] == DBNull.Value? null: Convert.ToDateTime(r["ReviewedOn"]),
                    CreatedOn =Convert.ToDateTime(r["CreatedOn"])
                };

               
                var items = ds.Tables.Count > 1? ds.Tables[1].Rows.Cast<DataRow>()
                        .Select(r => new ODAApprovalItemInfo
                        {
                            OpportunityQuotationItemId =Convert.ToInt32(r["OpportunityQuotationItemId"]),
                            Ref_OpportunityRequirementId =Convert.ToInt32(r["Ref_OpportunityRequirementId"]),
                            CategoryId =r["CategoryId"] == DBNull.Value? null: Convert.ToInt32(r["CategoryId"]),
                            CategoryName =r["CategoryName"] == DBNull.Value? null: r["CategoryName"]?.ToString(),
                            OfferingId =r["OfferingId"] == DBNull.Value? null: Convert.ToInt32(r["OfferingId"]),
                            OfferingName =r["OfferingName"] == DBNull.Value? null: r["OfferingName"]?.ToString(),
                            ProductId =r["ProductId"] == DBNull.Value? null: Convert.ToInt64(r["ProductId"]),
                            ProductCode =r["ProductCode"] == DBNull.Value? null: r["ProductCode"]?.ToString(),
                            ProductName =r["ProductName"] == DBNull.Value? null: r["ProductName"]?.ToString(),
                            Unit =r["Unit"] == DBNull.Value? null: r["Unit"]?.ToString(),
                            Quantity =r["Quantity"] == DBNull.Value? 0: Convert.ToDecimal(r["Quantity"]),
                            Rate =r["Rate"] == DBNull.Value? 0: Convert.ToDecimal(r["Rate"]),
                            Amount =r["Amount"] == DBNull.Value? 0: Convert.ToDecimal(r["Amount"])
                        }).ToList() : new List<ODAApprovalItemInfo>();
                

                var attachments = ds.Tables.Count > 2 ? ds.Tables[2].Rows.Cast<DataRow>()
                        .Select(r => new QuotationAttachmentInfo
                        {
                            AttachmentId =Convert.ToInt32(r["AttachmentId"]),
                            Ref_OpportunityQuotationId =Convert.ToInt32(r["Ref_OpportunityQuotationId"]),
                            FileName =r["FileName"]?.ToString() ?? string.Empty,
                            FileType =r["FileType"] == DBNull.Value? null: r["FileType"]?.ToString(),
                            FileSize =r["FileSize"] == DBNull.Value? null: Convert.ToInt64(r["FileSize"]),
                            FileUrl =r["FilePath"] == DBNull.Value? null: BuildFileUrl(r["FilePath"]?.ToString()),
                            CreatedOn =Convert.ToDateTime(r["CreatedOn"])
                        }).ToList() : new List<QuotationAttachmentInfo>();

                return new ApiResponse
                {
                    Status = true,
                    Message = "ODA approval details fetched successfully.",

                    Data = new ODAApprovalDetailData
                    {
                        Approval = approval,
                        Items = items,
                        Attachments = attachments
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

        #region  DECIDE ODA APPROVAL
        public async Task<ApiResponse> DecideODAApproval(DecideODAApprovalRequest model)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status","DECIDE_ODA_APPROVAL"),
                    new SqlParameter("@Ref_OpportunityQuotationId",model.Ref_OpportunityQuotationId),
                    new SqlParameter("@DecisionCode",model.DecisionCode),
                    new SqlParameter("@Remarks",(object?)model.Remarks ?? DBNull.Value),
                    new SqlParameter("@ReviewedByEmployeeId",model.ReviewedByEmployeeId),
                    new SqlParameter("@CreatedBy",model.CreatedBy)
                );

                return MapResponse(dt);
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

        #region  SEND ODA APPROVAL EMAIL
        private async Task SendODAApprovalEmailAsync(int quotationId)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync(SP_NAME,
                    new SqlParameter("@Status","GET_ODA_APPROVAL_EMAIL_DATA"),
                    new SqlParameter("@Ref_OpportunityQuotationId",quotationId)
                );

                if (ds == null || ds.Tables.Count < 2)
                {
                    return;
                }

                if (ds.Tables[0].Rows.Count == 0)
                {
                    return;
                }

                var quotation = ds.Tables[0].Rows[0];

                var approvers = ds.Tables[1].Rows;

                if (approvers.Count == 0)
                {
                    return;
                }

                string quotationNo =quotation["QuotationNo"]?.ToString() ?? "";
                string opportunityNo =quotation["OpportunityNo"]?.ToString() ?? "";
                string opportunityName =quotation["OpportunityName"]?.ToString() ?? "";
                string customerName =quotation["CustomerName"]?.ToString() ?? "";
                string deliveryPinCode =quotation["DeliveryPinCode"]?.ToString() ?? "";
                decimal subTotal =quotation["SubTotal"] == DBNull.Value? 0: Convert.ToDecimal(quotation["SubTotal"]);
                decimal odaCharge =quotation["ODACharge"] == DBNull.Value? 0: Convert.ToDecimal(quotation["ODACharge"]);
                decimal odaMinOrderValue =quotation["ODAMinOrderValue"] == DBNull.Value? 0: Convert.ToDecimal(quotation["ODAMinOrderValue"]);
                decimal freightCharge =quotation["FreightCharge"] == DBNull.Value? 0: Convert.ToDecimal(quotation["FreightCharge"]);
                decimal taxAmount =quotation["TaxAmount"] == DBNull.Value? 0: Convert.ToDecimal(quotation["TaxAmount"]);
                decimal totalAmount =quotation["TotalAmount"] == DBNull.Value? 0: Convert.ToDecimal(quotation["TotalAmount"]);
                string raisedBy =quotation["RaisedByEmployeeName"]?.ToString() ?? "";

                // TODO:
                // Replace this with your actual frontend URL.
                //string approvalUrl =$"{_config["CRMSettings:BaseUrl"]}/oda-approval?quotationId={quotationId}";
                // Deep link used only to open the Flutter application.// API environment remains DEV / QA.
                string approvalUrl =$"rajkamalcrm://oda-approval?quotationId={quotationId}";

                var values = new Dictionary<string, string?>
                {
                    ["Title"] = "ODA Approval Required",
                    ["Preheader"] = $"ODA approval required for quotation {quotationNo} - {customerName}, total INR {Money(totalAmount)}",
                    ["QuotationNo"] = quotationNo,
                    ["OpportunityNo"] = opportunityNo,
                    ["OpportunityName"] = opportunityName,
                    ["CustomerName"] = customerName,
                    ["DeliveryPinCode"] = deliveryPinCode,
                    ["RaisedBy"] = raisedBy,
                    ["SubTotal"] = Money(subTotal),
                    ["ODAMinOrderValue"] = Money(odaMinOrderValue),
                    ["ODACharge"] = Money(odaCharge),
                    ["FreightCharge"] = Money(freightCharge),
                    ["TaxAmount"] = Money(taxAmount),
                    ["TotalAmount"] = Money(totalAmount),
                    ["ApprovalUrl"] = approvalUrl
                };

                foreach (DataRow approver in approvers)
                {
                    string email = approver["OfficialEmail"]?.ToString() ?? "";

                    if (string.IsNullOrWhiteSpace(email))
                        continue;

                    values["ApproverName"] = approver["EmployeeName"]?.ToString() ?? "Sir/Madam";

                    var message = new EmailMessage
                    {
                        ToEmail = email,
                        Subject = $"ODA Approval Required - Quotation {quotationNo}",
                        HtmlBody = _templateRenderer.Render("OdaApproval", values)
                    };

                    await _emailService.SendAsync(message);
                }
            }
            catch (Exception ex)
            {
                // Email failure should not break the quotation transaction/result.
                _logger.LogError(
                    ex,
                    "Failed to send ODA approval email for quotation {QuotationId}.",
                    quotationId
                );
            }
        }
        #endregion
       
    }
}