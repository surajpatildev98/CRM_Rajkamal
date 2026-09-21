using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models.holiday;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CRMWebServices.Repository
{
    public class HolidayRepository : IHolidayRepository
    {
        private readonly IDatabaseService _database;
        public HolidayRepository(IDatabaseService database)
        {
            _database = database;
        }

        public async Task<ApiResponse> SaveHolidayCalendar(HolidayCalendarSaveRequest request)
        {
            try
            {
                // Create DataTable for HolidayListType
                var holidayTable = new DataTable();
                holidayTable.Columns.Add("HolidayId",typeof(int));
                holidayTable.Columns.Add("HolidayName",typeof(string));
                holidayTable.Columns.Add("HolidayDate",typeof(DateTime));
                holidayTable.Columns.Add("IsOptional",typeof(bool));
                holidayTable.Columns.Add("Remarks",typeof(string));
                // Add holidays to DataTable
                foreach (var holiday in request.Holidays)
                {
                    holidayTable.Rows.Add(
                        holiday.HolidayId > 0 ? holiday.HolidayId : DBNull.Value,
                        holiday.HolidayName,
                        holiday.HolidayDate,
                        holiday.IsOptional,
                        string.IsNullOrWhiteSpace(holiday.Remarks)? DBNull.Value : holiday.Remarks
                    );
                }

                // Call stored procedure
                var dt = await _database.ExecuteDataTableAsync("USP_ORG_HolidayManagement",
                    new SqlParameter("@Status", "SAVE_CALENDAR"),
                    new SqlParameter("@HolidayCalendarId", request.HolidayCalendarId ?? (object)DBNull.Value),
                    new SqlParameter("@CalendarName",request.CalendarName),
                    new SqlParameter("@CalendarYear",request.CalendarYear),
                    new SqlParameter("@Ref_CompanyId",request.CompanyId),
                    new SqlParameter("@Ref_OfficeId",request.OfficeId.HasValue ? request.OfficeId.Value : DBNull.Value),
                    new SqlParameter("@IsActive",request.IsActive),
                    new SqlParameter("@CreatedBy",request.CreatedBy),
                    new SqlParameter("@Holidays", holidayTable)
                    {
                        SqlDbType = SqlDbType.Structured,
                        TypeName = "dbo.HolidayListType"
                    }
                );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message ="No response returned from database."
                    };
                }

                var row = dt.Rows[0];

                return new ApiResponse
                {
                    Status =row["Status"] != DBNull.Value && Convert.ToBoolean(row["Status"]),
                    Message =row["Message"]?.ToString() ?? "",
                    Data =row["Status"] != DBNull.Value && Convert.ToBoolean(row["Status"])
                           ? new
                            {
                                HolidayCalendarId = row["HolidayCalendarId"] != DBNull.Value ? Convert.ToInt32(row["HolidayCalendarId"]) : 0
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

        public async Task<HolidayCalendarResponse> GetCalendars()
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync("USP_ORG_HolidayManagement",new SqlParameter("@Status", "GET_CALENDARS"));

                var response = new HolidayCalendarResponse();

                // Result Set 1 - Status and Message
                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    var row = ds.Tables[0].Rows[0];

                    response.Status = row["Status"] != DBNull.Value &&  Convert.ToBoolean(row["Status"]);
                    response.Message = row["Message"]?.ToString() ?? "";
                }

                // Result Set 2 - Calendar Data
                if (ds.Tables.Count > 1 && ds.Tables[1].Rows.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[1].Rows)
                    {
                        response.Data.Add(
                            new HolidayCalendarModel
                            {
                                HolidayCalendarId = row["HolidayCalendarId"] != DBNull.Value ? Convert.ToInt32( row["HolidayCalendarId"]) : 0,
                                CalendarName = row["CalendarName"]?.ToString() ?? "",
                                CalendarYear = row["CalendarYear"] != DBNull.Value  ? Convert.ToInt32(row["CalendarYear"]) : 0,
                                CompanyId = row["Ref_CompanyId"] != DBNull.Value ? Convert.ToInt32(row["Ref_CompanyId"]) : 0,
                                OfficeId = row["Ref_OfficeId"] != DBNull.Value ? Convert.ToInt32( row["Ref_OfficeId"]) : null,
                                IsActive = row["IsActive"] != DBNull.Value && Convert.ToBoolean( row["IsActive"]),
                                CreatedOn = row["CreatedOn"] != DBNull.Value ? Convert.ToDateTime( row["CreatedOn"]) : null,
                                CreatedBy = row["CreatedBy"] != DBNull.Value ? Convert.ToInt32(row["CreatedBy"]) : 0
                            }
                        );
                    }
                }

                return response;
            }
            catch (SqlException ex)
            {
                return new HolidayCalendarResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}",
                    Data = new List<HolidayCalendarModel>()
                };
            }
            catch (Exception ex)
            {
                return new HolidayCalendarResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}",
                    Data = new List<HolidayCalendarModel>()
                };
            }
        }

        public async Task<HolidayResponse> GetHolidays( int holidayCalendarId)
        {
            try
            {
                var response = new HolidayResponse();

                var ds = await _database.ExecuteDataSetAsync("USP_ORG_HolidayManagement",
                    new SqlParameter("@Status", "GET_HOLIDAYS"),
                    new SqlParameter("@HolidayCalendarId",holidayCalendarId)
                );

                //=========================================
                // Result Set 1 - Status and Message
                //=========================================

                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    var row = ds.Tables[0].Rows[0];

                    response.Status = row["Status"] != DBNull.Value && Convert.ToBoolean(row["Status"]);
                    response.Message = row["Message"]?.ToString() ?? "";
                }

                //=========================================
                // Result Set 2 - Holiday Data
                //=========================================

                if (ds.Tables.Count > 1 && ds.Tables[1].Rows.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[1].Rows)
                    {
                        response.Data.Add(
                            new HolidayModel
                            {
                                HolidayId =row["HolidayId"] != DBNull.Value ? Convert.ToInt32( row["HolidayId"]) : 0,
                                Ref_HolidayCalendarId =row["Ref_HolidayCalendarId"] != DBNull.Value ? Convert.ToInt32( row["Ref_HolidayCalendarId"]) : 0,
                                HolidayName = row["HolidayName"]?.ToString()?? "",
                                HolidayDate = row["HolidayDate"] != DBNull.Value ? Convert.ToDateTime( row["HolidayDate"]) : DateTime.MinValue,
                                IsOptional = row["IsOptional"] != DBNull.Value && Convert.ToBoolean(row["IsOptional"]),
                                Remarks = row["Remarks"] != DBNull.Value ? row["Remarks"]?.ToString() : null,
                                CreatedOn = row["CreatedOn"] != DBNull.Value ? Convert.ToDateTime(row["CreatedOn"]) : null,
                                CreatedBy = row["CreatedBy"] != DBNull.Value ? Convert.ToInt32(row["CreatedBy"]) : 0
                            }
                        );
                    }
                }

                return response;
            }
            catch (SqlException ex)
            {
                return new HolidayResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}",
                    Data = new List<HolidayModel>()
                };
            }
            catch (Exception ex)
            {
                return new HolidayResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}",
                    Data = new List<HolidayModel>()
                };
            }
        }
    }
}
