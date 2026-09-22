using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using static CRMWebServices.Models.Call_Log.CallLogModels;

namespace CRMWebServices.Repositories
{
    public class CallLogRepository : ICallLogRepository
    {
        private readonly IDatabaseService _databaseService;

        public CallLogRepository(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<ApiResponse> CreateCallLog(CallLogCreateRequest model)
        {
            try
            {
                var dt = await _databaseService.ExecuteDataTableAsync("USP_CRM_CallLogManagement",
                    new SqlParameter("@Status", "CREATE_CALL_LOG"),
                    new SqlParameter("@Ref_LeadId", (object?)model.Ref_LeadId ?? DBNull.Value),
                    new SqlParameter("@Ref_OpportunityId", (object?)model.Ref_OpportunityId ?? DBNull.Value),
                    new SqlParameter("@Ref_LeadContactId", (object?)model.Ref_LeadContactId ?? DBNull.Value),
                    new SqlParameter("@CallContext", (object?)model.CallContext ?? DBNull.Value),
                    new SqlParameter("@ContactPersonName", (object?)model.ContactPersonName ?? DBNull.Value),
                    new SqlParameter("@ContactPersonNumber", (object?)model.ContactPersonNumber ?? DBNull.Value),
                    new SqlParameter("@CallDirection", model.CallDirection),
                    new SqlParameter("@CallStatus", model.CallStatus),
                    new SqlParameter("@CallOutcome", (object?)model.CallOutcome ?? DBNull.Value),
                    new SqlParameter("@Remark", (object?)model.Remark ?? DBNull.Value),
                    new SqlParameter("@CallStartedOn", model.CallStartedOn),
                    new SqlParameter("@CallEndedOn", (object?)model.CallEndedOn ?? DBNull.Value),
                    new SqlParameter("@DurationInSeconds", model.DurationInSeconds),
                    new SqlParameter("@CalledByEmployeeId", (object?)model.CreatedBy ?? DBNull.Value),
                    new SqlParameter("@ExternalCallId", model.ExternalCallId),
                    new SqlParameter("@CallMatchStatus", (object?)model.CallMatchStatus ?? DBNull.Value),
                    new SqlParameter("@CreatedBy", (object?)model.CreatedBy ?? DBNull.Value)
                );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "No response received from server."
                    };
                }

                var row = dt.Rows[0];

                return new ApiResponse
                {
                    Status = Convert.ToInt32(row["Status"]) == 1,
                    Message = row["Message"]?.ToString()
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

        public async Task<ApiResponse> GetLeadCallLogs(int leadId)
        {
            try
            {
                var ds = await _databaseService.ExecuteDataSetAsync("USP_CRM_CallLogManagement",
                    new SqlParameter("@Status", "GET_LEAD_CALL_LOGS"),
                    new SqlParameter("@Ref_LeadId", leadId)
                );

                return MapCallLogData(ds);
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

        public async Task<ApiResponse> GetOpportunityCallLogs(int opportunityId)
        {
            try
            {
                var ds = await _databaseService.ExecuteDataSetAsync("USP_CRM_CallLogManagement",
                    new SqlParameter("@Status", "GET_OPPORTUNITY_CALL_LOGS"),
                    new SqlParameter("@Ref_OpportunityId", opportunityId)
                );

                return MapCallLogData(ds);
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

        private ApiResponse MapCallLogData(DataSet ds)
        {
            if (ds == null || ds.Tables.Count == 0)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = "No call logs found."
                };
            }

            var response = new CallLogData();

            // RESULT SET 1 - SUMMARY
            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                var row = ds.Tables[0].Rows[0];

                response.Summary = new CallLogSummary
                {
                    TotalCalls = row["TotalCalls"] == DBNull.Value? 0: Convert.ToInt32(row["TotalCalls"]),
                    MissedCalls = row["MissedCalls"] == DBNull.Value? 0: Convert.ToInt32(row["MissedCalls"]),
                    TotalTalkTimeInSeconds = row["TotalTalkTimeInSeconds"] == DBNull.Value? 0: Convert.ToInt32(row["TotalTalkTimeInSeconds"])
                };
            }

            // RESULT SET 2 - CALL LOG LIST
            if (ds.Tables.Count > 1)
            {
                response.Calls = ds.Tables[1].AsEnumerable()
                    .Select(row => new CallLogInfo
                    {
                        LeadCallLogId = Convert.ToInt32(row["LeadCallLogId"]),

                        Ref_LeadId = row["Ref_LeadId"] == DBNull.Value? null: Convert.ToInt32(row["Ref_LeadId"]),
                        LeadNo = row["LeadNo"] == DBNull.Value? null: row["LeadNo"].ToString(),
                        Ref_OpportunityId = row["Ref_OpportunityId"] == DBNull.Value? null: Convert.ToInt32(row["Ref_OpportunityId"]),
                        OpportunityNo = row["OpportunityNo"] == DBNull.Value? null: row["OpportunityNo"].ToString(),
                        Ref_LeadContactId = row["Ref_LeadContactId"] == DBNull.Value? null: Convert.ToInt32(row["Ref_LeadContactId"]),
                        CallContext = row["CallContext"] == DBNull.Value? null: row["CallContext"].ToString(),
                        ContactPersonName = row["ContactPersonName"] == DBNull.Value? null: row["ContactPersonName"].ToString(),
                        ContactPersonNumber = row["ContactPersonNumber"] == DBNull.Value? null: row["ContactPersonNumber"].ToString(),
                        CallDirection = row["CallDirection"] == DBNull.Value? null: row["CallDirection"].ToString(),
                        CallStatus = row["CallStatus"] == DBNull.Value? null: row["CallStatus"].ToString(),
                        CallOutcome = row["CallOutcome"] == DBNull.Value? null: row["CallOutcome"].ToString(),
                        Remark = row["Remark"] == DBNull.Value? null: row["Remark"].ToString(),
                        CallStartedOn = Convert.ToDateTime(row["CallStartedOn"]),
                        CallEndedOn = row["CallEndedOn"] == DBNull.Value? null: Convert.ToDateTime(row["CallEndedOn"]),
                        DurationInSeconds = row["DurationInSeconds"] == DBNull.Value? 0: Convert.ToInt32(row["DurationInSeconds"]),
                        CalledByEmployeeId = Convert.ToInt32(row["CalledByEmployeeId"]),
                        CalledByEmployeeName = row["CalledByEmployeeName"] == DBNull.Value? null: row["CalledByEmployeeName"].ToString(),
                        ExternalCallId = row["ExternalCallId"] == DBNull.Value? null: row["ExternalCallId"].ToString(),
                        CallMatchStatus = row["CallMatchStatus"] == DBNull.Value? null: row["CallMatchStatus"].ToString(),
                        CreatedOn = Convert.ToDateTime(row["CreatedOn"])
                    }).ToList();
            }

            return new ApiResponse
            {
                Status = true,
                Message = "Call logs fetched successfully.",
                Data = response
            };
        }
    }
}