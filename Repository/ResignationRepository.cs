using CRMWebServices.Interfaces;
using Microsoft.Data.SqlClient;
using ResignationApi.Models;
using System.Data;

namespace CRMWebServices.Repository
{
    public class ResignationRepository : IResignationRepository
    {
        private const string Proc = "dbo.usp_Resignation_Manage";
        private readonly string _connectionString;
        private readonly IConfiguration _config;

        public ResignationRepository(IConfiguration config)
        {
            _config = config;
            _connectionString = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing.");
        }

        /* ------------------------------ small helpers ------------------------------ */

        private static SqlParameter P(string name, object? value) => new(name, value ?? DBNull.Value);

        private static SqlCommand Build(SqlConnection conn, string action, int? employeeId, params SqlParameter[] extra)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = Proc;
            cmd.Parameters.Add(P("@Action", action));
            cmd.Parameters.Add(P("@EmployeeId", employeeId));
            cmd.Parameters.AddRange(extra);
            return cmd;
        }

        private static string? Str(SqlDataReader r, string col)
        {
            var i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? null : r.GetString(i);
        }

        private static int Int(SqlDataReader r, string col)
        {
            var i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? 0 : r.GetInt32(i);
        }

        private static DateTime? Dt(SqlDataReader r, string col)
        {
            var i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? null : r.GetDateTime(i);
        }

        /// <summary>Runs the procedure and reads its 3 result sets: header, timeline, clearance.</summary>
        private async Task<List<ResignationDto>> QueryResignationsAsync(
            string action, int? employeeId, params SqlParameter[] extra)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = Build(conn, action, employeeId, extra);
            await using var r = await cmd.ExecuteReaderAsync();

            var list = new List<ResignationDto>();
            var map = new Dictionary<int, ResignationDto>();

            // 1) header
            while (await r.ReadAsync())
            {
                var d = new ResignationDto
                {
                    ResignationId = Int(r, "resignationId"),
                    EmployeeId = Int(r, "employeeId"),
                    EmployeeName = Str(r, "employeeName") ?? "",
                    EmployeeCode = Str(r, "employeeCode"),
                    DepartmentName = Str(r, "departmentName"),
                    DesignationName = Str(r, "designationName"),
                    ReasonId = Int(r, "reasonId"),
                    ReasonName = Str(r, "reasonName") ?? "",
                    ReasonDetails = Str(r, "reasonDetails") ?? "",
                    HandoverNotes = Str(r, "handoverNotes"),
                    ResignationDate = Dt(r, "resignationDate") ?? DateTime.Today,
                    NoticePeriodDays = Int(r, "noticePeriodDays"),
                    RequestedLastWorkingDate = Dt(r, "requestedLastWorkingDate") ?? DateTime.Today,
                    ApprovedLastWorkingDate = Dt(r, "approvedLastWorkingDate"),
                    Status = Str(r, "status") ?? "PENDING",
                    ApproverRemarks = Str(r, "approverRemarks"),
                    CurrentApprovalLevel = Int(r, "currentApprovalLevel"),
                };
                list.Add(d);
                map[d.ResignationId] = d;
            }

            // 2) timeline
            if (await r.NextResultAsync())
            {
                while (await r.ReadAsync())
                {
                    if (!map.TryGetValue(Int(r, "resignationId"), out var d)) continue;
                    d.Timeline.Add(new TimelineDto
                    {
                        Title = Str(r, "title") ?? "",
                        Remarks = Str(r, "remarks"),
                        ActionBy = Str(r, "actionBy"),
                        Date = Dt(r, "date") ?? DateTime.Now,
                    });
                }
            }

            // 3) clearance items
            if (await r.NextResultAsync())
            {
                while (await r.ReadAsync())
                {
                    if (!map.TryGetValue(Int(r, "resignationId"), out var d)) continue;
                    d.Clearance.Add(new ClearanceItemDto
                    {
                        ClearanceItemId = Int(r, "clearanceItemId"),
                        Department = Str(r, "department") ?? "",
                        ItemName = Str(r, "itemName") ?? "",
                        Status = Str(r, "status") ?? "PENDING",
                        Remarks = Str(r, "remarks"),
                        ClearedBy = Str(r, "clearedBy"),
                        ClearedOn = Dt(r, "clearedOn"),
                    });
                }
            }

            return list;
        }

        /// <summary>For actions whose result we do not need (withdraw / approve / reject).</summary>
        private async Task ExecuteAsync(string action, int? employeeId, params SqlParameter[] extra)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = Build(conn, action, employeeId, extra);
            await cmd.ExecuteNonQueryAsync();
        }

        /* ------------------------------ lookups ------------------------------ */

        public async Task<List<ReasonDto>> GetReasonsAsync()
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = Build(conn, "REASONS", null);
            await using var r = await cmd.ExecuteReaderAsync();

            var list = new List<ReasonDto>();
            while (await r.ReadAsync())
                list.Add(new ReasonDto { ReasonId = Int(r, "reasonId"), ReasonName = Str(r, "reasonName") ?? "" });
            return list;
        }

        public async Task<NoticePolicyDto> GetNoticePolicyAsync(int employeeId)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = Build(conn, "NOTICE_POLICY", employeeId);
            await using var r = await cmd.ExecuteReaderAsync();

            if (!await r.ReadAsync()) return new NoticePolicyDto { NoticePeriodDays = 60 };
            return new NoticePolicyDto
            {
                NoticePeriodDays = Int(r, "noticePeriodDays"),
                IsOnProbation = r.GetBoolean(r.GetOrdinal("isOnProbation")),
            };
        }

        /// <summary>
        /// Reporting manager comes from vw_ResignationEmployee.ManagerEmployeeId.
        /// HR approver comes from appsettings Resignation:HRManagerEmployeeId.
        /// </summary>
        public async Task<(int? ManagerId, int? HrManagerId)> GetApproversAsync(int employeeId)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT ManagerEmployeeId FROM dbo.vw_ResignationEmployee WHERE EmployeeId = @id";
            cmd.Parameters.Add(P("@id", employeeId));

            var value = await cmd.ExecuteScalarAsync();
            int? managerId = value is null or DBNull ? null : Convert.ToInt32(value);
            int? hrId = _config.GetValue<int?>("Resignation:HRManagerEmployeeId");
            return (managerId, hrId);
        }

        /* ------------------------------ employee ------------------------------ */

        public async Task<ResignationDto?> GetMyAsync(int employeeId)
            => (await QueryResignationsAsync("MY", employeeId)).FirstOrDefault();

        public async Task<ResignationDto> ApplyAsync(
            int employeeId, ApplyResignationRequest req, int noticeDays, int? managerId, int? hrManagerId)
        {
            var list = await QueryResignationsAsync("APPLY", employeeId,
                P("@ReasonId", req.ReasonId),
                P("@ReasonDetails", req.ReasonDetails),
                P("@HandoverNotes", req.HandoverNotes),
                P("@NoticePeriodDays", noticeDays),
                P("@RequestedLastWorkingDate", req.RequestedLastWorkingDate.Date),
                P("@ManagerEmployeeId", managerId),
                P("@HRManagerEmployeeId", hrManagerId));
            return list.First();
        }

        public Task WithdrawAsync(int employeeId, WithdrawRequest req)
            => ExecuteAsync("WITHDRAW", employeeId,
                P("@ResignationId", req.ResignationId),
                P("@Remarks", req.Reason));

        /* ------------------------------ approver ------------------------------ */

        public Task<List<ResignationDto>> GetTeamAsync(int employeeId, bool isHr)
            => QueryResignationsAsync("TEAM", employeeId, P("@IsHR", isHr));

        public async Task<ResignationDto?> GetDetailsAsync(int employeeId, int resignationId, bool isHr)
            => (await QueryResignationsAsync("DETAILS", employeeId,
                    P("@ResignationId", resignationId), P("@IsHR", isHr))).FirstOrDefault();

        public Task ApproveAsync(int employeeId, ApproveRequest req)
            => ExecuteAsync("APPROVE", employeeId,
                P("@ResignationId", req.ResignationId),
                P("@ApprovedLastWorkingDate", req.ApprovedLastWorkingDate.Date),
                P("@Remarks", req.Remarks));

        public Task RejectAsync(int employeeId, RejectRequest req)
            => ExecuteAsync("REJECT", employeeId,
                P("@ResignationId", req.ResignationId),
                P("@Remarks", req.Reason));

        public async Task<ResignationDto> UpdateClearanceAsync(int employeeId, bool isHr, UpdateClearanceRequest req)
        {
            var list = await QueryResignationsAsync("CLEARANCE_UPDATE", employeeId,
                P("@ResignationId", req.ResignationId),
                P("@ClearanceItemId", req.ClearanceItemId),
                P("@ClearanceStatus", req.Status.Trim().ToUpperInvariant()),
                P("@Remarks", req.Remarks),
                P("@IsHR", isHr));
            return list.First();
        }
    }
}
