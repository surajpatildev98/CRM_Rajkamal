using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Services;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using static CRMWebServices.Models.AmcModels;

namespace CRMWebServices.Repository
{
    /// <summary>
    /// AMC module → USP_AMC_ContractManagement (switched on @Status).
    /// Permission codes (AMC_View / Create / Edit / Renew / Cancel / Coverage) are checked in AmcController.
    /// </summary>
    public class AmcRepository : IAmcRepository
    {
        private const string SP_NAME = "USP_AMC_ContractManagement";
        private static readonly string[] Filters = { "ALL", "ACTIVE", "EXPIRING", "EXPIRED", "RENEWED", "CANCELLED" };

        private readonly IDatabaseService _database;
        private readonly ILeadRepository _leadRepository;
        private readonly ILogger<AmcRepository> _logger;

        public AmcRepository(IDatabaseService database, ILeadRepository leadRepository, ILogger<AmcRepository> logger)
        {
            _database = database;
            _leadRepository = leadRepository;
            _logger = logger;
        }

        public async Task<ApiResponse> GetMasters()
        {
            try
            {
                var ds = await Exec("MASTERS", 0);
                return Ok("AMC masters.", new
                {
                    Durations = TicketSupportRepository.Rows(ds.Tables[0]),
                    CoverageServices = TicketSupportRepository.Rows(ds.Tables[1])
                });
            }
            catch (Exception ex) { return Error(ex, "MASTERS"); }
        }

        public Task<ApiResponse> GetList(string? filter, string? amcType, string? search) =>
            Query("GET_LIST", false,
                P("@Filter", Filters.Contains(filter?.Trim().ToUpperInvariant()) ? filter!.Trim().ToUpperInvariant() : "ALL"),
                P("@AmcTypeFilter", NormType(amcType)),
                P("@Search", string.IsNullOrWhiteSpace(search) ? null : search.Trim()));

        public Task<ApiResponse> GetCounts(string? amcType, string? search) =>
            Query("COUNTS", true,
                P("@AmcTypeFilter", NormType(amcType)),
                P("@Search", string.IsNullOrWhiteSpace(search) ? null : search.Trim()));

        public async Task<ApiResponse> GetDetails(int amcContractId)
        {
            try
            {
                var ds = await Exec("DETAILS", 0, P("@AmcContractId", amcContractId));
                var contract = TicketSupportRepository.Rows(ds.Tables[0])[0];
                contract["invoiceFileUrl"] = TicketSupportRepository.FileUrl(contract.GetValueOrDefault("invoiceFilePath") as string);
                return Ok("AMC details.", new
                {
                    Contract = contract,
                    Items = TicketSupportRepository.Rows(ds.Tables[1]),
                    CoverageServices = TicketSupportRepository.Rows(ds.Tables[2]),
                    Tickets = TicketSupportRepository.Rows(ds.Tables[3]),
                    History = TicketSupportRepository.Rows(ds.Tables[4])
                });
            }
            catch (Exception ex) { return Error(ex, "DETAILS"); }
        }

        public async Task<ApiResponse> Save(AmcSaveRequest model, string status, int employeeId)
        {
            if (model == null) return Fail("Request is required.");
            if (status != "CREATE" && model.AmcContractId <= 0) return Fail("AMC is required.");

            var source = (model.CustomerSource ?? "").Trim().ToUpperInvariant();
            int? customerId = null;
            // Renew keeps the old customer (taken in the SP), so no customer is needed in the request.
            if (status != "RENEW")
            {
                if ((source != "CRM" && source != "WMS") || model.SourceCustomerId <= 0) return Fail("Please select a customer.");
                if (source == "WMS")
                {
                    // Same as Ticket: WMS customer is mapped into CUS_Customer on first use.
                    var map = await _leadRepository.EnsureWmsCustomer(model.SourceCustomerId, null, employeeId);
                    if (!map.Status) return Fail(map.Message);
                    customerId = (int?)map.Data?.GetType().GetProperty("CustomerId")?.GetValue(map.Data);
                }
            }

            string? invoicePath = null;
            if (model.InvoiceFile != null && !string.IsNullOrWhiteSpace(model.InvoiceFile.Base64Content))
            {
                if (!SavePdf(model.InvoiceFile, out invoicePath, out var fileError)) return Fail(fileError);
            }

            return await Mutate(status, employeeId,
                P("@AmcContractId", status == "CREATE" ? null : model.AmcContractId),
                P("@AmcType", model.AmcType),
                P("@CoverageType", model.CoverageType),
                P("@CustomerSource", source),
                P("@SourceCustomerId", model.SourceCustomerId),
                P("@Ref_CustomerId", customerId),
                P("@CustomerName", model.CustomerName?.Trim()),
                P("@ContactMobile", model.ContactMobile?.Trim()),
                P("@ContactEmail", model.ContactEmail?.Trim()),
                P("@DurationMonths", model.DurationMonths),
                P("@StartDate", model.StartDate?.Date),
                P("@TotalVisits", model.TotalVisits),
                P("@InvoiceNo", model.InvoiceNo?.Trim()),
                P("@InvoiceDate", model.InvoiceDate?.Date),
                P("@InvoiceFilePath", invoicePath),
                P("@InvoiceFileName", invoicePath == null ? null : model.InvoiceFile!.FileName),
                P("@Amount", model.Amount),
                P("@GstPercent", model.GstPercent),
                P("@PaymentStatus", model.PaymentStatus),
                P("@PaymentMode", model.PaymentMode?.Trim()),
                P("@PaymentDate", model.PaymentDate?.Date),
                P("@PaymentRemark", model.PaymentRemark?.Trim()),
                P("@Remarks", model.Remarks?.Trim()),
                P("@CoverageIds", model.CoverageServiceIds == null ? null : string.Join(",", model.CoverageServiceIds.Distinct())),
                P("@ItemsJson", model.Items == null || model.Items.Count == 0 ? null : JsonConvert.SerializeObject(model.Items.Select(i => new
                {
                    categoryId = i.CategoryId, categoryName = i.CategoryName, offeringId = i.OfferingId, offeringName = i.OfferingName,
                    productId = i.ProductId, productName = i.ProductName, serialNo = i.SerialNo, licenseKey = i.LicenseKey,
                    softwareVersion = i.SoftwareVersion, noOfUsers = i.NoOfUsers, remark = i.Remark
                }))));
        }

        public async Task<ApiResponse> AttachInvoice(AmcInvoiceRequest model, int employeeId)
        {
            if (model == null || model.AmcContractId <= 0) return Fail("AMC is required.");
            if (model.InvoiceFile == null || string.IsNullOrWhiteSpace(model.InvoiceFile.Base64Content)) return Fail("Please attach the invoice PDF.");
            if (!SavePdf(model.InvoiceFile, out var path, out var error)) return Fail(error);

            return await Mutate("ATTACH_INVOICE", employeeId,
                P("@AmcContractId", model.AmcContractId),
                P("@InvoiceNo", model.InvoiceNo?.Trim()),
                P("@InvoiceDate", model.InvoiceDate?.Date),
                P("@InvoiceFilePath", path),
                P("@InvoiceFileName", model.InvoiceFile.FileName));
        }

        public Task<ApiResponse> Cancel(AmcCancelRequest model, int employeeId) =>
            model == null || model.AmcContractId <= 0
                ? Task.FromResult(Fail("AMC is required."))
                : Mutate("CANCEL", employeeId, P("@AmcContractId", model.AmcContractId), P("@Reason", model.Reason?.Trim()));

        /// <summary>
        /// Rows with the same Customer + Invoice No → one AMC (header taken from the first row).
        /// Rows without Invoice No → one AMC each. Every group is saved through CREATE, so it gets the same validation;
        /// a failed group is reported with its row numbers and the other groups are still saved.
        /// </summary>
        public async Task<ApiResponse> Upload(AmcUploadRequest model, int employeeId)
        {
            var rows = model?.Rows ?? new List<AmcUploadRow>();
            if (rows.Count == 0) return Fail("No rows to upload.");
            if (rows.Count > 1000) return Fail("Upload up to 1000 rows at a time.");

            var groups = rows.GroupBy(r => string.IsNullOrWhiteSpace(r.InvoiceNo)
                ? $"ROW|{r.RowNo}"
                : $"{(r.CustomerSource ?? "").Trim().ToUpperInvariant()}|{r.SourceCustomerId}|{r.InvoiceNo!.Trim().ToUpperInvariant()}");

            var created = new List<object>();
            var errors = new List<object>();
            foreach (var g in groups)
            {
                var h = g.First();
                var result = await Save(new AmcSaveRequest
                {
                    AmcType = h.AmcType, CoverageType = h.CoverageType, CustomerSource = h.CustomerSource,
                    SourceCustomerId = h.SourceCustomerId, CustomerName = h.CustomerName, ContactMobile = h.ContactMobile,
                    ContactEmail = h.ContactEmail, DurationMonths = h.DurationMonths, StartDate = h.StartDate,
                    TotalVisits = h.TotalVisits, InvoiceNo = h.InvoiceNo, InvoiceDate = h.InvoiceDate, Amount = h.Amount,
                    GstPercent = h.GstPercent, PaymentStatus = h.PaymentStatus, PaymentMode = h.PaymentMode,
                    PaymentDate = h.PaymentDate, Remarks = h.Remarks,
                    CoverageServiceIds = (h.CoverageServiceIds ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(x => int.TryParse(x, out var id) ? id : -1).ToList(),
                    Items = g.Select(r => (AmcItem)r).ToList()
                }, "CREATE", employeeId);

                var rowNos = g.Select(r => r.RowNo).ToList();
                if (result.Status && result.Data is Dictionary<string, object?> d)
                    created.Add(new { Rows = rowNos, AmcNo = d.GetValueOrDefault("amcNo"), AmcContractId = d.GetValueOrDefault("amcContractId") });
                else
                    errors.Add(new { Rows = rowNos, h.CustomerName, h.InvoiceNo, Error = result.Message });
            }

            return new ApiResponse
            {
                Status = created.Count > 0,
                Message = $"{created.Count} AMC created, {errors.Count} failed.",
                Data = new { Created = created, Errors = errors }
            };
        }

        public Task<ApiResponse> CheckCoverage(string? customerSource, int sourceCustomerId, string? serialNo, long? productId) =>
            string.IsNullOrWhiteSpace(serialNo) && productId == null
                ? Task.FromResult(Fail("Serial No. or product is required."))
                : Query("COVERAGE_CHECK", true,
                    P("@CustomerSource", customerSource?.Trim().ToUpperInvariant()),
                    P("@SourceCustomerId", sourceCustomerId),
                    P("@SerialNo", serialNo?.Trim()),
                    P("@ProductId", productId));

        /// <summary>Best-effort: a failed link must never fail ticket creation.</summary>
        public async Task LinkTicket(int ticketId)
        {
            try { await Exec("LINK_TICKET", 0, P("@TicketId", ticketId)); }
            catch (Exception ex) { _logger.LogError(ex, "AMC link failed for ticket {TicketId}", ticketId); }
        }

        public Task<ApiResponse> GetCoverageServices() => Query("COVERAGE_LIST", false);

        public Task<ApiResponse> SaveCoverageService(AmcCoverageSaveRequest model, int employeeId) =>
            model == null
                ? Task.FromResult(Fail("Request is required."))
                : Mutate("COVERAGE_SAVE", employeeId,
                    P("@CoverageServiceId", model.CoverageServiceId),
                    P("@ServiceName", model.ServiceName),
                    P("@AmcType", model.AmcType),
                    P("@IsActive", model.IsActive));

        #region Helpers
        private static bool SavePdf(AmcFile file, out string? path, out string error)
        {
            path = null;
            error = "";
            if (!string.Equals(Path.GetExtension(file.FileName ?? ""), ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                error = "Invoice must be a PDF file.";
                return false;
            }
            try
            {
                path = TicketSupportRepository.SaveBase64(file.Base64Content, file.FileName!, "AmcInvoices");
                return true;
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }

        private static string? NormType(string? amcType)
        {
            var t = amcType?.Trim().ToUpperInvariant();
            return t is "HARDWARE" or "SOFTWARE" ? t : null;
        }

        private Task<DataSet> Exec(string status, int employeeId, params SqlParameter[] extra)
        {
            var all = new List<SqlParameter> { new("@Status", status), new("@EmployeeId", employeeId) };
            all.AddRange(extra);
            return _database.ExecuteDataSetAsync(SP_NAME, all.ToArray());
        }

        private async Task<ApiResponse> Query(string status, bool single, params SqlParameter[] extra)
        {
            try
            {
                var ds = await Exec(status, 0, extra);
                var rows = ds.Tables.Count > 0 ? TicketSupportRepository.Rows(ds.Tables[0]) : new List<Dictionary<string, object?>>();
                return Ok(rows.Count > 0 ? "Records found." : "No records found.", single ? (rows.Count > 0 ? rows[0] : null) : rows);
            }
            catch (Exception ex) { return Error(ex, status); }
        }

        /// <summary>SP returns a Status / Message row; validation errors come as THROW messages.</summary>
        private async Task<ApiResponse> Mutate(string status, int employeeId, params SqlParameter[] extra)
        {
            try
            {
                var ds = await Exec(status, employeeId, extra);
                if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0) return Fail("No response from database.");
                var row = TicketSupportRepository.Rows(ds.Tables[0])[0];
                var ok = Convert.ToInt32(row.GetValueOrDefault("status") ?? 0) == 1;
                var message = row.GetValueOrDefault("message")?.ToString() ?? (ok ? "Saved." : "Failed.");
                row.Remove("status");
                row.Remove("message");
                return new ApiResponse { Status = ok, Message = message, Data = row };
            }
            catch (Exception ex) { return Error(ex, status); }
        }

        private ApiResponse Error(Exception ex, string status)
        {
            if (ex is not SqlException) _logger.LogError(ex, "AMC {Status} failed", status);
            return Fail(ex.Message);
        }

        private static SqlParameter P(string name, object? value) => new(name, value ?? DBNull.Value);
        private static ApiResponse Ok(string message, object? data) => new() { Status = true, Message = message, Data = data! };
        private static ApiResponse Fail(string message) => new() { Status = false, Message = message };
        #endregion
    }
}
