using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using CRMWebServices.Models.Shift;
using CRMWebServices.Services;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CRMWebServices.Repository
{
    public class ShiftRepository : IShiftRepository
    {
        private readonly IDatabaseService _database;
        public ShiftRepository(IDatabaseService database)
        {
            _database = database;
        }

        private List<ShiftModel> MapToDropdown(DataTable dt)
        {
            var list = new List<ShiftModel>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new ShiftModel
                {
                    ShiftId = row["ShiftId"] != DBNull.Value ? Convert.ToInt32(row["ShiftId"]) : 0,

                    ShiftCode = row["ShiftCode"]?.ToString() ?? string.Empty,

                    ShiftName = row["ShiftName"]?.ToString() ?? string.Empty,

                    StartTime = row["StartTime"] != DBNull.Value ? (TimeSpan)row["StartTime"] : TimeSpan.Zero,

                    EndTime = row["EndTime"] != DBNull.Value ? (TimeSpan)row["EndTime"] : TimeSpan.Zero,

                    BreakStartTime = row["BreakStartTime"] != DBNull.Value ? (TimeSpan)row["BreakStartTime"] : TimeSpan.Zero,

                    BreakEndTime = row["BreakEndTime"] != DBNull.Value ? (TimeSpan)row["BreakEndTime"] : TimeSpan.Zero,

                    GraceInMinutes = row["GraceInMinutes"] != DBNull.Value ? Convert.ToInt32(row["GraceInMinutes"]) : 0,

                    GraceOutMinutes = row["GraceOutMinutes"] != DBNull.Value ? Convert.ToInt32(row["GraceOutMinutes"]) : 0,

                    MinimumWorkingHours = row["MinimumWorkingHours"] != DBNull.Value ? Convert.ToDecimal(row["MinimumWorkingHours"]) : 0,

                    IsNightShift = row["IsNightShift"] != DBNull.Value && Convert.ToBoolean(row["IsNightShift"]),

                    IsActive = row["IsActive"] != DBNull.Value && Convert.ToBoolean(row["IsActive"])
                });
            }
            return list;
        }



        /*  public async Task<List<ShiftModel>> GetShifts()
          {
              var dt = await _database.ExecuteDataTableAsync("USP_ORG_SHIFTMANAGEMENT",
                  new SqlParameter("@Status", "GET_SHIFT"));
              return MapToDropdown(dt);
          }*/

        public async Task<ShiftResponse> GetShifts()
        {
            var ds = await _database.ExecuteDataSetAsync(
                "USP_ORG_SHIFTMANAGEMENT",
                new SqlParameter("@Status", "GET_SHIFT"));

            var response = new ShiftResponse();

            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                var row = ds.Tables[0].Rows[0];

                response.Status = Convert.ToBoolean(row["Status"]);
                response.Message = row["Message"]?.ToString() ?? "";
            }

            if (ds.Tables.Count > 1)
            {
                response.Data = MapToDropdown(ds.Tables[1]);
            }

            return response;
        }

        public async Task<ApiResponse> AddShift(ShiftModel model)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    "USP_ORG_SHIFTMANAGEMENT",

                    new SqlParameter("@Status", "ADD_SHIFT"),
                    new SqlParameter("@ShiftCode", model.ShiftCode),
                    new SqlParameter("@ShiftName", model.ShiftName),
                    new SqlParameter("@StartTime", model.StartTime),
                    new SqlParameter("@EndTime", model.EndTime),
                    new SqlParameter("@BreakStartTime", model.BreakStartTime),
                    new SqlParameter("@BreakEndTime", model.BreakEndTime),
                    new SqlParameter("@GraceInMinutes", model.GraceInMinutes),
                    new SqlParameter("@GraceOutMinutes", model.GraceOutMinutes),
                    new SqlParameter("@MinimumWorkingHours", model.MinimumWorkingHours),
                    new SqlParameter("@IsNightShift", model.IsNightShift),
                    new SqlParameter("@IsActive", model.IsActive),
                    new SqlParameter("@CreatedBy", model.CreatedBy)
                );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "No response returned from database."
                    };
                }

                var row = dt.Rows[0];

                return new ApiResponse
                {
                    Status = Convert.ToBoolean(row["Status"]),
                    Message = row["Message"]?.ToString(),
                    Data = Convert.ToBoolean(row["Status"])
                        ? new
                        {
                            ShiftId = Convert.ToInt32(row["ShiftId"])
                        }
                        : null
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

        public async Task<ShiftResponse> UpdateShift(ShiftModel shift)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync(
                    "USP_ORG_SHIFTMANAGEMENT",

                    new SqlParameter("@Status", "UPDATE_SHIFT"),

                    new SqlParameter("@ShiftId", shift.ShiftId),

                    new SqlParameter("@ShiftCode", shift.ShiftCode),
                    new SqlParameter("@ShiftName", shift.ShiftName),

                    new SqlParameter("@StartTime", shift.StartTime),
                    new SqlParameter("@EndTime", shift.EndTime),

                    new SqlParameter("@BreakStartTime", shift.BreakStartTime),
                    new SqlParameter("@BreakEndTime", shift.BreakEndTime),

                    new SqlParameter("@GraceInMinutes", shift.GraceInMinutes),
                    new SqlParameter("@GraceOutMinutes", shift.GraceOutMinutes),

                    new SqlParameter("@MinimumWorkingHours", shift.MinimumWorkingHours),

                    new SqlParameter("@IsNightShift", shift.IsNightShift),
                    new SqlParameter("@IsActive", shift.IsActive)
                );

                var response = new ShiftResponse();

                // Response table
                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    var row = ds.Tables[0].Rows[0];

                    response.Status =
                        row["Status"] != DBNull.Value &&
                        Convert.ToBoolean(row["Status"]);

                    response.Message =
                        row["Message"]?.ToString() ?? "";
                }

                // Updated shift data
                if (ds.Tables.Count > 1 && ds.Tables[1].Rows.Count > 0)
                {
                    response.Data = MapToDropdown(ds.Tables[1]);
                }

                return response;
            }
            catch (Exception ex)
            {
                return new ShiftResponse
                {
                    Status = false,
                    Message = ex.Message,
                    Data = new List<ShiftModel>()
                };
            }
        }

        public async Task<ApiResponse> DeleteShift(int shiftId)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    "USP_ORG_SHIFTMANAGEMENT",

                    new SqlParameter("@Status", "DELETE_SHIFT"),
                    new SqlParameter("@ShiftId", shiftId)
                );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "No response returned from database."
                    };
                }

                var row = dt.Rows[0];

                return new ApiResponse
                {
                    Status =
                        row["Status"] != DBNull.Value &&
                        Convert.ToBoolean(row["Status"]),

                    Message =
                        row["Message"]?.ToString() ?? ""
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
