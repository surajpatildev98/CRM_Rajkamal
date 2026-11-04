using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace CRMWebServices.Repository
{
    public class PayslipRepository : IPayslipRepository
    {
        private const string SP = "USP_ORG_Payslip";
        private readonly IDatabaseService _database;

        public PayslipRepository(IDatabaseService database)
        {
            _database = database;
        }

        // ---------- row helpers ----------
        private static bool Has(DataRow r, string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value;
        private static int RInt(DataRow r, string c) => Has(r, c) ? Convert.ToInt32(r[c]) : 0;
        private static decimal RDec(DataRow r, string c) => Has(r, c) ? Convert.ToDecimal(r[c]) : 0;
        private static string? RStr(DataRow r, string c) => Has(r, c) ? r[c].ToString() : null;
        private static DateTime? RNDate(DataRow r, string c) => Has(r, c) ? Convert.ToDateTime(r[c]) : null;

        private static PayslipModel MapPayslip(DataRow r) => new()
        {
            PayslipId = RInt(r, "PayslipId"),
            EmployeeId = RInt(r, "EmployeeId"),
            Month = RInt(r, "Month"),
            Year = RInt(r, "Year"),
            FinancialYear = RInt(r, "FinancialYear"),
            GrossEarnings = RDec(r, "GrossEarnings"),
            TotalDeductions = RDec(r, "TotalDeductions"),
            NetPay = RDec(r, "NetPay"),
            PaidOn = RNDate(r, "PaidOn"),
            PayslipStatus = RStr(r, "Status")
        };

        // THROW messages from the SP arrive as SqlException.Message
        private static string DbMsg(SqlException ex) => ex.Message;

        // ---------- employee: list ----------
        public async Task<PayslipListResponse> GetMyPayslips(int loggedInEmployeeId)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP,
                    new SqlParameter("@Status", "employee_payslip"),
                    new SqlParameter("@EmployeeId", loggedInEmployeeId)
                );

                var list = new List<PayslipModel>();
                foreach (DataRow row in dt.Rows) list.Add(MapPayslip(row));

                return new PayslipListResponse
                {
                    Status = true,
                    Message = "Payslips fetched successfully.",
                    Data = list
                };
            }
            catch (SqlException ex)
            {
                return new PayslipListResponse { Status = false, Message = DbMsg(ex) };
            }
            catch (Exception ex)
            {
                return new PayslipListResponse { Status = false, Message = $"Error: {ex.Message}" };
            }
        }

        // ---------- employee: one payslip + lines ----------
        public async Task<PayslipDetailResponse> GetPayslip(int loggedInEmployeeId, int payslipId)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync(SP,
                    new SqlParameter("@Status", "employee_payslip"),
                    new SqlParameter("@EmployeeId", loggedInEmployeeId),
                    new SqlParameter("@PayslipId", payslipId)
                );

                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                    return new PayslipDetailResponse { Status = false, Message = "Payslip not found." };

                var payslip = MapPayslip(ds.Tables[0].Rows[0]);

                if (ds.Tables.Count > 1)
                {
                    foreach (DataRow l in ds.Tables[1].Rows)
                    {
                        var line = new PayslipLineModel
                        {
                            PayslipLineId = RInt(l, "PayslipLineId"),
                            LineType = RStr(l, "LineType") ?? string.Empty,
                            Name = RStr(l, "Name") ?? string.Empty,
                            Amount = RDec(l, "Amount")
                        };

                        if (line.LineType == "E") payslip.Earnings.Add(line);
                        else if (line.LineType == "D") payslip.Deductions.Add(line);
                    }
                }

                return new PayslipDetailResponse
                {
                    Status = true,
                    Message = "Payslip fetched successfully.",
                    Data = payslip
                };
            }
            catch (SqlException ex)
            {
                return new PayslipDetailResponse { Status = false, Message = DbMsg(ex) };
            }
            catch (Exception ex)
            {
                return new PayslipDetailResponse { Status = false, Message = $"Error: {ex.Message}" };
            }
        }

        // ---------- HR: month list (drafts + published) ----------
        public async Task<PayslipListResponse> GetHrPayslips(PayslipMonthRequest request)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP,
                    new SqlParameter("@Status", "hr_payslip"),
                    new SqlParameter("@PayMonth", request.PayMonth),
                    new SqlParameter("@PayYear", request.PayYear)
                );

                var list = new List<PayslipModel>();
                foreach (DataRow row in dt.Rows) list.Add(MapPayslip(row));

                return new PayslipListResponse
                {
                    Status = true,
                    Message = "Payslips fetched successfully.",
                    Data = list
                };
            }
            catch (SqlException ex)
            {
                return new PayslipListResponse { Status = false, Message = DbMsg(ex) };
            }
            catch (Exception ex)
            {
                return new PayslipListResponse { Status = false, Message = $"Error: {ex.Message}" };
            }
        }

        // ---------- HR: save draft ----------
        public async Task<PayslipActionResponse> SavePayslip(SavePayslipRequest request)
        {
            try
            {
                // keys must match what the SP reads: lineType / name / amount / sortOrder
                var linesJson = JsonSerializer.Serialize(
                    request.Lines.Select(l => new
                    {
                        lineType = l.LineType,
                        name = l.Name,
                        amount = l.Amount,
                        sortOrder = l.SortOrder
                    }));

                var dt = await _database.ExecuteDataTableAsync(SP,
                    new SqlParameter("@Status", "Draft"),
                    new SqlParameter("@EmployeeId", request.EmployeeId),
                    new SqlParameter("@PayMonth", request.PayMonth),
                    new SqlParameter("@PayYear", request.PayYear),
                    new SqlParameter("@LinesJson", linesJson),
                    new SqlParameter("@PaidOn", request.PaidOn.HasValue ? request.PaidOn.Value.Date : (object)DBNull.Value)
                );

                int? id = dt.Rows.Count > 0 && Has(dt.Rows[0], "PayslipId") ? RInt(dt.Rows[0], "PayslipId") : null;

                return new PayslipActionResponse
                {
                    Status = true,
                    Message = "Payslip saved as draft.",
                    PayslipId = id
                };
            }
            catch (SqlException ex)
            {
                return new PayslipActionResponse { Status = false, Message = DbMsg(ex) };
            }
            catch (Exception ex)
            {
                return new PayslipActionResponse { Status = false, Message = $"Error: {ex.Message}" };
            }
        }

        // ---------- HR: publish a month ----------
        public async Task<PayslipActionResponse> PublishPayslips(PayslipMonthRequest request)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP,
                    new SqlParameter("@Status", "Published"),
                    new SqlParameter("@PayMonth", request.PayMonth),
                    new SqlParameter("@PayYear", request.PayYear),
                    new SqlParameter("@PaidOn", request.PaidOn.HasValue ? request.PaidOn.Value.Date : (object)DBNull.Value)
                );

                int count = dt.Rows.Count > 0 ? RInt(dt.Rows[0], "PublishedCount") : 0;

                return new PayslipActionResponse
                {
                    Status = count > 0,
                    Message = count > 0 ? $"{count} payslip(s) published." : "No draft payslips found for this month.",
                    Count = count
                };
            }
            catch (SqlException ex)
            {
                return new PayslipActionResponse { Status = false, Message = DbMsg(ex) };
            }
            catch (Exception ex)
            {
                return new PayslipActionResponse { Status = false, Message = $"Error: {ex.Message}" };
            }
        }

        // ---------- HR: unpublish ----------
        public async Task<PayslipActionResponse> UnpublishPayslip(int payslipId)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP,
                    new SqlParameter("@Status", "Draft"),
                    new SqlParameter("@PayslipId", payslipId)      // no @LinesJson -> unpublish
                );

                int count = dt.Rows.Count > 0 ? RInt(dt.Rows[0], "UpdatedCount") : 0;

                return new PayslipActionResponse
                {
                    Status = count > 0,
                    Message = count > 0 ? "Payslip moved back to draft." : "Published payslip not found.",
                    PayslipId = payslipId,
                    Count = count
                };
            }
            catch (SqlException ex)
            {
                return new PayslipActionResponse { Status = false, Message = DbMsg(ex) };
            }
            catch (Exception ex)
            {
                return new PayslipActionResponse { Status = false, Message = $"Error: {ex.Message}" };
            }
        }

        // ---------- HR: delete draft ----------
        public async Task<PayslipActionResponse> DeletePayslip(int payslipId)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP,
                    new SqlParameter("@Status", "Deleted"),
                    new SqlParameter("@PayslipId", payslipId)
                );

                int count = dt.Rows.Count > 0 ? RInt(dt.Rows[0], "DeletedCount") : 0;

                return new PayslipActionResponse
                {
                    Status = count > 0,
                    Message = count > 0 ? "Draft payslip deleted." : "Draft payslip not found (published payslips can't be deleted).",
                    PayslipId = payslipId,
                    Count = count
                };
            }
            catch (SqlException ex)
            {
                return new PayslipActionResponse { Status = false, Message = DbMsg(ex) };
            }
            catch (Exception ex)
            {
                return new PayslipActionResponse { Status = false, Message = $"Error: {ex.Message}" };
            }
        }
    }
}
