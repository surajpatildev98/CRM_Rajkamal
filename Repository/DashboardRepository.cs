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
    }
}