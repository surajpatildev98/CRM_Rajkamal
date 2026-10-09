using CRMWebServices.Interfaces;
using CRMWebServices.Models.Attendance;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CRMWebServices.Repository
{
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly IDatabaseService _database;
        private static bool Has(DataRow r, string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value;
        private static int RInt(DataRow r, string c) => Has(r, c) ? Convert.ToInt32(r[c]) : 0;
        private static int? RNInt(DataRow r, string c) => Has(r, c) ? Convert.ToInt32(r[c]) : null;
        private static decimal RDec(DataRow r, string c) => Has(r, c) ? Convert.ToDecimal(r[c]) : 0;
        private static decimal? RNDec(DataRow r, string c) => Has(r, c) ? Convert.ToDecimal(r[c]) : null;
        private static string? RStr(DataRow r, string c) => Has(r, c) ? r[c].ToString() : null;
        private static bool RBool(DataRow r, string c) => Has(r, c) && Convert.ToBoolean(r[c]);
        private static DateTime RDate(DataRow r, string c) => Has(r, c) ? Convert.ToDateTime(r[c]) : DateTime.MinValue;
        private static DateTime? RNDate(DataRow r, string c) => Has(r, c) ? Convert.ToDateTime(r[c]) : null;

        public AttendanceRepository(IDatabaseService database)
        {
            _database = database;
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


        public async Task<AttendanceResponse> GetAttendance(
            AttendanceRequest request)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_ORG_Attendance",
                    new SqlParameter("@Status", "GET"),
                    new SqlParameter("@AttendanceDate", request.AttendanceDate.HasValue ? request.AttendanceDate.Value.Date : DBNull.Value),
                    new SqlParameter("@OfficeId",request.OfficeId ?? (object)DBNull.Value),
                    new SqlParameter("@DepartmentId",request.DepartmentId ?? (object)DBNull.Value),
                    new SqlParameter("@Search",string.IsNullOrWhiteSpace(request.Search) ? DBNull.Value : request.Search.Trim())
                );

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
                    Message =$"Database Error: {ex.Message}",
                    Data = new List<AttendanceModel>()
                };
            }
            catch (Exception ex)
            {
                return new AttendanceResponse
                {
                    Status = false,
                    Message =$"Error: {ex.Message}",
                    Data = new List<AttendanceModel>()
                };
            }
        }

        public async Task<AttendancePunchResponse> GetPunches(AttendancePunchRequest request)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_ORG_Attendance",
                    new SqlParameter("@Status", "GET_PUNCHES"),
                    new SqlParameter("@AttendanceId",request.AttendanceId)
                );

                var list = new List<AttendancePunchModel>();

                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new AttendancePunchModel
                    {
                        AttendancePunchId = row["AttendancePunchId"] != DBNull.Value ? Convert.ToInt32(row["AttendancePunchId"]) : 0,
                        AttendanceId = row["AttendanceId"] != DBNull.Value ? Convert.ToInt32(row["AttendanceId"]): 0,
                        EmployeeId = row["EmployeeId"] != DBNull.Value ? Convert.ToInt32(row["EmployeeId"]) : 0,
                        PunchDateTime = row["PunchDateTime"] != DBNull.Value ? Convert.ToDateTime(row["PunchDateTime"]) : DateTime.MinValue,
                        PunchType = row["PunchType"] != DBNull.Value ? row["PunchType"]?.ToString() : null,
                        PunchSource = row["PunchSource"] != DBNull.Value ? row["PunchSource"]?.ToString() : null,
                        Remarks = row["Remarks"] != DBNull.Value ? row["Remarks"]?.ToString() : null,
                        CreatedOn = row["CreatedOn"] != DBNull.Value ? Convert.ToDateTime(row["CreatedOn"]) : null,
                        CreatedBy = row["CreatedBy"] != DBNull.Value ? Convert.ToInt32(row["CreatedBy"]) : null
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
        public async Task<AttendanceEmployeeReportResponse> GetEmployeeReport(
            AttendanceEmployeeReportRequest request, int loggedInEmployeeId, string viewScope)
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
    }
}
