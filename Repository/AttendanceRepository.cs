using CRMWebServices.Interfaces;
using CRMWebServices.Models.Attendance;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CRMWebServices.Repository
{
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly IDatabaseService _database;
        private readonly ISchedulerExecutionService _schedulerExecutionService;
        private readonly ILogger<AttendanceRepository> _logger;
        private readonly string _connectionString;
        private static bool Has(DataRow r, string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value;
        private static int RInt(DataRow r, string c) => Has(r, c) ? Convert.ToInt32(r[c]) : 0;
        private static int? RNInt(DataRow r, string c) => Has(r, c) ? Convert.ToInt32(r[c]) : null;
        private static decimal RDec(DataRow r, string c) => Has(r, c) ? Convert.ToDecimal(r[c]) : 0;
        private static decimal? RNDec(DataRow r, string c) => Has(r, c) ? Convert.ToDecimal(r[c]) : null;
        private static string? RStr(DataRow r, string c) => Has(r, c) ? r[c].ToString() : null;
        private static bool RBool(DataRow r, string c) => Has(r, c) && Convert.ToBoolean(r[c]);
        private static DateTime RDate(DataRow r, string c) => Has(r, c) ? Convert.ToDateTime(r[c]) : DateTime.MinValue;
        private static DateTime? RNDate(DataRow r, string c) => Has(r, c) ? Convert.ToDateTime(r[c]) : null;

        public AttendanceRepository(
            IDatabaseService database,
            ISchedulerExecutionService schedulerExecutionService,
            ILogger<AttendanceRepository> logger,
            IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection") ?? string.Empty;
            _database = database;
            _schedulerExecutionService = schedulerExecutionService;
            _logger = logger;
        }
        private List<AttendanceModel> MapAttendance(DataTable dt)
        {
            var list = new List<AttendanceModel>();

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new AttendanceModel
                {
                    AttendanceId = row["AttendanceId"] != DBNull.Value ? Convert.ToInt32(row["AttendanceId"]) : 0,
                    EmployeeId = row["EmployeeId"] != DBNull.Value ? Convert.ToInt32(row["EmployeeId"]) : 0,
                    EmployeeCode = row["EmployeeCode"]?.ToString() ?? string.Empty,
                    EmployeeName = row["EmployeeName"]?.ToString() ?? string.Empty,
                    OfficeId = row["OfficeId"] != DBNull.Value ? Convert.ToInt32(row["OfficeId"]) : null,
                    OfficeName = row["OfficeName"]?.ToString() ?? string.Empty,
                    DepartmentId = row["DepartmentId"] != DBNull.Value ? Convert.ToInt32(row["DepartmentId"]) : null,
                    DepartmentName = row["DepartmentName"]?.ToString() ?? string.Empty,
                    AttendanceDate = row["AttendanceDate"] != DBNull.Value ? Convert.ToDateTime(row["AttendanceDate"]) : DateTime.MinValue,
                    FirstInTime = row["FirstInTime"] != DBNull.Value ? Convert.ToDateTime(row["FirstInTime"]) : null,
                    LastOutTime = row["LastOutTime"] != DBNull.Value ? Convert.ToDateTime(row["LastOutTime"]) : null,
                    WorkingHours = row["WorkingHours"] != DBNull.Value ? Convert.ToDecimal(row["WorkingHours"]) : null,
                    OvertimeHours = row["OvertimeHours"] != DBNull.Value ? Convert.ToDecimal(row["OvertimeHours"]) : null,
                    LateMinutes = row["LateMinutes"] != DBNull.Value ? Convert.ToInt32(row["LateMinutes"]) : null,
                    EarlyExitMinutes = row["EarlyExitMinutes"] != DBNull.Value ? Convert.ToInt32(row["EarlyExitMinutes"]) : null,
                    BreakHours = row["BreakHours"] != DBNull.Value ? Convert.ToDecimal(row["BreakHours"]) : null,
                    Ref_AttendanceStatusId = row["Ref_AttendanceStatusId"] != DBNull.Value ? Convert.ToInt32(row["Ref_AttendanceStatusId"]) : null,
                    Ref_ShiftId = row["Ref_ShiftId"] != DBNull.Value ? Convert.ToInt32(row["Ref_ShiftId"]) : null,
                    ShiftName = row["ShiftName"] != DBNull.Value ? row["ShiftName"]?.ToString() : null,
                    PunchSource = row["PunchSource"] != DBNull.Value ? row["PunchSource"]?.ToString() : null,
                    WorkMode = row["WorkMode"] != DBNull.Value ? row["WorkMode"]?.ToString() : null,
                    Remarks = row["Remarks"] != DBNull.Value ? row["Remarks"]?.ToString() : null,
                    CreatedOn = row["CreatedOn"] != DBNull.Value ? Convert.ToDateTime(row["CreatedOn"]) : null,
                    CreatedBy = row["CreatedBy"] != DBNull.Value ? Convert.ToInt32(row["CreatedBy"]) : null,
                    UpdatedOn = row["UpdatedOn"] != DBNull.Value ? Convert.ToDateTime(row["UpdatedOn"]) : null,
                    UpdatedBy = row["UpdatedBy"] != DBNull.Value ? Convert.ToInt32(row["UpdatedBy"]) : null
                });
            }

            return list;
        }

        public async Task<string> ResolveViewScope(int employeeId, string roleScope)
        {
            // HR roles / explicit Manager role already decided
            if (roleScope != "SELF") return roleScope;

            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_ORG_Attendance",
                    new SqlParameter("@Status", "GET_IS_MANAGER"),
                    new SqlParameter("@LoggedInEmployeeId", employeeId)
                );

                if (dt.Rows.Count > 0
                    && dt.Columns.Contains("IsManager")
                    && dt.Rows[0]["IsManager"] != DBNull.Value
                    && Convert.ToBoolean(dt.Rows[0]["IsManager"]))
                {
                    return "TEAM";
                }
            }
            catch
            {
                // if the check fails, fall back to the safest scope
            }

            return "SELF";
        }

        public async Task<AttendanceResponse> GetAttendance(
    AttendanceRequest request, int loggedInEmployeeId, string viewScope)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_ORG_Attendance",
                    new SqlParameter("@Status", "GET"),
                    new SqlParameter("@AttendanceDate", request.AttendanceDate.HasValue ? request.AttendanceDate.Value.Date : DBNull.Value),
                    new SqlParameter("@OfficeId", request.OfficeId ?? (object)DBNull.Value),
                    new SqlParameter("@DepartmentId", request.DepartmentId ?? (object)DBNull.Value),
                    new SqlParameter("@Search", string.IsNullOrWhiteSpace(request.Search) ? DBNull.Value : request.Search.Trim()),
                    new SqlParameter("@LoggedInEmployeeId", loggedInEmployeeId),
                    new SqlParameter("@ViewScope", viewScope)
                );

                // SP validation error comes back as (Status, Message)
                if (dt.Columns.Contains("Message") && !dt.Columns.Contains("AttendanceId"))
                {
                    return new AttendanceResponse
                    {
                        Status = false,
                        Message = dt.Rows.Count > 0
                            ? dt.Rows[0]["Message"]?.ToString() ?? "Unable to fetch attendance."
                            : "Unable to fetch attendance.",
                        Data = new List<AttendanceModel>()
                    };
                }

                return new AttendanceResponse
                {
                    Status = true,
                    Message = "Attendance fetched successfully.",
                    Data = MapAttendance(dt)
                };
            }
            catch (SqlException ex)
            {
                return new AttendanceResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}",
                    Data = new List<AttendanceModel>()
                };
            }
            catch (Exception ex)
            {
                return new AttendanceResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}",
                    Data = new List<AttendanceModel>()
                };
            }
        }

        public async Task<AttendancePunchResponse> GetPunches(AttendancePunchRequest request, int loggedInEmployeeId, string viewScope)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_ORG_Attendance",
                    new SqlParameter("@Status", "GET_PUNCHES"),
                    new SqlParameter("@AttendanceId", request.AttendanceId),
                    new SqlParameter("@LoggedInEmployeeId", loggedInEmployeeId),
                    new SqlParameter("@ViewScope", viewScope)
                );

                // SP validation / permission error
                if (dt.Columns.Contains("Message") && !dt.Columns.Contains("AttendancePunchId"))
                {
                    return new AttendancePunchResponse
                    {
                        Status = false,
                        Message = dt.Rows.Count > 0
                            ? dt.Rows[0]["Message"]?.ToString() ?? "Unable to fetch punches."
                            : "Unable to fetch punches.",
                        Data = new List<AttendancePunchModel>()
                    };
                }

                var list = new List<AttendancePunchModel>();

                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new AttendancePunchModel
                    {
                        AttendancePunchId = RInt(row, "AttendancePunchId"),
                        AttendanceId = RInt(row, "AttendanceId"),
                        EmployeeId = RInt(row, "EmployeeId"),
                        PunchDateTime = RDate(row, "PunchDateTime"),

                        // "IN" / "OUT" instead of "I" / "O"
                        PunchType = RStr(row, "PunchTypeName") ?? RStr(row, "PunchType"),

                        PunchSource = RStr(row, "PunchSource"),
                        Remarks = RStr(row, "Remarks"),
                        CreatedOn = RNDate(row, "CreatedOn"),
                        CreatedBy = RNInt(row, "CreatedBy")
                    });
                }

                return new AttendancePunchResponse
                {
                    Status = true,
                    Message = "Attendance punches fetched successfully.",
                    Data = list
                };
            }
            catch (SqlException ex)
            {
                return new AttendancePunchResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}",
                    Data = new List<AttendancePunchModel>()
                };
            }
            catch (Exception ex)
            {
                return new AttendancePunchResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}",
                    Data = new List<AttendancePunchModel>()
                };
            }
        }

        public async Task<ManualPunchResponse> ManualPunch(ManualPunchRequest request)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_ORG_Attendance",
                    new SqlParameter("@Status", "MANUAL_PUNCH"),
                    new SqlParameter("@EmployeeId", request.EmployeeId),
                    new SqlParameter("@PunchType", string.IsNullOrWhiteSpace(request.PunchType) ? DBNull.Value : request.PunchType),
                    new SqlParameter("@WorkMode",string.IsNullOrWhiteSpace(request.WorkMode) ? DBNull.Value : request.WorkMode),
                    new SqlParameter("@Latitude",request.Latitude),
                    new SqlParameter("@Longitude",request.Longitude),
                    new SqlParameter("@Remarks",string.IsNullOrWhiteSpace(request.Remarks) ? DBNull.Value : request.Remarks),
                    new SqlParameter("@CreatedBy",request.CreatedBy)
                );

                if (dt.Rows.Count == 0)
                {
                    return new ManualPunchResponse
                    {
                        Status = false,
                        Message = "No response received from attendance procedure."
                    };
                }

                DataRow row = dt.Rows[0];

                return new ManualPunchResponse
                {
                    Status = row["Status"] != DBNull.Value && Convert.ToBoolean(row["Status"]),
                    Message = row["Message"]?.ToString() ?? string.Empty,
                    AttendanceId = row.Table.Columns.Contains("AttendanceId") && row["AttendanceId"] != DBNull.Value ? Convert.ToInt32(row["AttendanceId"]) : null,
                    PunchType = row.Table.Columns.Contains("PunchType") && row["PunchType"] != DBNull.Value ? row["PunchType"]?.ToString() : null,
                    WorkMode = row.Table.Columns.Contains("WorkMode") && row["WorkMode"] != DBNull.Value ? row["WorkMode"]?.ToString() : null,
                    Latitude = row.Table.Columns.Contains("Latitude") && row["Latitude"] != DBNull.Value ? Convert.ToDecimal(row["Latitude"]) : null,
                    Longitude = row.Table.Columns.Contains("Longitude") && row["Longitude"] != DBNull.Value ? Convert.ToDecimal(row["Longitude"]) : null,
                    DistanceKm = row.Table.Columns.Contains("DistanceKm") && row["DistanceKm"] != DBNull.Value ? Convert.ToDecimal(row["DistanceKm"]) : null
                };
            }
            catch (SqlException ex)
            {
                return new ManualPunchResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new ManualPunchResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        public async Task<AttendanceDashboardResponse> GetDashboardCount(AttendanceDashboardRequest request)
        {
            try
            {
                var dt = await _database.ExecuteDataSetAsync("USP_ORG_Attendance",
                    new SqlParameter("@Status","GET_DASHBOARD_COUNT"),
                    new SqlParameter("@FromDate",request.FromDate.HasValue ? request.FromDate.Value.Date : DBNull.Value),
                    new SqlParameter("@ToDate",request.ToDate.HasValue ? request.ToDate.Value.Date : DBNull.Value),
                    new SqlParameter("@EmployeeId", request.EmployeeId ?? (object)DBNull.Value)
                );

                if (dt.Tables.Count == 0 || dt == null)
                {
                    return new AttendanceDashboardResponse
                    {
                        Status = false,
                        Message = "No dashboard data found.",
                        Data = null
                    };
                }

                DataTable summaryTable = dt.Tables[0];

                if (summaryTable.Rows.Count == 0)
                {
                    return new AttendanceDashboardResponse
                    {
                        Status = false,
                        Message = "No dashboard data found.",
                        Data = null
                    };
                }

                DataRow row = summaryTable.Rows[0];

                // SP validation error
                if (row.Table.Columns.Contains("Status") && row["Status"] != DBNull.Value && !Convert.ToBoolean(row["Status"]))
                {
                    return new AttendanceDashboardResponse
                    {
                        Status = false,
                        Message = row.Table.Columns.Contains("Message") && row["Message"] != DBNull.Value ? row["Message"].ToString()! : "Unable to retrieve dashboard data.",
                        Data = null
                    };
                }

                var dashboard = new AttendanceDashboardModel
                {
                    Present = row["Present"] != DBNull.Value ? Convert.ToInt32(row["Present"]) : 0,
                    Absent = row["Absent"] != DBNull.Value ? Convert.ToInt32(row["Absent"]) : 0,
                    Late = row["Late"] != DBNull.Value ? Convert.ToInt32(row["Late"]) : 0,
                    OnLeave = row["Leave"] != DBNull.Value ? Convert.ToInt32(row["Leave"]) : 0,          // was "OnLeave"
                    TotalAttendance = row["TotalDays"] != DBNull.Value ? Convert.ToInt32(row["TotalDays"]) : 0, // was "TotalAttendance"
                    FromDate = row["FromDate"] != DBNull.Value ? Convert.ToDateTime(row["FromDate"]) : DateTime.MinValue,
                    ToDate = row["ToDate"] != DBNull.Value ? Convert.ToDateTime(row["ToDate"]) : DateTime.MinValue
                };

                if (dt.Tables.Count > 1)
                {
                    DataTable trendTable = dt.Tables[1];

                    foreach (DataRow trendRow in trendTable.Rows)
                    {
                        dashboard.AttendanceTrend.Add(new AttendanceTrendModel
                            {
                                Date = trendRow["AttendanceDate"] != DBNull.Value ? Convert.ToDateTime(trendRow["AttendanceDate"]) : DateTime.MinValue, // was "Date"
                                Present = Convert.ToInt32(trendRow["IsPresent"]),   // was "Present"
                                Absent = Convert.ToInt32(trendRow["IsAbsent"]),     // was "Absent"
                                Late = Convert.ToInt32(trendRow["IsLate"]),         // was "Late"
                                OnLeave = Convert.ToInt32(trendRow["IsLeave"])
                            }
                        );
                    }
                }
                return new AttendanceDashboardResponse
                {
                    Status = true,
                    Message ="Attendance dashboard count retrieved successfully.",
                    Data = dashboard
                };
            }
            catch (SqlException ex)
            {
                return new AttendanceDashboardResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                return new AttendanceDashboardResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}",
                    Data = null
                };
            }
        }

       /* private List<AttendanceTrendModel> MapAttendanceTrend(DataTable dt)
        {
            var list = new List<AttendanceTrendModel>();

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new AttendanceTrendModel
                {
                    Date = row["Date"] != DBNull.Value? Convert.ToDateTime(row["Date"]): DateTime.MinValue,
                    Present = row["Present"] != DBNull.Value? Convert.ToInt32(row["Present"]): 0,
                    Absent = row["Absent"] != DBNull.Value? Convert.ToInt32(row["Absent"]): 0,
                    Late = row["Late"] != DBNull.Value? Convert.ToInt32(row["Late"]): 0,
                    OnLeave = row["OnLeave"] != DBNull.Value? Convert.ToInt32(row["OnLeave"]): 0
                });
            }

            return list;
        }*/

        public async Task<AttendanceReportEmployeesResponse> GetReportEmployees(AttendanceReportEmployeesRequest request, int loggedInEmployeeId, string viewScope)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_ORG_Attendance",
                    new SqlParameter("@Status", "GET_REPORT_EMPLOYEES"),
                    new SqlParameter("@FromDate", request.FromDate.HasValue ? request.FromDate.Value.Date : (object)DBNull.Value),
                    new SqlParameter("@ToDate", request.ToDate.HasValue ? request.ToDate.Value.Date : (object)DBNull.Value),
                    new SqlParameter("@CompanyId", request.CompanyId ?? (object)DBNull.Value),
                    new SqlParameter("@OfficeId", request.OfficeId ?? (object)DBNull.Value),
                    new SqlParameter("@DepartmentId", request.DepartmentId ?? (object)DBNull.Value),
                    new SqlParameter("@Search", string.IsNullOrWhiteSpace(request.Search) ? DBNull.Value : request.Search.Trim()),
                    new SqlParameter("@LoggedInEmployeeId", loggedInEmployeeId),
                    new SqlParameter("@ViewScope", viewScope)
                );

                // SP validation error comes back as (Status, Message)
                if (dt.Columns.Contains("Message") && !dt.Columns.Contains("EmployeeId"))
                {
                    return new AttendanceReportEmployeesResponse
                    {
                        Status = false,
                        Message = dt.Rows.Count > 0 ? dt.Rows[0]["Message"]?.ToString() ?? "Unable to fetch attendance report." : "Unable to fetch attendance report.",
                        ViewScope = viewScope
                    };
                }

                var list = new List<AttendanceReportEmployeeModel>();

                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new AttendanceReportEmployeeModel
                    {
                        EmployeeId = RInt(row, "EmployeeId"),
                        EmployeeCode = RStr(row, "EmployeeCode"),
                        EmployeeName = RStr(row, "EmployeeName"),
                        CompanyId = RNInt(row, "CompanyId"),
                        OfficeId = RNInt(row, "OfficeId"),
                        OfficeName = RStr(row, "OfficeName"),
                        DepartmentId = RNInt(row, "DepartmentId"),
                        DepartmentName = RStr(row, "DepartmentName"),
                        DesignationId = RNInt(row, "DesignationId"),
                        DesignationName = RStr(row, "DesignationName"),
                        ReportingManagerId = RNInt(row, "ReportingManagerId"),
                        ReportingHRManagerId = RNInt(row, "ReportingHRManagerId"),

                        PresentDays = RInt(row, "PresentDays"),
                        AbsentDays = RInt(row, "AbsentDays"),
                        HalfDays = RInt(row, "HalfDays"),
                        LeaveDays = RInt(row, "LeaveDays"),
                        WFHDays = RInt(row, "WFHDays"),
                        LateDays = RInt(row, "LateDays"),
                        HolidayDays = RInt(row, "HolidayDays"),
                        WeekOffDays = RInt(row, "WeekOffDays"),
                        MarkedDays = RInt(row, "MarkedDays"),
                        TotalWorkingHours = RDec(row, "TotalWorkingHours")
                    });
                }

                return new AttendanceReportEmployeesResponse
                {
                    Status = true,
                    Message = "Attendance report fetched successfully.",
                    ViewScope = viewScope,
                    Data = list
                };
            }
            catch (SqlException ex)
            {
                return new AttendanceReportEmployeesResponse { Status = false, Message = $"Database Error: {ex.Message}", ViewScope = viewScope };
            }
            catch (Exception ex)
            {
                return new AttendanceReportEmployeesResponse { Status = false, Message = $"Error: {ex.Message}", ViewScope = viewScope };
            }
        }

        // ---------- 2. one employee, From - To ----------
        public async Task<AttendanceEmployeeReportResponse> GetEmployeeReport(AttendanceEmployeeReportRequest request, int loggedInEmployeeId, string viewScope)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync("USP_ORG_Attendance",
                    new SqlParameter("@Status", "GET_EMPLOYEE_ATTENDANCE"),
                    new SqlParameter("@EmployeeId", request.EmployeeId),
                    new SqlParameter("@FromDate", request.FromDate.HasValue ? request.FromDate.Value.Date : (object)DBNull.Value),
                    new SqlParameter("@ToDate", request.ToDate.HasValue ? request.ToDate.Value.Date : (object)DBNull.Value),
                    new SqlParameter("@LoggedInEmployeeId", loggedInEmployeeId),
                    new SqlParameter("@ViewScope", viewScope)
                );

                if (ds == null || ds.Tables.Count == 0)
                {
                    return new AttendanceEmployeeReportResponse { Status = false, Message = "No attendance data found." };
                }

                var summaryTable = ds.Tables[0];

                // SP validation / permission error
                if (summaryTable.Columns.Contains("Message") && !summaryTable.Columns.Contains("EmployeeId"))
                {
                    return new AttendanceEmployeeReportResponse
                    {
                        Status = false,
                        Message = summaryTable.Rows.Count > 0 ? summaryTable.Rows[0]["Message"]?.ToString() ?? "Unable to fetch attendance." : "Unable to fetch attendance."
                    };
                }

                if (summaryTable.Rows.Count == 0)
                {
                    return new AttendanceEmployeeReportResponse { Status = false, Message = "Employee not found." };
                }

                var s = summaryTable.Rows[0];

                var report = new AttendanceEmployeeReportModel
                {
                    Summary = new AttendanceEmployeeSummaryModel
                    {
                        EmployeeId = RInt(s, "EmployeeId"),
                        EmployeeCode = RStr(s, "EmployeeCode"),
                        EmployeeName = RStr(s, "EmployeeName"),
                        OfficeName = RStr(s, "OfficeName"),
                        DepartmentName = RStr(s, "DepartmentName"),
                        DesignationName = RStr(s, "DesignationName"),
                        PresentDays = RInt(s, "PresentDays"),
                        AbsentDays = RInt(s, "AbsentDays"),
                        HalfDays = RInt(s, "HalfDays"),
                        LeaveDays = RInt(s, "LeaveDays"),
                        WFHDays = RInt(s, "WFHDays"),
                        LateDays = RInt(s, "LateDays"),
                        HolidayDays = RInt(s, "HolidayDays"),
                        WeekOffDays = RInt(s, "WeekOffDays"),
                        TotalWorkingHours = RDec(s, "TotalWorkingHours"),
                        TotalOvertimeHours = RDec(s, "TotalOvertimeHours")
                    }
                };

                if (ds.Tables.Count > 1)
                {
                    foreach (DataRow d in ds.Tables[1].Rows)
                    {
                        report.Days.Add(new AttendanceEmployeeDayModel
                        {
                            AttendanceDate = RDate(d, "AttendanceDate"),
                            AttendanceId = RNInt(d, "AttendanceId"),
                            StatusCode = RStr(d, "StatusCode") ?? "A",
                            StatusName = RStr(d, "StatusName") ?? "Absent",
                            IsPresent = RBool(d, "IsPresent"),
                            IsLeave = RBool(d, "IsLeave"),
                            IsLate = RBool(d, "IsLate"),
                            FirstInTime = RNDate(d, "FirstInTime"),
                            LastOutTime = RNDate(d, "LastOutTime"),
                            WorkingHours = RNDec(d, "WorkingHours"),
                            BreakHours = RNDec(d, "BreakHours"),
                            OvertimeHours = RNDec(d, "OvertimeHours"),
                            LateMinutes = RNInt(d, "LateMinutes"),
                            EarlyExitMinutes = RNInt(d, "EarlyExitMinutes"),
                            WorkMode = RStr(d, "WorkMode"),
                            ShiftId = RNInt(d, "Ref_ShiftId"),
                            ShiftName = RStr(d, "ShiftName"),
                            Remarks = RStr(d, "Remarks")
                        });
                    }
                }

                return new AttendanceEmployeeReportResponse
                {
                    Status = true,
                    Message = "Employee attendance fetched successfully.",
                    Data = report
                };
            }
            catch (SqlException ex)
            {
                return new AttendanceEmployeeReportResponse { Status = false, Message = $"Database Error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new AttendanceEmployeeReportResponse { Status = false, Message = $"Error: {ex.Message}" };
            }
        }

        // ==================== REGULARIZATION ====================

        public Task<RegularizationListResponse> GetMyRegularizations(int loggedInEmployeeId)
            => GetRegularizations("GET_MY", loggedInEmployeeId);

        public Task<RegularizationListResponse> GetTeamRegularizations(int loggedInEmployeeId)
            => GetRegularizations("GET_TEAM", loggedInEmployeeId);

        private async Task<RegularizationListResponse> GetRegularizations(string status, int loggedInEmployeeId)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_ORG_AttendanceRegularization",
                    new SqlParameter("@Status", status),
                    new SqlParameter("@LoggedInEmployeeId", loggedInEmployeeId)
                );

                // SP validation error comes back as (Status, Message)
                if (dt.Columns.Contains("Message") && !dt.Columns.Contains("RegularizationId"))
                {
                    return new RegularizationListResponse
                    {
                        Status = false,
                        Message = dt.Rows.Count > 0 ? dt.Rows[0]["Message"]?.ToString() ?? "Unable to fetch requests." : "Unable to fetch requests."
                    };
                }

                var list = new List<RegularizationModel>();

                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new RegularizationModel
                    {
                        RegularizationId = RInt(row, "RegularizationId"),
                        RequestNo = RStr(row, "RequestNo"),
                        EmployeeId = RInt(row, "EmployeeId"),
                        EmployeeCode = RStr(row, "EmployeeCode"),
                        EmployeeName = RStr(row, "EmployeeName"),
                        AttendanceDate = RDate(row, "AttendanceDate"),
                        RegularizationType = RStr(row, "RegularizationType") ?? string.Empty,
                        CurrentInTime = RNDate(row, "CurrentInTime"),
                        CurrentOutTime = RNDate(row, "CurrentOutTime"),
                        RequestedInTime = RNDate(row, "RequestedInTime"),
                        RequestedOutTime = RNDate(row, "RequestedOutTime"),
                        Reason = RStr(row, "Reason"),
                        Status = RStr(row, "RequestStatus") ?? "PENDING",
                        AppliedOn = RNDate(row, "AppliedOn"),
                        ReviewedBy = RNInt(row, "ReviewedBy"),
                        ReviewedByName = RStr(row, "ReviewedByName"),
                        ReviewedOn = RNDate(row, "ReviewedOn"),
                        ReviewerRemarks = RStr(row, "ReviewerRemarks")
                    });
                }

                return new RegularizationListResponse
                {
                    Status = true,
                    Message = "Regularization requests fetched successfully.",
                    Data = list
                };
            }
            catch (SqlException ex)
            {
                return new RegularizationListResponse { Status = false, Message = $"Database Error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new RegularizationListResponse { Status = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<RegularizationActionResponse> ApplyRegularization(ApplyRegularizationRequest request, int loggedInEmployeeId)
        {
            var result = await RunRegularizationAction(
                new SqlParameter("@Status", "APPLY"),
                new SqlParameter("@LoggedInEmployeeId", loggedInEmployeeId),
                new SqlParameter("@AttendanceDate", request.AttendanceDate.HasValue ? request.AttendanceDate.Value.Date : DBNull.Value),
                new SqlParameter("@RegularizationType", string.IsNullOrWhiteSpace(request.RegularizationType) ? DBNull.Value : request.RegularizationType.Trim()),
                new SqlParameter("@RequestedInTime", request.RequestedInTime ?? (object)DBNull.Value),
                new SqlParameter("@RequestedOutTime", request.RequestedOutTime ?? (object)DBNull.Value),
                new SqlParameter("@Reason", string.IsNullOrWhiteSpace(request.Reason) ? DBNull.Value : request.Reason.Trim())
            );

            if (result.Status)
                await NotifyManagersOfNewRegularization(loggedInEmployeeId, request, result);

            return result;
        }

        /// <summary>
        /// Best-effort push to the employee's reporting manager and reporting HR
        /// manager, the same people GET_TEAM lets review the request.
        /// Never fails the request.
        /// </summary>
        private async Task NotifyManagersOfNewRegularization(int employeeId, ApplyRegularizationRequest request, RegularizationActionResponse result)
        {
            try
            {
                string employeeName = string.Empty;
                var managerIds = new HashSet<int>();

                // Read-only lookup; same columns the regularization SP uses for GET_TEAM.
                await using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    await using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"SELECT Ref_ReportingManagerId, Ref_ReportingHRManagerId,
                                               LTRIM(RTRIM(ISNULL(FirstName,'') + ' ' + ISNULL(LastName,''))) AS EmployeeName
                                        FROM dbo.ORG_Employee WHERE EmployeeId = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", employeeId));

                    await using var r = await cmd.ExecuteReaderAsync();
                    if (await r.ReadAsync())
                    {
                        if (!r.IsDBNull(0)) managerIds.Add(Convert.ToInt32(r.GetValue(0)));
                        if (!r.IsDBNull(1)) managerIds.Add(Convert.ToInt32(r.GetValue(1)));
                        employeeName = r.IsDBNull(2) ? string.Empty : r.GetString(2);
                    }
                }

                managerIds.Remove(employeeId);
                if (managerIds.Count == 0)
                {
                    _logger.LogWarning("Regularization {Id}: employee {EmployeeId} has no reporting manager, notification skipped.", result.RegularizationId, employeeId);
                    return;
                }

                var who = string.IsNullOrWhiteSpace(employeeName) ? "An employee" : employeeName;
                var date = request.AttendanceDate?.ToString("dd-MMM-yyyy") ?? string.Empty;

                foreach (var managerId in managerIds)
                {
                    try
                    {
                        await _schedulerExecutionService.SendEmployeeNotificationAsync(
                            schedulerJobItemId: null,
                            employeeId: managerId,
                            entityId: result.RegularizationId ?? 0,
                            title: "New Regularization Request",
                            message: $"{who} applied for regularization {result.RequestNo} for {date}.",
                            entityType: "REGULARIZATION_REQUEST",
                            recipientType: "MANAGER"
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Regularization {Id}: notification to manager {ManagerId} failed.", result.RegularizationId, managerId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Regularization {Id}: manager notification failed.", result.RegularizationId);
            }
        }

        public Task<RegularizationActionResponse> CancelRegularization(RegularizationActionRequest request, int loggedInEmployeeId)
            => RunRegularizationAction(
                new SqlParameter("@Status", "CANCEL"),
                new SqlParameter("@LoggedInEmployeeId", loggedInEmployeeId),
                new SqlParameter("@RegularizationId", request.RegularizationId),
                new SqlParameter("@Reason", string.IsNullOrWhiteSpace(request.Reason) ? DBNull.Value : request.Reason.Trim())
            );

        // On approval the SP writes the requested punches (source REGULARIZATION)
        // and recalculates that day's attendance.
        public async Task<RegularizationActionResponse> ApproveRegularization(RegularizationActionRequest request, int loggedInEmployeeId)
        {
            var target = await FindTeamRegularization(loggedInEmployeeId, request.RegularizationId);

            var result = await RunRegularizationAction(
                new SqlParameter("@Status", "APPROVE"),
                new SqlParameter("@LoggedInEmployeeId", loggedInEmployeeId),
                new SqlParameter("@RegularizationId", request.RegularizationId),
                new SqlParameter("@Remarks", string.IsNullOrWhiteSpace(request.Remarks) ? DBNull.Value : request.Remarks.Trim())
            );

            if (result.Status)
                await NotifyRegularizationDecision(target, request.RegularizationId, approved: true, request.Remarks);

            return result;
        }

        public async Task<RegularizationActionResponse> RejectRegularization(RegularizationActionRequest request, int loggedInEmployeeId)
        {
            var target = await FindTeamRegularization(loggedInEmployeeId, request.RegularizationId);

            var result = await RunRegularizationAction(
                new SqlParameter("@Status", "REJECT"),
                new SqlParameter("@LoggedInEmployeeId", loggedInEmployeeId),
                new SqlParameter("@RegularizationId", request.RegularizationId),
                new SqlParameter("@Reason", string.IsNullOrWhiteSpace(request.Reason) ? DBNull.Value : request.Reason.Trim())
            );

            if (result.Status)
                await NotifyRegularizationDecision(target, request.RegularizationId, approved: false, request.Reason);

            return result;
        }

        /// <summary>
        /// Finds the request in the reviewer's team list (GET_TEAM), which gives the
        /// applicant and date. The APPROVE/REJECT result sets don't include them.
        /// Returns null if not found; the notification is then skipped.
        /// </summary>
        private async Task<RegularizationModel?> FindTeamRegularization(int loggedInEmployeeId, int regularizationId)
        {
            try
            {
                var team = await GetTeamRegularizations(loggedInEmployeeId);
                return team.Data?.FirstOrDefault(r => r.RegularizationId == regularizationId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Regularization {Id}: could not load request for notification.", regularizationId);
                return null;
            }
        }

        /// <summary>Best-effort push to the applicant. Never fails the request.</summary>
        private async Task NotifyRegularizationDecision(RegularizationModel? target, int regularizationId, bool approved, string? remarks)
        {
            if (target == null || target.EmployeeId <= 0)
            {
                _logger.LogWarning("Regularization {Id}: applicant not found, notification skipped.", regularizationId);
                return;
            }

            try
            {
                var date = target.AttendanceDate.ToString("dd-MMM-yyyy");
                var note = string.IsNullOrWhiteSpace(remarks) ? "." : $": {remarks.Trim()}";

                await _schedulerExecutionService.SendEmployeeNotificationAsync(
                    schedulerJobItemId: null,
                    employeeId: target.EmployeeId,
                    entityId: regularizationId,
                    title: approved ? "Regularization Approved" : "Regularization Rejected",
                    message: $"Your regularization request {target.RequestNo} for {date} was {(approved ? "approved" : "rejected")}{note}",
                    entityType: "REGULARIZATION",
                    recipientType: "EMPLOYEE"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Regularization {Id}: notification failed.", regularizationId);
            }
        }

        private async Task<RegularizationActionResponse> RunRegularizationAction(params SqlParameter[] parameters)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_ORG_AttendanceRegularization", parameters);

                if (dt.Rows.Count == 0)
                {
                    return new RegularizationActionResponse
                    {
                        Status = false,
                        Message = "No response received from regularization procedure."
                    };
                }

                var row = dt.Rows[0];

                return new RegularizationActionResponse
                {
                    Status = RBool(row, "Status"),
                    Message = RStr(row, "Message") ?? string.Empty,
                    RegularizationId = RNInt(row, "RegularizationId"),
                    RequestNo = RStr(row, "RequestNo"),
                    AttendanceId = RNInt(row, "AttendanceId")
                };
            }
            catch (SqlException ex)
            {
                return new RegularizationActionResponse { Status = false, Message = $"Database Error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new RegularizationActionResponse { Status = false, Message = $"Error: {ex.Message}" };
            }
        }
    }
}
