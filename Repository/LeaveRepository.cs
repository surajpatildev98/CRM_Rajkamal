using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using static CRMWebServices.Models.OpportunityModels;

namespace CRMWebServices.Repository
{
    public class LeaveRepository : ILeaveRepository
    {
        private readonly IDatabaseService _database;
        private readonly ILogger<LeaveRepository> _logger;
        private readonly ISchedulerExecutionService _schedulerExecutionService;
        private readonly ILeadRepository _leadRepository;

        public LeaveRepository(IDatabaseService database,ILogger<LeaveRepository> logger, ISchedulerExecutionService schedulerExecutionService, ILeadRepository leadRepository)
        {
            _database = database;
            _logger  = logger;
            _schedulerExecutionService = schedulerExecutionService;
            _leadRepository = leadRepository;
        }

        #region GET LEAVE BALANCE
        public async Task<LeaveResponse<List<LeaveBalanceModel>>> GetLeaveBalance(int employeeId)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync("USP_ORG_LeaveManagement",
                    new SqlParameter("@Status", "GET_LEAVE_BALANCE"),
                    new SqlParameter("@EmployeeId", employeeId)
                );

                var response = new LeaveResponse<List<LeaveBalanceModel>>
                {
                    Status = false,
                    Message = "No response returned from database.",
                    Data = new List<LeaveBalanceModel>()
                };

                // TABLE 0 = RESPONSE

                if (ds.Tables.Count > 0 &&
                    ds.Tables[0].Rows.Count > 0)
                {
                    var row = ds.Tables[0].Rows[0];

                    response.Status =row["Status"] != DBNull.Value && Convert.ToBoolean(row["Status"]);
                    response.Message =row["Message"]?.ToString() ?? "";
                }

                // TABLE 1 = LEAVE BALANCE DATA

                if (ds.Tables.Count > 1)
                {
                    response.Data = MapLeaveBalance(ds.Tables[1]);
                }

                return response;
            }
            catch (SqlException ex)
            {
                return new LeaveResponse<List<LeaveBalanceModel>>
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}",
                    Data = new List<LeaveBalanceModel>()
                };
            }
            catch (Exception ex)
            {
                return new LeaveResponse<List<LeaveBalanceModel>>
                {
                    Status = false,
                    Message = $"Error: {ex.Message}",
                    Data = new List<LeaveBalanceModel>()
                };
            }
        }
        #endregion

        #region GET MY LEAVES
        public async Task<LeaveResponse<List<LeaveEntryModel>>> GetMyLeaves(
            int employeeId)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync("USP_ORG_LeaveManagement",
                    new SqlParameter("@Status", "GET_MY_LEAVES"),
                    new SqlParameter("@EmployeeId", employeeId)
                );

                var response = new LeaveResponse<List<LeaveEntryModel>>
                {
                    Status = false,
                    Message = "No response returned from database.",
                    Data = new List<LeaveEntryModel>()
                };

                // TABLE 0 = RESPONSE

                if (ds.Tables.Count > 0 &&
                    ds.Tables[0].Rows.Count > 0)
                {
                    var row = ds.Tables[0].Rows[0];

                    response.Status =row["Status"] != DBNull.Value && Convert.ToBoolean(row["Status"]);
                    response.Message =row["Message"]?.ToString() ?? "";
                }

                // TABLE 1 = LEAVE DATA

                if (ds.Tables.Count > 1)
                {
                    response.Data = MapMyLeaves(ds.Tables[1]);
                }
                return response;
            }
            catch (SqlException ex)
            {
                return new LeaveResponse<List<LeaveEntryModel>>
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}",
                    Data = new List<LeaveEntryModel>()
                };
            }
            catch (Exception ex)
            {
                return new LeaveResponse<List<LeaveEntryModel>>
                {
                    Status = false,
                    Message = $"Error: {ex.Message}",
                    Data = new List<LeaveEntryModel>()
                };
            }
        }
        #endregion

        #region MAP LEAVE BALANCE
        private List<LeaveBalanceModel> MapLeaveBalance(DataTable dt)
        {
            var list = new List<LeaveBalanceModel>();

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new LeaveBalanceModel
                {
                    LeaveBalanceId =row["LeaveBalanceId"] != DBNull.Value? Convert.ToInt32(row["LeaveBalanceId"]): 0,
                    EmployeeId =row["EmployeeId"] != DBNull.Value? Convert.ToInt32(row["EmployeeId"]): 0,
                    LeaveTypeId =row["LeaveTypeId"] != DBNull.Value? Convert.ToInt32(row["LeaveTypeId"]): 0,
                    LeaveCode =row["LeaveCode"]?.ToString() ?? "",
                    LeaveName =row["LeaveName"]?.ToString() ?? "",
                    IsPaid =row["IsPaid"] != DBNull.Value && Convert.ToBoolean(row["IsPaid"]),
                    IsHourlyAllowed =row["IsHourlyAllowed"] != DBNull.Value && Convert.ToBoolean(row["IsHourlyAllowed"]),
                    OpeningBalance =row["OpeningBalance"] != DBNull.Value? Convert.ToDecimal(row["OpeningBalance"]): 0,
                    Earned =row["Earned"] != DBNull.Value? Convert.ToDecimal(row["Earned"]): 0,
                    Availed =row["Availed"] != DBNull.Value? Convert.ToDecimal(row["Availed"]): 0,
                    Balance =row["Balance"] != DBNull.Value? Convert.ToDecimal(row["Balance"]): 0,
                    CarriedForward =row["CarriedForward"] != DBNull.Value? Convert.ToDecimal(row["CarriedForward"]): 0,
                    Adjusted =row["Adjusted"] != DBNull.Value? Convert.ToDecimal(row["Adjusted"]): 0,
                    Encashed =row["Encashed"] != DBNull.Value? Convert.ToDecimal(row["Encashed"]): 0,
                    Expired =row["Expired"] != DBNull.Value? Convert.ToDecimal(row["Expired"]): 0,
                    LeaveYearId =row["LeaveYearId"] != DBNull.Value? Convert.ToInt32(row["LeaveYearId"]): 0,
                    YearName =row["YearName"]?.ToString() ?? ""
                });
            }

            return list;
        }
        #endregion

        #region MAP MY LEAVES
        private List<LeaveEntryModel> MapMyLeaves(DataTable dt)
        {
            var list = new List<LeaveEntryModel>();

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new LeaveEntryModel
                {
                    EmployeeLeaveId =row["EmployeeLeaveId"] != DBNull.Value? Convert.ToInt32(row["EmployeeLeaveId"]): 0,
                    EmployeeId =row["EmployeeId"] != DBNull.Value? Convert.ToInt32(row["EmployeeId"]): 0,
                    LeaveTypeId =row["LeaveTypeId"] != DBNull.Value? Convert.ToInt32(row["LeaveTypeId"]): 0,
                    LeaveCode =row["LeaveCode"]?.ToString() ?? "",
                    LeaveName =row["LeaveName"]?.ToString() ?? "",
                    IsPaid =row["IsPaid"] != DBNull.Value && Convert.ToBoolean(row["IsPaid"]),
                    FromDate =row["FromDate"] != DBNull.Value? Convert.ToDateTime(row["FromDate"]): null,
                    ToDate =row["ToDate"] != DBNull.Value? Convert.ToDateTime(row["ToDate"]): null,
                    TotalDays =row["TotalDays"] != DBNull.Value ? Convert.ToDecimal(row["TotalDays"]): 0,
                    Reason =row["Reason"]?.ToString() ?? "",
                    Status = row["Status"] != DBNull.Value ? row["Status"]?.ToString(): null,
                    LeaveApplicationNo =row["LeaveApplicationNo"]?.ToString() ?? "",
                    AppliedOn =row["AppliedOn"] != DBNull.Value ? Convert.ToDateTime(row["AppliedOn"]): null,
                    ApprovedBy =row["ApprovedBy"] != DBNull.Value ? Convert.ToInt32(row["ApprovedBy"]): null,
                    ApprovedOn =row["ApprovedOn"] != DBNull.Value? Convert.ToDateTime(row["ApprovedOn"]): null,
                    Remarks =row["Remarks"]?.ToString() ?? "",
                    RejectedOn =row["RejectedOn"] != DBNull.Value ? Convert.ToDateTime(row["RejectedOn"]): null,
                    RejectedReason =row["RejectedReason"]?.ToString() ?? "",
                    RejectedBy =row["RejectedBy"] != DBNull.Value ? Convert.ToInt32(row["RejectedBy"]): null,
                    CancelledOn =row["CancelledOn"] != DBNull.Value? Convert.ToDateTime(row["CancelledOn"]): null,
                    CancelledReason =row["CancelledReason"]?.ToString() ?? "",
                    CancelledBy =row["CancelledBy"] != DBNull.Value ? Convert.ToInt32(row["CancelledBy"]): null,
                    LeaveYearId =row["LeaveYearId"] != DBNull.Value? Convert.ToInt32(row["LeaveYearId"]): 0,
                    YearName =row["YearName"]?.ToString() ?? ""
                });
            }
            return list;
        }
        #endregion

        #region Get Leave Type
        public async Task<LeaveResponse<List<ApplyLeaveTypeModel>>> GetApplyLeaveTypes(int employeeId)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync(
                    "USP_ORG_LeaveManagement",
                    new SqlParameter("@Status", "GET_APPLY_LEAVE_TYPES"),
                    new SqlParameter("@EmployeeId", employeeId)
                );

                var response = new LeaveResponse<List<ApplyLeaveTypeModel>>
                {
                    Status = false,
                    Message = "Failed to fetch leave types.",
                    Data = new List<ApplyLeaveTypeModel>()
                };

                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    response.Status = Convert.ToBoolean(
                        ds.Tables[0].Rows[0]["Status"]);

                    response.Message =
                        ds.Tables[0].Rows[0]["Message"]?.ToString();
                }

                if (ds.Tables.Count > 1)
                {
                    response.Data = ds.Tables[1].AsEnumerable()
                        .Select(row => new ApplyLeaveTypeModel
                        {
                            LeaveTypeId = Convert.ToInt32(row["LeaveTypeId"]),
                            LeaveCode = row["LeaveCode"]?.ToString(),
                            LeaveName = row["LeaveName"]?.ToString(),

                            IsPaid = row["IsPaid"] != DBNull.Value &&
                                     Convert.ToBoolean(row["IsPaid"]),

                            Balance = row["Balance"] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(row["Balance"]),

                            MaxDaysPerApplication =
                                row["MaxDaysPerApplication"] == DBNull.Value
                                    ? null
                                    : Convert.ToDecimal(
                                        row["MaxDaysPerApplication"]),

                            NoticeDays = row["NoticeDays"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(row["NoticeDays"]),

                            IsHalfDayAllowed =
                                row["IsHalfDayAllowed"] != DBNull.Value &&
                                Convert.ToBoolean(row["IsHalfDayAllowed"]),

                            IsHourlyAllowed =
                                row["IsHourlyAllowed"] != DBNull.Value &&
                                Convert.ToBoolean(row["IsHourlyAllowed"]),

                            IncludeWeekends =
                                row["IncludeWeekends"] != DBNull.Value &&
                                Convert.ToBoolean(row["IncludeWeekends"]),

                            IncludeHolidays =
                                row["IncludeHolidays"] != DBNull.Value &&
                                Convert.ToBoolean(row["IncludeHolidays"]),

                            SandwichRule =
                                row["SandwichRule"] != DBNull.Value &&
                                Convert.ToBoolean(row["SandwichRule"]),

                            RequiresDocument =
                                row["RequiresDocument"] != DBNull.Value &&
                                Convert.ToBoolean(row["RequiresDocument"])
                        })
                        .ToList();
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while fetching apply leave types for EmployeeId: {EmployeeId}",
                    employeeId);

                return new LeaveResponse<List<ApplyLeaveTypeModel>>
                {
                    Status = false,
                    Message = ex.Message,
                    Data = new List<ApplyLeaveTypeModel>()
                };
            }
        }
#endregion

        #region Apply leave
        public async Task<LeaveResponse<object>> ApplyLeave(ApplyLeaveRequest model)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync(
                    "USP_ORG_LeaveManagement",

                    new SqlParameter("@Status", "APPLY_LEAVE"),
                    new SqlParameter("@EmployeeId", model.EmployeeId),
                    new SqlParameter("@LeaveTypeId", model.LeaveTypeId),
                    new SqlParameter("@FromDate", model.FromDate.Date),
                    new SqlParameter("@ToDate", model.ToDate.Date),
                    new SqlParameter("@TotalDays", model.TotalDays),
                    new SqlParameter("@Reason", model.Reason),
                    new SqlParameter("@DayType", model.DayType)
                );

                var response = new LeaveResponse<object>
                {
                    Status = false,
                    Message = "Failed to apply leave.",
                    Data = null
                };

                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    response.Status = Convert.ToBoolean(
                        ds.Tables[0].Rows[0]["Status"]);

                    response.Message =
                        ds.Tables[0].Rows[0]["Message"]?.ToString();
                }

                if (ds.Tables.Count > 1 && ds.Tables[1].Rows.Count > 0)
                {
                    var row = ds.Tables[1].Rows[0];

                    int employeeLeaveId =
                        Convert.ToInt32(row["EmployeeLeaveId"]);

                    string leaveApplicationNo =
                        row["LeaveApplicationNo"]?.ToString() ?? "";

                    decimal totalDays =
                        Convert.ToDecimal(row["TotalDays"]);

                    response.Data = new
                    {
                        EmployeeLeaveId = employeeLeaveId,
                        LeaveApplicationNo = leaveApplicationNo,
                        TotalDays = totalDays
                    };
                    if (response.Status)
                    {
                        try
                        {
                            int? managerEmployeeId =
                                await _leadRepository.GetEmployeeManagerIdAsync(model.EmployeeId);

                            string message =
                                $"A new leave application has been submitted. " +
                                $"Application No: {leaveApplicationNo}. " +
                                $"From: {model.FromDate:dd-MM-yyyy}. " +
                                $"To: {model.ToDate:dd-MM-yyyy}. " +
                                $"Total Days: {totalDays}.";

                            // REPORTING MANAGER
                            if (managerEmployeeId.HasValue &&
                                managerEmployeeId.Value != model.EmployeeId)
                            {

                                await _schedulerExecutionService.SendEmployeeNotificationAsync(
                                           schedulerJobItemId: null,
                                           employeeId: managerEmployeeId.Value,
                                           entityId: employeeLeaveId,
                                           title: "New Leave Application",
                                           message: message,
                                           entityType: "LEAVE",
                                           recipientType: "MANAGER"
                                       );
                            }

                            // HR MANAGER
                            // Add the HR Manager lookup here once its existing
                            // role/table/stored-procedure mapping is confirmed.
                        }
                        catch (Exception notificationEx)
                        {
                            _logger.LogError(
                                notificationEx,
                                "Leave application succeeded, but notification processing failed. EmployeeId: {EmployeeId}",
                                model.EmployeeId);
                        }
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while applying leave for EmployeeId: {EmployeeId}, LeaveTypeId: {LeaveTypeId}",
                    model.EmployeeId,
                    model.LeaveTypeId);

                return new LeaveResponse<object>
                {
                    Status = false,
                    Message = ex.Message,
                    Data = null
                };
            }
        }
#endregion

        #region CANCEL LEAVE

        public async Task<LeaveResponse<object>> CancelLeave(
            CancelLeaveRequest model)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync("USP_ORG_LeaveManagement",

                    new SqlParameter("@Status", "CANCEL_LEAVE"),

                    new SqlParameter("@EmployeeId", model.EmployeeId),

                    new SqlParameter("@EmployeeLeaveId", model.EmployeeLeaveId),

                    new SqlParameter("@Reason", string.IsNullOrWhiteSpace(model.Reason) ? DBNull.Value : model.Reason.Trim())
                );

                var response = new LeaveResponse<object>
                {
                    Status = false,
                    Message = "Failed to cancel leave.",
                    Data = null
                };


                // TABLE 0 = RESPONSE

                if (ds.Tables.Count > 0 &&
                    ds.Tables[0].Rows.Count > 0)
                {
                    var row = ds.Tables[0].Rows[0];

                    response.Status = row["Status"] != DBNull.Value && Convert.ToBoolean(row["Status"]);

                    response.Message = row["Message"]?.ToString() ?? "";
                }


                // TABLE 1 = CANCELLED LEAVE

                if (ds.Tables.Count > 1 &&
                    ds.Tables[1].Rows.Count > 0)
                {
                    var row = ds.Tables[1].Rows[0];

                    response.Data = new
                    {
                        EmployeeLeaveId =row["EmployeeLeaveId"] != DBNull.Value ? Convert.ToInt32(row["EmployeeLeaveId"]) : 0,

                        EmployeeId = row["EmployeeId"] != DBNull.Value ? Convert.ToInt32(row["EmployeeId"]) : 0,

                        LeaveTypeId = row["LeaveTypeId"] != DBNull.Value ? Convert.ToInt32(row["LeaveTypeId"]) : 0,

                        LeaveCode = row["LeaveCode"]?.ToString(),

                        LeaveName = row["LeaveName"]?.ToString(),

                        FromDate = row["FromDate"] != DBNull.Value ? Convert.ToDateTime(row["FromDate"]) : (DateTime?)null,

                        ToDate = row["ToDate"] != DBNull.Value ? Convert.ToDateTime(row["ToDate"]) : (DateTime?)null,

                        TotalDays = row["TotalDays"] != DBNull.Value ? Convert.ToDecimal(row["TotalDays"]) : 0,

                        Reason = row["Reason"]?.ToString(),

                        Status = row["Status"]?.ToString(),

                        LeaveApplicationNo =row["LeaveApplicationNo"]?.ToString(),

                        AppliedOn =  row["AppliedOn"] != DBNull.Value  ? Convert.ToDateTime(row["AppliedOn"]) : (DateTime?)null,

                        CancelledOn = row["CancelledOn"] != DBNull.Value ? Convert.ToDateTime(row["CancelledOn"]) : (DateTime?)null,

                        CancelledReason = row["CancelledReason"]?.ToString(),

                        CancelledBy = row["CancelledBy"] != DBNull.Value ? Convert.ToInt32(row["CancelledBy"]) : (int?)null,

                        LeaveYearId = row["LeaveYearId"] != DBNull.Value ? Convert.ToInt32(row["LeaveYearId"]): 0,

                        YearName = row["YearName"]?.ToString()
                    };
                }

                return response;
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error while cancelling leave. EmployeeId: {EmployeeId}, EmployeeLeaveId: {EmployeeLeaveId}",
                    model.EmployeeId,
                    model.EmployeeLeaveId);

                return new LeaveResponse<object>
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while cancelling leave. EmployeeId: {EmployeeId}, EmployeeLeaveId: {EmployeeLeaveId}",
                    model.EmployeeId,
                    model.EmployeeLeaveId);

                return new LeaveResponse<object>
                {
                    Status = false,
                    Message = ex.Message,
                    Data = null
                };
            }
        }

        #endregion

        #region GET TEAM LEAVES

public async Task<LeaveResponse<List<TeamLeaveModel>>> GetTeamLeaves(
    int employeeId)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync("USP_ORG_LeaveManagement",

                    new SqlParameter("@Status","GET_TEAM_LEAVES"),

                    new SqlParameter("@EmployeeId",employeeId)
                );

                var response = new LeaveResponse<List<TeamLeaveModel>>
                {
                    Status = false,
                    Message = "No team leaves found.",
                    Data = new List<TeamLeaveModel>()
                };

                if (ds.Tables.Count == 0)
                {
                    return response;
                }

                // TABLE 0 = TEAM LEAVES
                var dt = ds.Tables[0];

                foreach (DataRow row in dt.Rows)
                {
                    response.Data.Add(new TeamLeaveModel
                    {
                        EmployeeLeaveId = row["EmployeeLeaveId"] != DBNull.Value ? Convert.ToInt32(row["EmployeeLeaveId"]) : 0,

                        EmployeeId = row["EmployeeId"] != DBNull.Value ? Convert.ToInt32(row["EmployeeId"]) : 0,

                        EmployeeCode = row["EmployeeCode"] != DBNull.Value ? row["EmployeeCode"]?.ToString() : null,

                        FirstName = row["FirstName"] != DBNull.Value ? row["FirstName"]?.ToString() : null,

                        MiddleName = row["MiddleName"] != DBNull.Value ? row["MiddleName"]?.ToString() : null,

                        LastName = row["LastName"] != DBNull.Value ? row["LastName"]?.ToString() : null,

                        EmployeeName = row["EmployeeName"] != DBNull.Value ? row["EmployeeName"]?.ToString() : null,

                        LeaveTypeId = row["LeaveTypeId"] != DBNull.Value ? Convert.ToInt32(row["LeaveTypeId"]) : 0,

                        LeaveCode = row["LeaveCode"] != DBNull.Value ? row["LeaveCode"]?.ToString() : null,

                        LeaveName = row["LeaveName"] != DBNull.Value ? row["LeaveName"]?.ToString() : null,

                        IsPaid = row["IsPaid"] != DBNull.Value ? Convert.ToBoolean(row["IsPaid"]) : null,

                        FromDate = row["FromDate"] != DBNull.Value ? Convert.ToDateTime(row["FromDate"]) : null,

                        ToDate =row["ToDate"] != DBNull.Value ? Convert.ToDateTime(row["ToDate"]) : null,

                        TotalDays = row["TotalDays"] != DBNull.Value ? Convert.ToDecimal(row["TotalDays"]) : 0,

                        Reason = row["Reason"] != DBNull.Value ? row["Reason"]?.ToString() : null,

                        Status = row["Status"] != DBNull.Value ? row["Status"]?.ToString() : null,

                        LeaveApplicationNo = row["LeaveApplicationNo"] != DBNull.Value  ? row["LeaveApplicationNo"]?.ToString() : null,

                        AppliedOn = row["AppliedOn"] != DBNull.Value ? Convert.ToDateTime(row["AppliedOn"]) : null,

                        ApprovedBy = row["ApprovedBy"] != DBNull.Value ? Convert.ToInt32(row["ApprovedBy"]) : null,

                        ApprovedOn = row["ApprovedOn"] != DBNull.Value ? Convert.ToDateTime(row["ApprovedOn"])  : null,

                        RejectedBy = row["RejectedBy"] != DBNull.Value ? Convert.ToInt32(row["RejectedBy"]) : null,

                        RejectedOn = row["RejectedOn"] != DBNull.Value ? Convert.ToDateTime(row["RejectedOn"]) : null,

                        RejectedReason = row["RejectedReason"] != DBNull.Value  ? row["RejectedReason"]?.ToString() : null,

                        CancelledBy =  row["CancelledBy"] != DBNull.Value  ? Convert.ToInt32(row["CancelledBy"]) : null,

                        CancelledOn = row["CancelledOn"] != DBNull.Value ? Convert.ToDateTime(row["CancelledOn"]) : null,

                        CancelledReason = row["CancelledReason"] != DBNull.Value ? row["CancelledReason"]?.ToString() : null,

                        LeaveYearId = row["LeaveYearId"] != DBNull.Value ? Convert.ToInt32(row["LeaveYearId"]) : 0,

                        YearName =  row["YearName"] != DBNull.Value ? row["YearName"]?.ToString() : null
                    });
                }

                response.Status = true;
                response.Message = "Team leaves fetched successfully.";

                return response;
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error while getting team leaves. EmployeeId: {EmployeeId}",
                    employeeId);

                return new LeaveResponse<List<TeamLeaveModel>>
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}",
                    Data = new List<TeamLeaveModel>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while getting team leaves. EmployeeId: {EmployeeId}",
                    employeeId);

                return new LeaveResponse<List<TeamLeaveModel>>
                {
                    Status = false,
                    Message = ex.Message,
                    Data = new List<TeamLeaveModel>()
                };
            }
        }

#endregion

        #region APPROVE LEAVE

public async Task<LeaveResponse<object>> ApproveLeave(
    ApproveLeaveRequest model)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync(
                    "USP_ORG_LeaveManagement",

                    new SqlParameter("@Status", "APPROVE_LEAVE"),

                    new SqlParameter("@EmployeeId", model.EmployeeId),

                    new SqlParameter("@EmployeeLeaveId",model.EmployeeLeaveId)
                );

                var response = new LeaveResponse<object>
                {
                    Status = false,
                    Message = "Failed to approve leave.",
                    Data = null
                };

                // TABLE 0 = RESPONSE
                if (ds.Tables.Count > 0 &&
                    ds.Tables[0].Rows.Count > 0)
                {
                    var row = ds.Tables[0].Rows[0];

                    response.Status = row["Status"] != DBNull.Value && Convert.ToBoolean(row["Status"]);

                    response.Message = row["Message"]?.ToString() ?? "";
                }

                // TABLE 1 = UPDATED LEAVE
                if (ds.Tables.Count > 1 &&
                    ds.Tables[1].Rows.Count > 0)
                {
                    var row = ds.Tables[1].Rows[0];

                    response.Data = new
                    {
                        EmployeeLeaveId = row["EmployeeLeaveId"] != DBNull.Value ? Convert.ToInt32(row["EmployeeLeaveId"]) : 0,

                        EmployeeId = row["EmployeeId"] != DBNull.Value  ? Convert.ToInt32(row["EmployeeId"]) : 0,

                        EmployeeCode = row["EmployeeCode"]?.ToString(),

                        EmployeeName = row["EmployeeName"]?.ToString(),

                        LeaveTypeId = row["LeaveTypeId"] != DBNull.Value ? Convert.ToInt32(row["LeaveTypeId"]) : 0,

                        LeaveCode = row["LeaveCode"]?.ToString(),

                        LeaveName =row["LeaveName"]?.ToString(),

                        FromDate = row["FromDate"] != DBNull.Value ? Convert.ToDateTime(row["FromDate"]) : (DateTime?)null,

                        ToDate = row["ToDate"] != DBNull.Value ? Convert.ToDateTime(row["ToDate"]) : (DateTime?)null,

                        TotalDays = row["TotalDays"] != DBNull.Value ? Convert.ToDecimal(row["TotalDays"]): 0,

                        Reason = row["Reason"]?.ToString(),

                        Status = row["Status"]?.ToString(),

                        LeaveApplicationNo =row["LeaveApplicationNo"]?.ToString(),

                        AppliedOn = row["AppliedOn"] != DBNull.Value ? Convert.ToDateTime(row["AppliedOn"]) : (DateTime?)null,

                        ApprovedBy = row["ApprovedBy"] != DBNull.Value ? Convert.ToInt32(row["ApprovedBy"]) : (int?)null,

                        ApprovedOn = row["ApprovedOn"] != DBNull.Value ? Convert.ToDateTime(row["ApprovedOn"]) : (DateTime?)null,

                        LeaveYearId =row["LeaveYearId"] != DBNull.Value ? Convert.ToInt32(row["LeaveYearId"]) : 0,

                        YearName =row["YearName"]?.ToString()
                    };
                }

                return response;
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error while approving leave. ManagerId: {ManagerId}, EmployeeLeaveId: {EmployeeLeaveId}",
                    model.EmployeeId,
                    model.EmployeeLeaveId);

                return new LeaveResponse<object>
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while approving leave. ManagerId: {ManagerId}, EmployeeLeaveId: {EmployeeLeaveId}",
                    model.EmployeeId,
                    model.EmployeeLeaveId);

                return new LeaveResponse<object>
                {
                    Status = false,
                    Message = ex.Message,
                    Data = null
                };
            }
        }

        #endregion

        #region REJECT LEAVE

public async Task<LeaveResponse<object>> RejectLeave(RejectLeaveRequest model)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync(
                    "USP_ORG_LeaveManagement",

                    new SqlParameter("@Status","REJECT_LEAVE"),

                    new SqlParameter("@EmployeeId", model.EmployeeId),

                    new SqlParameter("@EmployeeLeaveId",model.EmployeeLeaveId),

                    new SqlParameter("@Reason", string.IsNullOrWhiteSpace(model.Reason) ? DBNull.Value: model.Reason.Trim())
                );

                var response = new LeaveResponse<object>
                {
                    Status = false,
                    Message = "Failed to reject leave.",
                    Data = null
                };

                // TABLE 0 = RESPONSE
                if (ds.Tables.Count > 0 &&
                    ds.Tables[0].Rows.Count > 0)
                {
                    var row = ds.Tables[0].Rows[0];

                    response.Status = row["Status"] != DBNull.Value && Convert.ToBoolean(row["Status"]);

                    response.Message = row["Message"]?.ToString() ?? "";
                }

                // TABLE 1 = UPDATED LEAVE
                if (ds.Tables.Count > 1 &&
                    ds.Tables[1].Rows.Count > 0)
                {
                    var row = ds.Tables[1].Rows[0];

                    response.Data = new
                    {
                        EmployeeLeaveId =  row["EmployeeLeaveId"] != DBNull.Value ? Convert.ToInt32(row["EmployeeLeaveId"]) : 0,

                        EmployeeId = row["EmployeeId"] != DBNull.Value ? Convert.ToInt32(row["EmployeeId"]) : 0,

                        EmployeeCode = row["EmployeeCode"]?.ToString(),

                        EmployeeName = row["EmployeeName"]?.ToString(),

                        LeaveTypeId = row["LeaveTypeId"] != DBNull.Value  ? Convert.ToInt32(row["LeaveTypeId"]) : 0,

                        LeaveCode = row["LeaveCode"]?.ToString(),

                        LeaveName = row["LeaveName"]?.ToString(),

                        FromDate = row["FromDate"] != DBNull.Value ? Convert.ToDateTime(row["FromDate"]) : (DateTime?)null,

                        ToDate = row["ToDate"] != DBNull.Value ? Convert.ToDateTime(row["ToDate"]) : (DateTime?)null,

                        TotalDays = row["TotalDays"] != DBNull.Value ? Convert.ToDecimal(row["TotalDays"]) : 0,

                        Reason = row["Reason"]?.ToString(),

                        Status = row["Status"]?.ToString(),

                        LeaveApplicationNo =  row["LeaveApplicationNo"]?.ToString(),

                        AppliedOn =row["AppliedOn"] != DBNull.Value ? Convert.ToDateTime(row["AppliedOn"]) : (DateTime?)null,

                        RejectedBy = row["RejectedBy"] != DBNull.Value ? Convert.ToInt32(row["RejectedBy"]) : (int?)null,

                        RejectedOn = row["RejectedOn"] != DBNull.Value ? Convert.ToDateTime(row["RejectedOn"]) : (DateTime?)null,

                        RejectedReason = row["RejectedReason"]?.ToString(),

                        LeaveYearId = row["LeaveYearId"] != DBNull.Value ? Convert.ToInt32(row["LeaveYearId"]) : 0,

                        YearName = row["YearName"]?.ToString()
                    };
                }

                return response;
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error while rejecting leave. ManagerId: {ManagerId}, EmployeeLeaveId: {EmployeeLeaveId}",
                    model.EmployeeId,
                    model.EmployeeLeaveId);

                return new LeaveResponse<object>
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while rejecting leave. ManagerId: {ManagerId}, EmployeeLeaveId: {EmployeeLeaveId}",
                    model.EmployeeId,
                    model.EmployeeLeaveId);

                return new LeaveResponse<object>
                {
                    Status = false,
                    Message = ex.Message,
                    Data = null
                };
            }
        }

#endregion




    }
}
