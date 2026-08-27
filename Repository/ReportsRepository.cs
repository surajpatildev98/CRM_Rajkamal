using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using static CRMWebServices.Models.ReportModels;

namespace CRMWebServices.Repository
{
    public class ReportsRepository : IReportsRepository
    {
        private readonly IDatabaseService _database;
        private readonly ILogger<ReportsRepository> _logger;

        public ReportsRepository(IDatabaseService database, ILogger<ReportsRepository> logger)
        {
            _database = database;
            _logger = logger;
        }

        // DASHBOARD
        public async Task<ApiResponse> GetDashboard(ReportModels.CRMReportFilterRequest request)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync("USP_CRM_Reports",
                    new SqlParameter("@Status", "CRM_REPORT_DASHBOARD"),
                    new SqlParameter("@SearchText", (object?)request.SearchText ?? DBNull.Value),
                    new SqlParameter("@FromDate", (object?)request.FromDate ?? DBNull.Value),
                    new SqlParameter("@ToDate", (object?)request.ToDate ?? DBNull.Value),
                    new SqlParameter("@DateType", (object?)request.DateType ?? DBNull.Value),
                    new SqlParameter("@EmployeeId", (object?)request.EmployeeId ?? DBNull.Value),
                    new SqlParameter("@CompanyId", (object?)request.CompanyId ?? DBNull.Value),
                    new SqlParameter("@PriorityId", (object?)request.PriorityId ?? DBNull.Value),
                    new SqlParameter("@ProductId", (object?)request.ProductId ?? DBNull.Value),
                    new SqlParameter("@LeadStatusId", (object?)request.LeadStatusId ?? DBNull.Value),
                    new SqlParameter("@LeadSourceId", (object?)request.LeadSourceId ?? DBNull.Value),
                    new SqlParameter("@OpportunityStageId", (object?)request.OpportunityStageId ?? DBNull.Value),
                    new SqlParameter("@OpportunityTypeId", (object?)request.OpportunityTypeId ?? DBNull.Value),
                    new SqlParameter("@MinProbability", (object?)request.MinProbability ?? DBNull.Value),
                    new SqlParameter("@MaxProbability", (object?)request.MaxProbability ?? DBNull.Value),
                    new SqlParameter("@ExpectedCloseFromDate", (object?)request.ExpectedCloseFromDate ?? DBNull.Value),
                    new SqlParameter("@ExpectedCloseToDate", (object?)request.ExpectedCloseToDate ?? DBNull.Value),
                    new SqlParameter("@ActivityTypeId", (object?)request.ActivityTypeId ?? DBNull.Value),
                    new SqlParameter("@ActivityStatusId", (object?)request.ActivityStatusId ?? DBNull.Value),
                    new SqlParameter("@FollowUpStatusId", (object?)request.FollowUpStatusId ?? DBNull.Value),
                    new SqlParameter("@BusinessState", string.IsNullOrWhiteSpace(request.BusinessState) ? "ALL" : request.BusinessState),
                    new SqlParameter("@IsActive", (object?)request.IsActive ?? DBNull.Value)
                );

                if (ds == null || ds.Tables.Count < 4)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Invalid dashboard report response."
                    };
                }

                // RESULT SET 1 : KPI 
                var kpi = new ReportModels.CRMReportKpiResponse();

                if (ds.Tables[0].Rows.Count > 0)
                {
                    DataRow row = ds.Tables[0].Rows[0];

                    kpi.TotalLeads = Convert.ToInt32(row["TotalLeads"]);
                    //kpi.ConvertedLeads = Convert.ToInt32(row["ConvertedLeads"]);
                    kpi.QualifiedLeads = Convert.ToInt32(row["QualifiedLeads"]);
                    kpi.TotalOpportunities = Convert.ToInt32(row["TotalOpportunities"]);
                    kpi.TotalQuotations = Convert.ToInt32(row["TotalQuotations"]);
                    kpi.NegotiationOpportunities = Convert.ToInt32(row["NegotiationOpportunities"]);
                    kpi.ManagementReviewOpportunities = Convert.ToInt32(row["ManagementReviewOpportunities"]);
                    kpi.WonOpportunities = Convert.ToInt32(row["WonOpportunities"]);
                    kpi.LostOpportunities = Convert.ToInt32(row["LostOpportunities"]);
                    kpi.OpenPipelineValue = Convert.ToDecimal(row["OpenPipelineValue"]);
                    kpi.WonValue = Convert.ToDecimal(row["WonValue"]);
                    kpi.AverageProbability = Convert.ToDecimal(row["AverageProbability"]);
                    kpi.LeadToOpportunityConversionPercent = Convert.ToDecimal(row["LeadToOpportunityConversionPercent"]);
                }

                // RESULT SET 2 : STAGE PIPELINE  
                var stagePipeline = new List<ReportModels.CRMReportStagePipelineResponse>();

                foreach (DataRow row in ds.Tables[1].Rows)
                {
                    stagePipeline.Add(new ReportModels.CRMReportStagePipelineResponse
                    {
                        OpportunityStageId = Convert.ToInt32(row["OpportunityStageId"]),
                        StageCode = row["StageCode"] == DBNull.Value ? null : row["StageCode"].ToString(),
                        StageName = row["StageName"] == DBNull.Value ? null : row["StageName"].ToString(),
                        StageOrder = Convert.ToInt32(row["StageOrder"]),
                        IsClosed = Convert.ToBoolean(row["IsClosed"]),
                        IsWon = Convert.ToBoolean(row["IsWon"]),
                        IsLost = Convert.ToBoolean(row["IsLost"]),
                        OpportunityCount = Convert.ToInt32(row["OpportunityCount"]),
                        ExpectedRevenue = Convert.ToDecimal(row["ExpectedRevenue"]),
                        AverageProbability = Convert.ToDecimal(row["AverageProbability"])
                    });
                }

                // RESULT SET 3 : OPPORTUNITIES       
                var opportunities = new List<ReportModels.CRMReportOpportunityResponse>();

                foreach (DataRow row in ds.Tables[2].Rows)
                {
                    opportunities.Add(new ReportModels.CRMReportOpportunityResponse
                    {
                        OpportunityId = Convert.ToInt32(row["OpportunityId"]),
                        OpportunityNo = row["OpportunityNo"]?.ToString(),
                        Ref_LeadId = Convert.ToInt32(row["Ref_LeadId"]),
                        LeadNo = row["LeadNo"]?.ToString(),
                        OpportunityName = row["OpportunityName"]?.ToString(),
                        Ref_LeadOrganizationId = row["Ref_LeadOrganizationId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_LeadOrganizationId"]),
                        CompanyName = row["CompanyName"]?.ToString(),
                        Ref_OpportunityTypeId = Convert.ToInt32(row["Ref_OpportunityTypeId"]),
                        OpportunityTypeCode = row["OpportunityTypeCode"]?.ToString(),
                        OpportunityTypeName = row["OpportunityTypeName"]?.ToString(),
                        Ref_OpportunityStageId = Convert.ToInt32(row["Ref_OpportunityStageId"]),
                        StageCode = row["StageCode"]?.ToString(),
                        StageName = row["StageName"]?.ToString(),
                        StageOrder = Convert.ToInt32(row["StageOrder"]),
                        Ref_OwnerEmployeeId = Convert.ToInt32(row["Ref_OwnerEmployeeId"]),
                        OwnerName = row["OwnerName"]?.ToString(),
                        ExpectedRevenue = row["ExpectedRevenue"] == DBNull.Value ? null : Convert.ToDecimal(row["ExpectedRevenue"]),
                        Probability = row["Probability"] == DBNull.Value ? null : Convert.ToDecimal(row["Probability"]),
                        ExpectedCloseDate = row["ExpectedCloseDate"] == DBNull.Value ? null : Convert.ToDateTime(row["ExpectedCloseDate"]),
                        Ref_PriorityId = row["Ref_PriorityId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_PriorityId"]),
                        CompetitorName = row["CompetitorName"]?.ToString(),
                        OpportunityRemark = row["OpportunityRemark"]?.ToString(),
                        CreatedOn = Convert.ToDateTime(row["CreatedOn"]),
                        UpdatedOn = row["UpdatedOn"] == DBNull.Value ? null : Convert.ToDateTime(row["UpdatedOn"]),
                        QuotationCount = Convert.ToInt32(row["QuotationCount"]),
                        NegotiationRoundCount = Convert.ToInt32(row["NegotiationRoundCount"]),
                        LastActivityDate = row["LastActivityDate"] == DBNull.Value ? null : Convert.ToDateTime(row["LastActivityDate"]),
                        NextFollowUpDate = row["NextFollowUpDate"] == DBNull.Value ? null : Convert.ToDateTime(row["NextFollowUpDate"])
                    });
                }

                // RESULT SET 4 : CONVERSIONS    
                var conversions = new List<ReportModels.CRMReportConversionResponse>();

                foreach (DataRow row in ds.Tables[3].Rows)
                {
                    conversions.Add(new ReportModels.CRMReportConversionResponse
                    {
                        LeadId = Convert.ToInt32(row["LeadId"]),
                        LeadNo = row["LeadNo"]?.ToString(),
                        LeadCreatedOn = row["LeadCreatedOn"] == DBNull.Value ? null : Convert.ToDateTime(row["LeadCreatedOn"]),
                        Ref_LeadStatusId = row["Ref_LeadStatusId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_LeadStatusId"]),
                        OpportunityId = Convert.ToInt32(row["OpportunityId"]),
                        OpportunityNo = row["OpportunityNo"]?.ToString(),
                        OpportunityCreatedOn = row["OpportunityCreatedOn"] == DBNull.Value ? null : Convert.ToDateTime(row["OpportunityCreatedOn"]),
                        ExpectedRevenue = row["ExpectedRevenue"] == DBNull.Value ? null : Convert.ToDecimal(row["ExpectedRevenue"]),
                        StageCode = row["StageCode"]?.ToString(),
                        StageName = row["StageName"]?.ToString(),
                        OpportunityOwner = row["OpportunityOwner"]?.ToString(),
                        ConversionDate = row["ConversionDate"] == DBNull.Value ? null : Convert.ToDateTime(row["ConversionDate"]),
                        AssignmentRemark = row["AssignmentRemark"]?.ToString()
                    });
                }

                // FINAL RESPONSE  
                return new ApiResponse
                {
                    Status = true,
                    Message = "CRM report dashboard fetched successfully.",
                    Data = new ReportModels.CRMReportDashboardResponse
                    {
                        Dashboard = kpi,
                        StagePipeline = stagePipeline,
                        Opportunities = opportunities,
                        Conversions = conversions
                    }
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error while fetching CRM report dashboard.");

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching CRM report dashboard.");

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        // JOURNEY
        public async Task<ApiResponse> GetJourney(ReportModels.CRMReportJourneyRequest request)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync("USP_CRM_Reports",
                    new SqlParameter("@Status", "CRM_REPORT_JOURNEY"),
                    new SqlParameter("@SearchText", (object?)request.SearchText ?? DBNull.Value),
                    new SqlParameter("@LeadId", (object?)request.LeadId ?? DBNull.Value),
                    new SqlParameter("@OpportunityId", (object?)request.OpportunityId ?? DBNull.Value)
                );

                if (ds == null || ds.Tables.Count < 15)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Invalid journey report response."
                    };
                }

                var response = new ReportModels.CRMReportJourneyResponse();

                // HEADER
                if (ds.Tables[0].Rows.Count > 0)
                {
                    DataRow row = ds.Tables[0].Rows[0];

                    response.Header = new ReportModels.CRMReportJourneyHeaderResponse
                    {
                        LeadId = Convert.ToInt32(row["LeadId"]),
                        LeadNo = row["LeadNo"]?.ToString(),
                        LeadTitle = row["LeadTitle"]?.ToString(),
                        CompanyName = row["CompanyName"]?.ToString(),
                        LeadCreatedOn = row["LeadCreatedOn"] == DBNull.Value ? null : Convert.ToDateTime(row["LeadCreatedOn"]),
                        Ref_LeadStatusId = row["Ref_LeadStatusId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_LeadStatusId"]),
                        OpportunityId = row["OpportunityId"] == DBNull.Value ? null : Convert.ToInt32(row["OpportunityId"]),
                        OpportunityNo = row["OpportunityNo"]?.ToString(),
                        OpportunityName = row["OpportunityName"]?.ToString(),
                        OpportunityCreatedOn = row["OpportunityCreatedOn"] == DBNull.Value ? null : Convert.ToDateTime(row["OpportunityCreatedOn"]),
                        ExpectedRevenue = row["ExpectedRevenue"] == DBNull.Value ? null : Convert.ToDecimal(row["ExpectedRevenue"]),
                        ExpectedCloseDate = row["ExpectedCloseDate"] == DBNull.Value ? null : Convert.ToDateTime(row["ExpectedCloseDate"]),
                        Probability = row["Probability"] == DBNull.Value ? null : Convert.ToDecimal(row["Probability"]),
                        StageCode = row["StageCode"]?.ToString(),
                        StageName = row["StageName"]?.ToString(),
                        OpportunityOwnerName = row["OpportunityOwnerName"]?.ToString(),
                        ConversionDate = row["ConversionDate"] == DBNull.Value ? null : Convert.ToDateTime(row["ConversionDate"]),
                        ConversionRemark = row["ConversionRemark"]?.ToString()
                    };
                }

                // LEAD 
                if (ds.Tables[1].Rows.Count > 0)
                {
                    DataRow row = ds.Tables[1].Rows[0];

                    response.Lead = new ReportModels.CRMReportLeadResponse
                    {
                        LeadId = Convert.ToInt32(row["LeadId"]),
                        LeadNo = row["LeadNo"]?.ToString(),
                        LeadTitle = row["LeadTitle"]?.ToString(),
                        Requirement = row["Requirement"]?.ToString(),
                        LeadRemark = row["LeadRemark"]?.ToString(),
                        Ref_LeadOrganizationId = Convert.ToInt32(row["Ref_LeadOrganizationId"]),
                        CompanyName = row["CompanyName"]?.ToString(),
                        Ref_LeadSourceId = Convert.ToInt32(row["Ref_LeadSourceId"]),
                        Ref_LeadStatusId = row["Ref_LeadStatusId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_LeadStatusId"]),
                        Ref_CurrentAssignedEmployeeId = row["Ref_CurrentAssignedEmployeeId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_CurrentAssignedEmployeeId"]),
                        LeadOwnerName = row["LeadOwnerName"]?.ToString(),
                        Ref_PriorityId = row["Ref_PriorityId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_PriorityId"]),
                        ExpectedValue = row["ExpectedValue"] == DBNull.Value ? null : Convert.ToDecimal(row["ExpectedValue"]),
                        ExpectedClosureDate = row["ExpectedClosureDate"] == DBNull.Value ? null : Convert.ToDateTime(row["ExpectedClosureDate"]),
                        Probability = row["Probability"] == DBNull.Value ? null : Convert.ToDecimal(row["Probability"]),
                        CreatedOn = row["CreatedOn"] == DBNull.Value ? null : Convert.ToDateTime(row["CreatedOn"]),
                        UpdatedOn = row["UpdatedOn"] == DBNull.Value ? null : Convert.ToDateTime(row["UpdatedOn"]),
                        ClosedOn = row["ClosedOn"] == DBNull.Value ? null : Convert.ToDateTime(row["ClosedOn"])
                    };
                }

                // LEAD ACTIVITIES
                foreach (DataRow row in ds.Tables[2].Rows)
                {
                    response.LeadActivities.Add(new ReportModels.CRMReportLeadActivityResponse
                    {
                        ActivityId = Convert.ToInt64(row["ActivityId"]),
                        Ref_LeadId = Convert.ToInt32(row["Ref_LeadId"]),
                        Ref_LeadAssignmentId = row["Ref_LeadAssignmentId"] == DBNull.Value ? null : Convert.ToInt64(row["Ref_LeadAssignmentId"]),
                        Ref_ActivityTypeId = row["Ref_ActivityTypeId"] == DBNull.Value ? null : Convert.ToInt64(row["Ref_ActivityTypeId"]),
                        Ref_EmployeeId = row["Ref_EmployeeId"] == DBNull.Value ? null : Convert.ToInt64(row["Ref_EmployeeId"]),
                        EmployeeName = row["EmployeeName"]?.ToString(),
                        ActivityDateTime = Convert.ToDateTime(row["ActivityDateTime"]),
                        Subject = row["Subject"]?.ToString(),
                        Description = row["Description"]?.ToString(),
                        Outcome = row["Outcome"]?.ToString(),
                        NextFollowUpDate = row["NextFollowUpDate"] == DBNull.Value ? null : Convert.ToDateTime(row["NextFollowUpDate"]),
                        DurationInMinutes = row["DurationInMinutes"] == DBNull.Value ? null : Convert.ToInt32(row["DurationInMinutes"]),
                        Latitude = row["Latitude"] == DBNull.Value ? null : Convert.ToDecimal(row["Latitude"]),
                        Longitude = row["Longitude"] == DBNull.Value ? null : Convert.ToDecimal(row["Longitude"]),
                        LocationAddress = row["LocationAddress"]?.ToString(),
                        Ref_ActivityStatusId = row["Ref_ActivityStatusId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_ActivityStatusId"]),
                        Ref_PreviousLeadStatusId = row["Ref_PreviousLeadStatusId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_PreviousLeadStatusId"]),
                        Ref_NewLeadStatusId = row["Ref_NewLeadStatusId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_NewLeadStatusId"]),
                        CreatedOn = row["CreatedOn"] == DBNull.Value ? null : Convert.ToDateTime(row["CreatedOn"]),
                        Attachments = string.IsNullOrWhiteSpace(row["AttachmentsJson"]?.ToString()) ? new List<ReportModels.CRMReportActivityAttachmentResponse>() : JsonConvert.DeserializeObject<List<ReportModels.CRMReportActivityAttachmentResponse>>(row["AttachmentsJson"].ToString()) ?? new List<ReportModels.CRMReportActivityAttachmentResponse>()
                    });
                }

                // LEAD FOLLOW UPS
                foreach (DataRow row in ds.Tables[3].Rows)
                {
                    response.LeadFollowUps.Add(new ReportModels.CRMReportLeadFollowUpResponse
                    {
                        FollowUpId = Convert.ToInt32(row["FollowUpId"]),
                        Ref_LeadId = Convert.ToInt32(row["Ref_LeadId"]),
                        Ref_AssignedEmployeeId = row["Ref_AssignedEmployeeId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_AssignedEmployeeId"]),
                        AssignedEmployeeName = row["AssignedEmployeeName"]?.ToString(),
                        ScheduledOn = Convert.ToDateTime(row["ScheduledOn"]),
                        Priority = row["Priority"] == DBNull.Value ? null : Convert.ToInt32(row["Priority"]),
                        Remarks = row["Remarks"]?.ToString(),
                        IsCompleted = Convert.ToBoolean(row["IsCompleted"]),
                        CompletedOn = row["CompletedOn"] == DBNull.Value ? null : Convert.ToDateTime(row["CompletedOn"]),
                        Ref_CompletedByEmployeeId = row["Ref_CompletedByEmployeeId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_CompletedByEmployeeId"]),
                        CompletedByEmployeeName = row["CompletedByEmployeeName"]?.ToString(),
                        Ref_ActivityStatusId = row["Ref_ActivityStatusId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_ActivityStatusId"]),
                        Ref_ActivityId = row["Ref_ActivityId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_ActivityId"]),
                        CreatedOn = row["CreatedOn"] == DBNull.Value ? null : Convert.ToDateTime(row["CreatedOn"])
                    });
                }



                // OPPORTUNITIES
                foreach (DataRow row in ds.Tables[4].Rows)
                {
                    response.Opportunities.Add(
                        new ReportModels.CRMReportOpportunityDetailsResponse
                        {
                            OpportunityId = Convert.ToInt32(row["OpportunityId"]),
                            OpportunityNo = row["OpportunityNo"]?.ToString(),
                            Ref_LeadId = Convert.ToInt32(row["Ref_LeadId"]),
                            LeadNo = row["LeadNo"]?.ToString(),
                            Ref_CustomerId = row["Ref_CustomerId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_CustomerId"]),
                            CompanyName = row["CompanyName"]?.ToString(),
                            OpportunityName = row["OpportunityName"]?.ToString(),
                            Ref_OpportunityTypeId = Convert.ToInt32(row["Ref_OpportunityTypeId"]),
                            OpportunityTypeCode = row["OpportunityTypeCode"]?.ToString(),
                            OpportunityTypeName = row["OpportunityTypeName"]?.ToString(),
                            Ref_OpportunityStageId = Convert.ToInt32(row["Ref_OpportunityStageId"]),
                            StageCode = row["StageCode"]?.ToString(),
                            StageName = row["StageName"]?.ToString(),
                            StageOrder = Convert.ToInt32(row["StageOrder"]),
                            ExpectedRevenue = row["ExpectedRevenue"] == DBNull.Value ? null : Convert.ToDecimal(row["ExpectedRevenue"]),
                            ExpectedCloseDate = row["ExpectedCloseDate"] == DBNull.Value ? null : Convert.ToDateTime(row["ExpectedCloseDate"]),
                            Probability = row["Probability"] == DBNull.Value ? null : Convert.ToDecimal(row["Probability"]),
                            Ref_OwnerEmployeeId = Convert.ToInt32(row["Ref_OwnerEmployeeId"]),
                            OwnerName = row["OwnerName"]?.ToString(),
                            Ref_PriorityId = row["Ref_PriorityId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_PriorityId"]),
                            CompetitorName = row["CompetitorName"]?.ToString(),
                            OpportunityRemark = row["OpportunityRemark"]?.ToString(),
                            CreatedOn = row["CreatedOn"] == DBNull.Value ? null : Convert.ToDateTime(row["CreatedOn"]),
                            CreatedBy = Convert.ToInt32(row["CreatedBy"]),
                            UpdatedOn = row["UpdatedOn"] == DBNull.Value ? null : Convert.ToDateTime(row["UpdatedOn"]),
                            UpdatedBy = row["UpdatedBy"] == DBNull.Value ? null : Convert.ToInt32(row["UpdatedBy"])
                        });
                }

                // REQUIREMENTS
                //foreach (DataRow row in ds.Tables[5].Rows)
                //{
                //    response.Requirements.Add(new ReportModels.CRMReportOpportunityRequirementResponse
                //    {
                //        OpportunityRequirementId = Convert.ToInt32(row["OpportunityRequirementId"]),
                //        Ref_OpportunityId = Convert.ToInt32(row["Ref_OpportunityId"]),
                //        Ref_LeadRequirementId = row["Ref_LeadRequirementId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_LeadRequirementId"]),
                //        Ref_ProductId = Convert.ToInt32(row["Ref_ProductId"]),
                //        ProductCode = row["ProductCode"]?.ToString(),
                //        ProductName = row["ProductName"]?.ToString(),
                //        Ref_ProductCategoryId = row["Ref_ProductCategoryId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_ProductCategoryId"]),
                //        CategoryCode = row["CategoryCode"]?.ToString(),
                //        CategoryName = row["CategoryName"]?.ToString(),
                //        Quantity = Convert.ToDecimal(row["Quantity"]),
                //        RequirementRemark = row["RequirementRemark"]?.ToString(),
                //        Ref_UnitId = Convert.ToInt32(row["Ref_UnitId"]),
                //        IsActive = Convert.ToBoolean(row["IsActive"]),
                //        IsDeleted = Convert.ToBoolean(row["IsDeleted"]),
                //        CreatedOn = Convert.ToDateTime(row["CreatedOn"]),
                //        CreatedBy = Convert.ToInt64(row["CreatedBy"]),
                //        UpdatedOn = row["UpdatedOn"] == DBNull.Value ? null : Convert.ToDateTime(row["UpdatedOn"]),
                //        UpdatedBy = row["UpdatedBy"] == DBNull.Value ? null : Convert.ToInt64(row["UpdatedBy"])
                //    });
                //}

                foreach (DataRow row in ds.Tables[5].Rows)
                {
                    response.Requirements.Add(new ReportModels.CRMReportRequirementResponse
                    {
                        RequirementType = row["RequirementType"]?.ToString(),
                        RequirementId = Convert.ToInt32(row["RequirementId"]),
                        LeadId = Convert.ToInt32(row["LeadId"]),
                        OpportunityId = row["OpportunityId"] == DBNull.Value ? null : Convert.ToInt32(row["OpportunityId"]),
                        Ref_LeadRequirementId = row["Ref_LeadRequirementId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_LeadRequirementId"]),
                        Ref_ProductId = row["ProductId"] == DBNull.Value ? null : Convert.ToInt32(row["ProductId"]),
                        ProductCode = row["ProductCode"]?.ToString(),
                        ProductName = row["ProductName"]?.ToString(),
                        Ref_ProductCategoryId = row["Ref_ProductCategoryId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_ProductCategoryId"]),
                        CategoryCode = row["CategoryCode"]?.ToString(),
                        CategoryName = row["CategoryName"]?.ToString(),
                        Quantity = row["Quantity"] == DBNull.Value ? null : Convert.ToDecimal(row["Quantity"]),
                        RequirementRemark = row["RequirementRemark"]?.ToString(),
                        Ref_UnitId = row["UnitId"] == DBNull.Value ? null : Convert.ToInt32(row["UnitId"]),
                        IsActive = row["IsActive"] != DBNull.Value && Convert.ToBoolean(row["IsActive"]),
                        IsDeleted = row["IsDeleted"] != DBNull.Value && Convert.ToBoolean(row["IsDeleted"]),
                        CreatedOn = row["CreatedOn"] == DBNull.Value ? null : Convert.ToDateTime(row["CreatedOn"]),
                        CreatedBy = row["CreatedBy"] == DBNull.Value ? null : Convert.ToInt64(row["CreatedBy"]),
                        UpdatedOn = row["UpdatedOn"] == DBNull.Value ? null : Convert.ToDateTime(row["UpdatedOn"]),
                        UpdatedBy = row["UpdatedBy"] == DBNull.Value ? null : Convert.ToInt64(row["UpdatedBy"])
                    });
                }

                // QUOTATIONS
                foreach (DataRow row in ds.Tables[6].Rows)
                {
                    response.Quotations.Add(new ReportModels.CRMReportQuotationResponse
                    {
                        OpportunityQuotationId = Convert.ToInt32(row["OpportunityQuotationId"]),
                        Ref_OpportunityId = Convert.ToInt32(row["Ref_OpportunityId"]),
                        QuotationNo = row["QuotationNo"]?.ToString(),
                        VersionNumber = Convert.ToInt32(row["VersionNumber"]),
                        RevisionNumber = Convert.ToInt32(row["RevisionNumber"]),
                        RootQuotationNo = row["RootQuotationNo"]?.ToString(),
                        Ref_PreviousQuotationId = row["Ref_PreviousQuotationId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_PreviousQuotationId"]),
                        IsCurrent = Convert.ToBoolean(row["IsCurrent"]),
                        QuotationDate = Convert.ToDateTime(row["QuotationDate"]),
                        ValidUntil = row["ValidUntil"] == DBNull.Value ? null : Convert.ToDateTime(row["ValidUntil"]),
                        SubTotal = Convert.ToDecimal(row["SubTotal"]),
                        FreightCharge = row["FreightCharge"] == DBNull.Value ? null : Convert.ToDecimal(row["FreightCharge"]),
                        FreightPercent = row["FreightPercent"] == DBNull.Value ? null : Convert.ToDecimal(row["FreightPercent"]),
                        TaxPercent = row["TaxPercent"] == DBNull.Value ? null : Convert.ToDecimal(row["TaxPercent"]),
                        TaxAmount = row["TaxAmount"] == DBNull.Value ? null : Convert.ToDecimal(row["TaxAmount"]),
                        TotalAmount = Convert.ToDecimal(row["TotalAmount"]),
                        TermsRemark = row["TermsRemark"]?.ToString(),
                        SentByEmployeeId = row["SentByEmployeeId"] == DBNull.Value ? null : Convert.ToInt32(row["SentByEmployeeId"]),
                        SentByEmployeeName = row["SentByEmployeeName"]?.ToString(),
                        SentOn = row["SentOn"] == DBNull.Value ? null : Convert.ToDateTime(row["SentOn"]),
                        CustomerResponseCode = row["CustomerResponseCode"]?.ToString(),
                        CustomerResponseByEmployeeId = row["CustomerResponseByEmployeeId"] == DBNull.Value ? null : Convert.ToInt32(row["CustomerResponseByEmployeeId"]),
                        CustomerResponseByEmployeeName = row["CustomerResponseByEmployeeName"]?.ToString(),
                        CustomerResponseOn = row["CustomerResponseOn"] == DBNull.Value ? null : Convert.ToDateTime(row["CustomerResponseOn"]),
                        CustomerResponseRemark = row["CustomerResponseRemark"]?.ToString(),
                        IsActive = Convert.ToBoolean(row["IsActive"]),
                        CreatedOn = Convert.ToDateTime(row["CreatedOn"]),
                        CreatedBy = row["CreatedBy"] == DBNull.Value ? null : Convert.ToInt32(row["CreatedBy"]),
                        UpdatedOn = row["UpdatedOn"] == DBNull.Value ? null : Convert.ToDateTime(row["UpdatedOn"]),
                        UpdatedBy = row["UpdatedBy"] == DBNull.Value ? null : Convert.ToInt32(row["UpdatedBy"]),
                        Attachments = string.IsNullOrWhiteSpace(row["AttachmentsJson"]?.ToString()) ? new List<ReportModels.CRMReportQuotationAttachmentResponse>() : JsonConvert.DeserializeObject<List<ReportModels.CRMReportQuotationAttachmentResponse>>(row["AttachmentsJson"].ToString()) ?? new List<ReportModels.CRMReportQuotationAttachmentResponse>()
                    });
                }

                // QUOTATION ITEMS
                foreach (DataRow row in ds.Tables[7].Rows)
                {
                    response.QuotationItems.Add(new ReportModels.CRMReportQuotationItemResponse
                    {
                        OpportunityQuotationItemId = Convert.ToInt32(row["OpportunityQuotationItemId"]),
                        Ref_OpportunityQuotationId = Convert.ToInt32(row["Ref_OpportunityQuotationId"]),
                        Ref_OpportunityRequirementId = Convert.ToInt32(row["Ref_OpportunityRequirementId"]),
                        Quantity = Convert.ToDecimal(row["Quantity"]),
                        Rate = Convert.ToDecimal(row["Rate"]),
                        Amount = Convert.ToDecimal(row["Amount"]),
                        Ref_ProductId = row["Ref_ProductId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_ProductId"]),
                        ProductCode = row["ProductCode"]?.ToString(),
                        ProductName = row["ProductName"]?.ToString()
                    });
                }

                // NEGOTIATIONS
                foreach (DataRow row in ds.Tables[8].Rows)
                {
                    response.Negotiations.Add(new ReportModels.CRMReportNegotiationResponse
                    {
                        OpportunityNegotiationId = Convert.ToInt32(row["OpportunityNegotiationId"]),
                        Ref_OpportunityId = Convert.ToInt32(row["Ref_OpportunityId"]),
                        Ref_OpportunityQuotationId = Convert.ToInt32(row["Ref_OpportunityQuotationId"]),
                        RoundNumber = Convert.ToInt32(row["RoundNumber"]),
                        OurOfferAmount = Convert.ToDecimal(row["OurOfferAmount"]),
                        CustomerCounterAmount = row["CustomerCounterAmount"] == DBNull.Value ? null : Convert.ToDecimal(row["CustomerCounterAmount"]),
                        OfferDifference = row["OfferDifference"] == DBNull.Value ? null : Convert.ToDecimal(row["OfferDifference"]),
                        Remarks = row["Remarks"]?.ToString(),
                        RoundDate = Convert.ToDateTime(row["RoundDate"]),
                        NextFollowUpDate = row["NextFollowUpDate"] == DBNull.Value ? null : Convert.ToDateTime(row["NextFollowUpDate"]),
                        StatusCode = row["StatusCode"]?.ToString(),
                        IsActive = Convert.ToBoolean(row["IsActive"]),
                        CreatedOn = Convert.ToDateTime(row["CreatedOn"]),
                        CreatedBy = row["CreatedBy"] == DBNull.Value ? null : Convert.ToInt32(row["CreatedBy"]),
                        UpdatedOn = row["UpdatedOn"] == DBNull.Value ? null : Convert.ToDateTime(row["UpdatedOn"]),
                        UpdatedBy = row["UpdatedBy"] == DBNull.Value ? null : Convert.ToInt32(row["UpdatedBy"])
                    });
                }

                // MANAGEMENT REVIEWS
                foreach (DataRow row in ds.Tables[9].Rows)
                {
                    response.ManagementReviews.Add(new ReportModels.CRMReportManagementReviewResponse
                    {
                        ManagementReviewId = Convert.ToInt32(row["ManagementReviewId"]),
                        Ref_OpportunityId = Convert.ToInt32(row["Ref_OpportunityId"]),
                        Ref_OpportunityQuotationId = row["Ref_OpportunityQuotationId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_OpportunityQuotationId"]),
                        Ref_OpportunityNegotiationId = row["Ref_OpportunityNegotiationId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_OpportunityNegotiationId"]),
                        Margin = row["Margin"] == DBNull.Value ? null : Convert.ToDecimal(row["Margin"]),
                        Remarks = row["Remarks"]?.ToString(),
                        DecisionCode = row["DecisionCode"]?.ToString(),
                        ReviewedByEmployeeId = row["ReviewedByEmployeeId"] == DBNull.Value ? null : Convert.ToInt32(row["ReviewedByEmployeeId"]),
                        ReviewedByEmployeeName = row["ReviewedByEmployeeName"]?.ToString(),
                        ReviewedOn = row["ReviewedOn"] == DBNull.Value ? null : Convert.ToDateTime(row["ReviewedOn"]),
                        IsActive = Convert.ToBoolean(row["IsActive"]),
                        CreatedOn = Convert.ToDateTime(row["CreatedOn"]),
                        CreatedBy = row["CreatedBy"] == DBNull.Value ? null : Convert.ToInt32(row["CreatedBy"])
                    });
                }

                // OPPORTUNITY ACTIVITIES
                foreach (DataRow row in ds.Tables[10].Rows)
                {
                    response.OpportunityActivities.Add(new ReportModels.CRMReportOpportunityActivityResponse
                    {
                        ActivityId = Convert.ToInt64(row["ActivityId"]),
                        Ref_OpportunityId = Convert.ToInt64(row["Ref_OpportunityId"]),
                        Ref_OpportunityAssignmentId = row["Ref_OpportunityAssignmentId"] == DBNull.Value ? null : Convert.ToInt64(row["Ref_OpportunityAssignmentId"]),
                        Ref_ActivityTypeId = Convert.ToInt64(row["Ref_ActivityTypeId"]),
                        Ref_EmployeeId = Convert.ToInt64(row["Ref_EmployeeId"]),
                        EmployeeName = row["EmployeeName"]?.ToString(),
                        ActivityDateTime = Convert.ToDateTime(row["ActivityDateTime"]),
                        Subject = row["Subject"]?.ToString(),
                        Description = row["Description"]?.ToString(),
                        Outcome = row["Outcome"]?.ToString(),
                        NextFollowUpDate = row["NextFollowUpDate"] == DBNull.Value ? null : Convert.ToDateTime(row["NextFollowUpDate"]),
                        DurationInMinutes = row["DurationInMinutes"] == DBNull.Value ? null : Convert.ToInt32(row["DurationInMinutes"]),
                        Latitude = row["Latitude"] == DBNull.Value ? null : Convert.ToDecimal(row["Latitude"]),
                        Longitude = row["Longitude"] == DBNull.Value ? null : Convert.ToDecimal(row["Longitude"]),
                        LocationAddress = row["LocationAddress"]?.ToString(),
                        Ref_ActivityStatusId = row["Ref_ActivityStatusId"] == DBNull.Value ? null : Convert.ToInt64(row["Ref_ActivityStatusId"]),
                        Ref_OpportunityStageId = Convert.ToInt64(row["Ref_OpportunityStageId"]),
                        Ref_PreviousOpportunityStageId = row["Ref_PreviousOpportunityStageId"] == DBNull.Value ? null : Convert.ToInt64(row["Ref_PreviousOpportunityStageId"]),
                        Ref_NewOpportunityStageId = row["Ref_NewOpportunityStageId"] == DBNull.Value ? null : Convert.ToInt64(row["Ref_NewOpportunityStageId"]),
                        Ref_SourceLeadActivityId = row["Ref_SourceLeadActivityId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_SourceLeadActivityId"]),
                        CreatedOn = Convert.ToDateTime(row["CreatedOn"]),
                        CreatedBy = Convert.ToInt64(row["CreatedBy"]),
                        UpdatedOn = row["UpdatedOn"] == DBNull.Value ? null : Convert.ToDateTime(row["UpdatedOn"]),
                        UpdatedBy = row["UpdatedBy"] == DBNull.Value ? null : Convert.ToInt64(row["UpdatedBy"]),
                        Attachments = string.IsNullOrWhiteSpace(row["AttachmentsJson"]?.ToString()) ? new List<ReportModels.CRMReportActivityAttachmentResponse>() : JsonConvert.DeserializeObject<List<ReportModels.CRMReportActivityAttachmentResponse>>(row["AttachmentsJson"].ToString()) ?? new List<ReportModels.CRMReportActivityAttachmentResponse>()
                    });
                }

                // OPPORTUNITY FOLLOW UPS
                foreach (DataRow row in ds.Tables[11].Rows)
                {
                    response.OpportunityFollowUps.Add(
                        new ReportModels.CRMReportOpportunityFollowUpResponse
                        {
                            OpportunityFollowUpId = Convert.ToInt32(row["OpportunityFollowUpId"]),
                            Ref_OpportunityId = Convert.ToInt32(row["Ref_OpportunityId"]),
                            Ref_OpportunityActivityId = Convert.ToInt32(row["Ref_OpportunityActivityId"]),
                            Ref_AssignedEmployeeId = Convert.ToInt32(row["Ref_AssignedEmployeeId"]),
                            AssignedEmployeeName = row["AssignedEmployeeName"]?.ToString(),
                            ScheduledOn = Convert.ToDateTime(row["ScheduledOn"]),
                            Priority = Convert.ToInt32(row["Priority"]),
                            Remarks = row["Remarks"]?.ToString(),
                            IsCompleted = Convert.ToBoolean(row["IsCompleted"]),
                            CompletedOn = row["CompletedOn"] == DBNull.Value ? null : Convert.ToDateTime(row["CompletedOn"]),
                            Ref_CompletedByEmployeeId = row["Ref_CompletedByEmployeeId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_CompletedByEmployeeId"]),
                            CompletedByEmployeeName = row["CompletedByEmployeeName"]?.ToString(),
                            Ref_ActivityStatusId = Convert.ToInt32(row["Ref_ActivityStatusId"]),
                            Ref_SourceLeadFollowUpId = row["Ref_SourceLeadFollowUpId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_SourceLeadFollowUpId"]),
                            CreatedOn = Convert.ToDateTime(row["CreatedOn"]),
                            CreatedBy = Convert.ToInt32(row["CreatedBy"]),
                            UpdatedOn = row["UpdatedOn"] == DBNull.Value ? null : Convert.ToDateTime(row["UpdatedOn"]),
                            UpdatedBy = row["UpdatedBy"] == DBNull.Value ? null : Convert.ToInt32(row["UpdatedBy"])
                        });
                }

                // OPPORTUNITY STATUS HISTORY
                foreach (DataRow row in ds.Tables[12].Rows)
                {
                    response.OpportunityStatusHistory.Add(new ReportModels.CRMReportOpportunityStatusHistoryResponse
                    {
                        OpportunityStatusHistoryId = Convert.ToInt32(row["OpportunityStatusHistoryId"]),
                        Ref_OpportunityId = Convert.ToInt32(row["Ref_OpportunityId"]),
                        Ref_OpportunityStageId = Convert.ToInt32(row["Ref_OpportunityStageId"]),
                        StageCode = row["StageCode"]?.ToString(),
                        StageName = row["StageName"]?.ToString(),
                        StageOrder = Convert.ToInt32(row["StageOrder"]),
                        Ref_PreviousOpportunityStageId = row["Ref_PreviousOpportunityStageId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_PreviousOpportunityStageId"]),
                        PreviousStageCode = row["PreviousStageCode"]?.ToString(),
                        PreviousStageName = row["PreviousStageName"]?.ToString(),
                        Ref_ChangedByEmployeeId = row["Ref_ChangedByEmployeeId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_ChangedByEmployeeId"]),
                        ChangedByEmployeeName = row["ChangedByEmployeeName"]?.ToString(),
                        StatusRemark = row["StatusRemark"]?.ToString(),
                        ChangedOn = Convert.ToDateTime(row["ChangedOn"]),
                        CreatedOn = Convert.ToDateTime(row["CreatedOn"])
                    });
                }

                // OPPORTUNITY ASSIGNMENTS
                foreach (DataRow row in ds.Tables[13].Rows)
                {
                    response.OpportunityAssignments.Add(new ReportModels.CRMReportOpportunityAssignmentResponse
                    {
                        OpportunityAssignmentId = Convert.ToInt32(row["OpportunityAssignmentId"]),
                        Ref_OpportunityId = Convert.ToInt32(row["Ref_OpportunityId"]),
                        Ref_FromEmployeeId = row["Ref_FromEmployeeId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_FromEmployeeId"]),
                        FromEmployeeName = row["FromEmployeeName"]?.ToString(),
                        Ref_ToEmployeeId = Convert.ToInt32(row["Ref_ToEmployeeId"]),
                        ToEmployeeName = row["ToEmployeeName"]?.ToString(),
                        Ref_AssignedByEmployeeId = Convert.ToInt32(row["Ref_AssignedByEmployeeId"]),
                        AssignedByEmployeeName = row["AssignedByEmployeeName"]?.ToString(),
                        AssignmentType = row["AssignmentType"]?.ToString(),
                        AssignmentRemark = row["AssignmentRemark"]?.ToString(),
                        AssignedOn = Convert.ToDateTime(row["AssignedOn"]),
                        IsCurrent = Convert.ToBoolean(row["IsCurrent"]),
                        IsActive = Convert.ToBoolean(row["IsActive"]),
                        CreatedOn = Convert.ToDateTime(row["CreatedOn"])
                    });
                }

                //// ACTIVITY ATTACHMENTS
                //foreach (DataRow row in ds.Tables[14].Rows)
                //{
                //    response.ActivityAttachments.Add(new ReportModels.CRMReportActivityAttachmentResponse
                //    {
                //        AttachmentId = Convert.ToInt32(row["AttachmentId"]),
                //        Ref_OpportunityActivityId = Convert.ToInt64(row["Ref_OpportunityActivityId"]),
                //        Ref_OpportunityId = Convert.ToInt64(row["Ref_OpportunityId"]),
                //        FileName = row["FileName"]?.ToString(),
                //        FilePath = row["FilePath"]?.ToString(),
                //        FileType = row["FileType"]?.ToString(),
                //        FileSize = row["FileSize"] == DBNull.Value ? null : Convert.ToInt64(row["FileSize"]),
                //        CreatedBy = row["CreatedBy"] == DBNull.Value ? null : Convert.ToInt64(row["CreatedBy"]),
                //        CreatedOn = row["CreatedOn"] == DBNull.Value ? null : Convert.ToDateTime(row["CreatedOn"]),
                //        Ref_ActivityExpenseId = row["Ref_ActivityExpenseId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_ActivityExpenseId"])
                //    });
                //}

                // EXPENSES
                foreach (DataRow row in ds.Tables[14].Rows)
                {
                    var attachments = new List<ReportModels.CRMReportActivityAttachmentResponse>();
                    if (row["LeadAttachmentsJson"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["LeadAttachmentsJson"]?.ToString()))
                        attachments.AddRange(JsonConvert.DeserializeObject<List<ReportModels.CRMReportActivityAttachmentResponse>>(row["LeadAttachmentsJson"].ToString()) ?? new List<ReportModels.CRMReportActivityAttachmentResponse>());
                    if (row["OpportunityAttachmentsJson"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["OpportunityAttachmentsJson"]?.ToString()))
                        attachments.AddRange(JsonConvert.DeserializeObject<List<ReportModels.CRMReportActivityAttachmentResponse>>(row["OpportunityAttachmentsJson"].ToString()) ?? new List<ReportModels.CRMReportActivityAttachmentResponse>());

                    response.Expenses.Add(new ReportModels.CRMReportActivityExpenseResponse
                    {
                        ActivityExpenseId = Convert.ToInt32(row["ActivityExpenseId"]),
                        ExpenseType = row["ExpenseType"]?.ToString(),
                        Ref_LeadId = row["Ref_LeadId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_LeadId"]),
                        Ref_LeadActivityId = row["Ref_LeadActivityId"] == DBNull.Value ? null : Convert.ToInt64(row["Ref_LeadActivityId"]),
                        Ref_OpportunityId = row["Ref_OpportunityId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_OpportunityId"]),
                        Ref_OpportunityActivityId = row["Ref_OpportunityActivityId"] == DBNull.Value ? null : Convert.ToInt64(row["Ref_OpportunityActivityId"]),
                        Ref_ExpenseCategoryId = row["Ref_ExpenseCategoryId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_ExpenseCategoryId"]),
                        CategoryCode = row["CategoryCode"]?.ToString(),
                        CategoryName = row["CategoryName"]?.ToString(),
                        Ref_TransportModeId = row["Ref_TransportModeId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_TransportModeId"]),
                        ModeCode = row["ModeCode"]?.ToString(),
                        ModeName = row["ModeName"]?.ToString(),
                        ExpenseDate = Convert.ToDateTime(row["ExpenseDate"]),
                        Amount = Convert.ToDecimal(row["Amount"]),
                        Description = row["Description"]?.ToString(),
                        FromLocation = row["FromLocation"]?.ToString(),
                        ToLocation = row["ToLocation"]?.ToString(),
                        CreatedOn = row["CreatedOn"] == DBNull.Value ? null : Convert.ToDateTime(row["CreatedOn"]),
                        CreatedBy = row["CreatedBy"] == DBNull.Value ? null : Convert.ToInt32(row["CreatedBy"]),
                        UpdatedOn = row["UpdatedOn"] == DBNull.Value ? null : Convert.ToDateTime(row["UpdatedOn"]),
                        UpdatedBy = row["UpdatedBy"] == DBNull.Value ? null : Convert.ToInt32(row["UpdatedBy"]),
                        Attachments = attachments
                    });
                }

                return new ApiResponse
                {
                    Status = true,
                    Message = "CRM journey report fetched successfully.",
                    Data = response
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error while fetching CRM journey report.");

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching CRM journey report.");

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse> GetReportFilters()
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync("USP_CRM_Reports",
                    new SqlParameter("@Status","REPORT_FILTERS")
                );

                if (ds == null || ds.Tables.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "No report filter data returned."
                    };
                }

                var response =new CRMReportFilterOptionsResponse();
                
                // RESULT SET 1 - EMPLOYEES
                if (ds.Tables.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        response.Employees.Add(new CRMReportFilterOption
                            {
                                Id = Convert.ToInt32(row["Id"]),
                                Code =row["Code"] == DBNull.Value? null: row["Code"].ToString(),
                                Name =row["Name"] == DBNull.Value? null: row["Name"].ToString()
                            }
                        );
                    }
                }
              
                // RESULT SET 2 - COMPANIES
                if (ds.Tables.Count > 1)
                {
                    foreach (DataRow row in ds.Tables[1].Rows)
                    {
                        response.Companies.Add(new CRMReportFilterOption
                            {
                            Id = Convert.ToInt32(row["Id"]),
                            Code = row["Code"] == DBNull.Value ? null : row["Code"].ToString(),
                            Name = row["Name"] == DBNull.Value ? null : row["Name"].ToString()
                        }
                        );
                    }
                }      
                
                // RESULT SET 3 - PRIORITIES         
                if (ds.Tables.Count > 2)
                {
                    foreach (DataRow row in ds.Tables[2].Rows)
                    {
                        response.Priorities.Add(
                            new CRMReportFilterOption
                            {
                                Id = Convert.ToInt32(row["Id"]),
                                Code = row["Code"] == DBNull.Value ? null : row["Code"].ToString(),
                                Name = row["Name"] == DBNull.Value ? null : row["Name"].ToString()
                            }
                        );
                    }
                }
              
                // RESULT SET 4 - PRODUCTSs
                if (ds.Tables.Count > 3)
                {
                    foreach (DataRow row in ds.Tables[3].Rows)
                    {
                        response.Products.Add(
                            new CRMReportFilterOption
                            {
                                Id = Convert.ToInt32(row["Id"]),
                                Code = row["Code"] == DBNull.Value ? null : row["Code"].ToString(),
                                Name = row["Name"] == DBNull.Value ? null : row["Name"].ToString()
                            }
                        );
                    }
                }
                
                // RESULT SET 5 - LEAD STATUS         
                if (ds.Tables.Count > 4)
                {
                    foreach (DataRow row in ds.Tables[4].Rows)
                    {
                        response.LeadStatuses.Add(
                            new CRMReportFilterOption
                            {
                                Id = Convert.ToInt32(row["Id"]),
                                Code = row["Code"] == DBNull.Value ? null : row["Code"].ToString(),
                                Name = row["Name"] == DBNull.Value ? null : row["Name"].ToString()
                            }
                        );
                    }
                }
                
                // RESULT SET 6 - LEAD SOURCE       
                if (ds.Tables.Count > 5)
                {
                    foreach (DataRow row in ds.Tables[5].Rows)
                    {
                        response.LeadSources.Add(
                            new CRMReportFilterOption
                            {
                                Id = Convert.ToInt32(row["Id"]),
                                Code = row["Code"] == DBNull.Value ? null : row["Code"].ToString(),
                                Name = row["Name"] == DBNull.Value ? null : row["Name"].ToString()
                            }
                        );
                    }
                }
                
                // RESULT SET 7 - OPPORTUNITY STAGE       
                if (ds.Tables.Count > 6)
                {
                    foreach (DataRow row in ds.Tables[6].Rows)
                    {
                        response.OpportunityStages.Add(
                            new CRMReportFilterOption
                            {
                                Id = Convert.ToInt32(row["Id"]),
                                Code = row["Code"] == DBNull.Value ? null : row["Code"].ToString(),
                                Name = row["Name"] == DBNull.Value ? null : row["Name"].ToString()
                            }
                        );
                    }
                }
              
                // RESULT SET 8 - OPPORTUNITY TYPE    
                if (ds.Tables.Count > 7)
                {
                    foreach (DataRow row in ds.Tables[7].Rows)
                    {
                        response.OpportunityTypes.Add(
                            new CRMReportFilterOption
                            {
                                Id = Convert.ToInt32(row["Id"]),
                                Code = row["Code"] == DBNull.Value ? null : row["Code"].ToString(),
                                Name = row["Name"] == DBNull.Value ? null : row["Name"].ToString()
                            }
                        );
                    }
                }
                
                // RESULT SET 9 - ACTIVITY TYPE             
                if (ds.Tables.Count > 8)
                {
                    foreach (DataRow row in ds.Tables[8].Rows)
                    {
                        response.ActivityTypes.Add(
                            new CRMReportFilterOption
                            {
                                Id = Convert.ToInt32(row["Id"]),
                                Code = row["Code"] == DBNull.Value ? null : row["Code"].ToString(),
                                Name = row["Name"] == DBNull.Value ? null : row["Name"].ToString()
                            }
                        );
                    }
                }
              
                // RESULT SET 10 - ACTIVITY STATUS
                if (ds.Tables.Count > 9)
                {
                    foreach (DataRow row in ds.Tables[9].Rows)
                    {
                        response.ActivityStatuses.Add(
                            new CRMReportFilterOption
                            {
                                Id = Convert.ToInt32(row["Id"]),
                                Code = row["Code"] == DBNull.Value ? null : row["Code"].ToString(),
                                Name = row["Name"] == DBNull.Value ? null : row["Name"].ToString()
                            }
                        );
                    }
                }
                
                // RESULT SET 11 - FOLLOW-UP STATUS      
                if (ds.Tables.Count > 10)
                {
                    foreach (DataRow row in ds.Tables[10].Rows)
                    {
                        response.FollowUpStatuses.Add(
                            new CRMReportFilterOption
                            {
                                Id = Convert.ToInt32(row["Id"]),
                                Code = row["Code"] == DBNull.Value ? null : row["Code"].ToString(),
                                Name = row["Name"] == DBNull.Value ? null : row["Name"].ToString()
                            }
                        );
                    }
                }
                
                // FIXED REPORTING FILTERS
                response.DateTypes = new List<string>
                {
                    "CREATED",
                    "EXPECTED_CLOSE",
                    "ACTIVITY",
                    "FOLLOWUP",
                    "ASSIGNMENT",
                    "STATUS"
                };

                response.BusinessStates = new List<string>
                {
                    "ALL",
                    "LEAD",
                    "QUALIFIED",
                    "OPPORTUNITY",
                    "QUOTE",
                    "NEGOTIATION",
                    "REVIEW",
                    "WON",
                    "LOST"
                };

                return new ApiResponse
                {
                    Status = true,
                    Message = "Report filters fetched successfully.",
                    Data = response
                };
            }
            catch (SqlException ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                };
            }
        }
    }
}