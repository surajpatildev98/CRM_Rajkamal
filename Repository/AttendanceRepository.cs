using CRMWebServices.Interfaces;
using CRMWebServices.Models.Attendance;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CRMWebServices.Repository
{
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly IDatabaseService _database;

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
                var dt = await _database.ExecuteDataTableAsync(
                    "USP_ORG_Attendance",

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

                    Message =
                        $"Database Error: {ex.Message}",

                    Data = new List<AttendanceModel>()
                };
            }
            catch (Exception ex)
            {
                return new AttendanceResponse
                {
                    Status = false,

                    Message =
                        $"Error: {ex.Message}",

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

        public async Task<ManualPunchResponse> ManualPunch(
    ManualPunchRequest request)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    "USP_ORG_Attendance",

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
    }
}
