using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CRMWebServices.Repository
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly IDatabaseService _database;

        public DashboardRepository(IDatabaseService database)
        {
            _database = database;
        }

        #region  GetDashboardFull used for both Admin and Employee dashboards
        public async Task<ApiResponse> GetDashboardFull(int? employeeId, DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync("USP_CRM_DashboardManagement",
                    new SqlParameter("@Status", "GET_DASHBOARD_FULL"),
                    new SqlParameter("@EmployeeId", (object?)employeeId ?? DBNull.Value),
                    new SqlParameter("@FromDate", (object?)fromDate ?? DBNull.Value),
                    new SqlParameter("@ToDate", (object?)toDate ?? DBNull.Value),
                    new SqlParameter("@IsAdmin", employeeId == null ? 1 : 0)  // ← If employeeId is null, it's Admin viewing all
                );

                if (ds.Tables.Count < 7)
                {
                    return new ApiResponse { Status = false, Message = "Unexpected response from database." };
                }

                var kpi = ds.Tables[0].Rows.Count > 0 ? RowToDict(ds.Tables[0].Rows[0]) : new Dictionary<string, object>();
                var leadFunnel = ds.Tables[1].Rows.Cast<DataRow>().Select(RowToDict).ToList();
                var opportunityPipeline = ds.Tables[2].Rows.Cast<DataRow>().Select(RowToDict).ToList();
                var pendingTasks = ds.Tables[3].Rows.Cast<DataRow>().Select(RowToDict).ToList();
                var recentActivity = ds.Tables[4].Rows.Cast<DataRow>().Select(RowToDict).ToList();
                var assignedLeads = ds.Tables[5].Rows.Count > 0? RowToDict(ds.Tables[5].Rows[0]): new Dictionary<string, object>();
                var unassignedLeads = ds.Tables[6].Rows.Count > 0? RowToDict(ds.Tables[6].Rows[0]): new Dictionary<string, object>();

                return new ApiResponse
                {
                    Status = true,
                    Message = "Dashboard data fetched successfully.",
                    Data = new
                    {
                        kpi,
                        leadFunnel,
                        opportunityPipeline,
                        pendingTasks,
                        recentActivity,
                        assignedLeads,
                        unassignedLeads
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

        #region  GetEmployeePerformance used for Admin dashboard only
        public async Task<ApiResponse> GetEmployeePerformance()
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_CRM_DashboardManagement",
                    new SqlParameter("@Status", "GET_EMPLOYEE_PERFORMANCE"),
                    new SqlParameter("@EmployeeId", DBNull.Value),
                    new SqlParameter("@FromDate", DBNull.Value),
                    new SqlParameter("@ToDate", DBNull.Value),
                    new SqlParameter("@IsAdmin", 1)  // ← Employee performance is always Admin only
                );

                var data = dt.Rows.Cast<DataRow>().Select(RowToDict).ToList();

                return new ApiResponse { Status = true, Message = "Employee performance fetched successfully.", Data = data };
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

        #region  GetDashboardFull helper method to convert DataRow to Dictionary
        private Dictionary<string, object> RowToDict(DataRow row)
        {
            var dict = new Dictionary<string, object>();
            foreach (DataColumn col in row.Table.Columns)
                dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
            return dict;
        }
        #endregion

        #region  GetHrmsDashboardFull used for Employee / Manager / Admin HRMS dashboard
        public async Task<ApiResponse> GetHrmsDashboardFull(int employeeId, bool isAdmin)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync("USP_CRM_DashboardManagement",
                    new SqlParameter("@Status", "GET_HRMS_DASHBOARD_FULL"),
                    new SqlParameter("@EmployeeId", employeeId),          // always the logged-in user (personal cards need it)
                    new SqlParameter("@FromDate", DBNull.Value),
                    new SqlParameter("@ToDate", DBNull.Value),
                    new SqlParameter("@IsAdmin", isAdmin ? 1 : 0)         // 1 = org-wide scope
                );

                if (ds.Tables.Count < 18)
                {
                    return new ApiResponse { Status = false, Message = "Unexpected response from database." };
                }

                var kpi = ds.Tables[0].Rows.Count > 0 ? RowToDict(ds.Tables[0].Rows[0]) : new Dictionary<string, object>();

                // My cards (single row)
                var myAttendanceToday = ds.Tables[1].Rows.Count > 0 ? RowToDict(ds.Tables[1].Rows[0]) : null;
                var myMonthSummary = ds.Tables[2].Rows.Count > 0 ? RowToDict(ds.Tables[2].Rows[0]) : null;
                var myLeaveBalance = ds.Tables[3].Rows.Cast<DataRow>().Select(RowToDict).ToList();
                var myShift = ds.Tables[4].Rows.Count > 0 ? RowToDict(ds.Tables[4].Rows[0]) : null;
                var myResignation = ds.Tables[5].Rows.Count > 0 ? RowToDict(ds.Tables[5].Rows[0]) : null;
                var myNoticePeriod = ds.Tables[6].Rows.Count > 0 ? RowToDict(ds.Tables[6].Rows[0]) : null;
                var myProbation = ds.Tables[7].Rows.Count > 0 ? RowToDict(ds.Tables[7].Rows[0]) : null;
                var myRecentPunches = ds.Tables[8].Rows.Cast<DataRow>().Select(RowToDict).ToList();

                // Scope cards (org / team / self)
                var todayStatusBreakdown = ds.Tables[9].Rows.Cast<DataRow>().Select(RowToDict).ToList();
                var workModeSplit = ds.Tables[10].Rows.Cast<DataRow>().Select(RowToDict).ToList();
                var attendanceTrend = ds.Tables[11].Rows.Cast<DataRow>().Select(RowToDict).ToList();
                var activeResignations = ds.Tables[12].Rows.Cast<DataRow>().Select(RowToDict).ToList();
                var employeesOnProbation = ds.Tables[13].Rows.Cast<DataRow>().Select(RowToDict).ToList();
                var employmentTypeCount = ds.Tables[14].Rows.Cast<DataRow>().Select(RowToDict).ToList();

                // Org-wide feed cards
                var upcomingBirthdays = ds.Tables[15].Rows.Cast<DataRow>().Select(RowToDict).ToList();
                var workAnniversaries = ds.Tables[16].Rows.Cast<DataRow>().Select(RowToDict).ToList();
                var newJoiners = ds.Tables[17].Rows.Cast<DataRow>().Select(RowToDict).ToList();

                return new ApiResponse
                {
                    Status = true,
                    Message = "HRMS dashboard data fetched successfully.",
                    Data = new
                    {
                        kpi,
                        myAttendanceToday,
                        myMonthSummary,
                        myLeaveBalance,
                        myShift,
                        myResignation,
                        myNoticePeriod,
                        myProbation,
                        myRecentPunches,
                        todayStatusBreakdown,
                        workModeSplit,
                        attendanceTrend,
                        activeResignations,
                        employeesOnProbation,
                        employmentTypeCount,
                        upcomingBirthdays,
                        workAnniversaries,
                        newJoiners
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
    }
}