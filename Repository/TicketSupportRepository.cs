using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Services;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using static CRMWebServices.Models.TicketSupportModels;

namespace CRMWebServices.Repository
{
    /// <summary>
    /// Ticket Support module → USP_TKT_TicketManagement (switched on @Status).
    /// Access rules (raiser / assignee / assignee's Support Manager) are enforced in the SP.
    /// </summary>
    public class TicketSupportRepository : ITicketSupportRepository
    {
        private const string SP_NAME = "USP_TKT_TicketManagement";
        private const string ENTITY_TYPE = "TICKET";

        // Same rules as LeadRepository.SaveBase64Attachment.
        private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png" };
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        private static readonly string[] TravelModes = { "BUS", "TRAIN", "AUTO", "PERSONAL_BIKE", "PERSONAL_CAR", "STAY", "OTHER" };

        private readonly IDatabaseService _database;
        private readonly ILeadRepository _leadRepository;
        private readonly ISchedulerExecutionService _scheduler;
        private readonly ILogger<TicketSupportRepository> _logger;

        public TicketSupportRepository(
            IDatabaseService database,
            ILeadRepository leadRepository,
            ISchedulerExecutionService scheduler,
            ILogger<TicketSupportRepository> logger)
        {
            _database = database;
            _leadRepository = leadRepository;
            _scheduler = scheduler;
            _logger = logger;
        }

        #region Create
        public async Task<ApiResponse> CreateTicket(CreateSupportTicketRequest model, int employeeId, bool isManager)
        {
            if (model == null) return Fail("Request is required.");
            var source = (model.CustomerSource ?? "").Trim().ToUpperInvariant();
            if ((source != "CRM" && source != "WMS") || model.SourceCustomerId <= 0)
                return Fail("Please select a company.");
            if (!ValidTravel(model.Travel, out var travelError)) return Fail(travelError);

            // WMS customer: map into the CRM customer master (CUS_Customer) on first use.
            int? customerId = null;
            if (source == "WMS")
            {
                var map = await _leadRepository.EnsureWmsCustomer(model.SourceCustomerId, null, employeeId);
                if (!map.Status) return Fail(map.Message);
                customerId = (int?)map.Data?.GetType().GetProperty("CustomerId")?.GetValue(map.Data);
            }

            var response = await Mutate("CREATE_TICKET", employeeId, isManager,
                P("@CustomerSource", source),
                P("@SourceCustomerId", model.SourceCustomerId),
                P("@Ref_CustomerId", customerId),
                P("@CustomerName", model.CustomerName?.Trim()),
                P("@Issue", model.Issue?.Trim()),
                P("@Priority", model.Priority?.Trim().ToUpperInvariant()),
                P("@ServiceMode", model.ServiceMode?.Trim().ToUpperInvariant()),
                P("@ExpectedDate", model.ExpectedDate.Date),
                P("@AssignedTo", isManager ? model.AssignedTo : null),
                P("@ExpensesJson", ToExpenseJson(model.Travel, null)));

            if (response.Status && response.Data is Dictionary<string, object?> row)
            {
                var ticketId = Int(row, "ticketId");
                var ticketNo = Str(row, "ticketNo");
                var assignedTo = Int(row, "assignedTo");
                if (assignedTo != employeeId)
                    await Notify(assignedTo, ticketId, "New Ticket Assigned",
                        $"Ticket {ticketNo} ({model.CustomerName}) has been assigned to you.", "ASSIGNEE");
                if (Str(row, "approvalStatus") == "PENDING")
                    await Notify(Int(row, "approverId"), ticketId, "Ticket Approval Pending",
                        $"Visit ticket {ticketNo} ({model.CustomerName}) needs your approval.", "MANAGER");
            }
            return response;
        }
        #endregion

        #region Lists / details
        public Task<ApiResponse> GetTickets(string listType, int employeeId, bool isManager) =>
            Query("GET_TICKETS", employeeId, isManager, false, P("@ListType", NormalizeList(listType)));

        public Task<ApiResponse> GetMyCounts(string listType, int employeeId, bool isManager) =>
            Query("MY_COUNTS", employeeId, isManager, true, P("@ListType", NormalizeList(listType)));

        public async Task<ApiResponse> GetTicketDetails(int ticketId, int employeeId, bool isManager)
        {
            try
            {
                var ds = await Exec("TICKET_DETAILS", employeeId, isManager, P("@TicketId", ticketId));
                if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0) return Fail("Ticket not found.");

                var expenses = Rows(ds.Tables[2]);
                foreach (var x in expenses) x["receiptUrl"] = FileUrl(x.GetValueOrDefault("receiptPath") as string);
                var attachments = Rows(ds.Tables[3]);
                foreach (var a in attachments) a["fileUrl"] = FileUrl(a.GetValueOrDefault("filePath") as string);

                return Ok("Ticket details.", new
                {
                    Ticket = Rows(ds.Tables[0])[0],
                    Activities = Rows(ds.Tables[1]),
                    Expenses = expenses,
                    Attachments = attachments,
                    StatusLog = Rows(ds.Tables[4])
                });
            }
            catch (SqlException ex) { return Fail(ex.Message); }
            catch (Exception ex) { _logger.LogError(ex, "Ticket details failed"); return Fail(ex.Message); }
        }
        #endregion

        #region Approval flow
        public async Task<ApiResponse> SendForApproval(TicketTravelRequest model, int employeeId, bool isManager)
        {
            if (model == null || model.Travel == null || model.Travel.Count == 0) return Fail("Please select a travel mode.");
            if (!ValidTravel(model.Travel, out var error)) return Fail(error);

            var response = await Mutate("SEND_FOR_APPROVAL", employeeId, isManager,
                P("@TicketId", model.TicketId),
                P("@ExpensesJson", ToExpenseJson(model.Travel, null)));

            // Notify the Support Manager every time something is waiting for him.
            if (response.Status && response.Data is Dictionary<string, object?> row && Int(row, "hasPending") == 1)
                await Notify(Int(row, "approverId"), model.TicketId, "Visit Approval Pending",
                    $"Ticket {Str(row, "ticketNo")} ({Str(row, "customerName")}): Visit travel request needs your approval.", "MANAGER");
            return response;
        }

        public async Task<ApiResponse> Decide(TicketDecisionRequest model, int employeeId, bool isManager)
        {
            if (model == null) return Fail("Request is required.");
            var decision = (model.Decision ?? "").Trim().ToUpperInvariant();
            if (decision != "APPROVE" && decision != "REJECT") return Fail("Decision must be APPROVE or REJECT.");

            var response = model.TicketExpenseId.HasValue
                ? await Mutate("APPROVE_EXPENSE", employeeId, isManager,
                    P("@TicketId", model.TicketId), P("@TicketExpenseId", model.TicketExpenseId),
                    P("@Decision", decision), P("@Remarks", model.Remarks?.Trim()))
                : await Mutate("APPROVE_TICKET", employeeId, isManager,
                    P("@TicketId", model.TicketId), P("@Decision", decision), P("@Remarks", model.Remarks?.Trim()));

            if (response.Status && response.Data is Dictionary<string, object?> row)
                await Notify(Int(row, "assignedTo"), model.TicketId,
                    decision == "APPROVE" ? "Request Approved" : "Request Rejected",
                    $"Ticket {Str(row, "ticketNo")}: {response.Message}", "ASSIGNEE");
            return response;
        }
        #endregion

        #region Work on ticket
        public Task<ApiResponse> CheckIn(TicketCheckInRequest model, int employeeId, bool isManager) =>
            model == null ? Task.FromResult(Fail("Request is required.")) :
            Mutate("CHECK_IN", employeeId, isManager,
                P("@TicketId", model.TicketId), P("@Latitude", model.Latitude),
                P("@Longitude", model.Longitude), P("@Address", model.Address?.Trim()));

        public Task<ApiResponse> AddActivity(TicketActivityRequest model, int employeeId, bool isManager) =>
            model == null ? Task.FromResult(Fail("Request is required.")) :
            Mutate("ADD_ACTIVITY", employeeId, isManager,
                P("@TicketId", model.TicketId),
                P("@ActivityType", model.ActivityType?.Trim().ToUpperInvariant()),
                P("@Title", model.Title?.Trim()),
                P("@ActivityOn", model.ActivityOn),
                P("@Address", model.Location?.Trim()),
                P("@Latitude", model.Latitude),
                P("@Longitude", model.Longitude),
                P("@ContactPerson", model.ContactPerson?.Trim()),
                P("@Remarks", model.Remarks?.Trim()),
                P("@ActivityStatus", model.ActivityStatus?.Trim().ToUpperInvariant()));

        public async Task<ApiResponse> AddExpenses(TicketExpenseRequest model, int employeeId, bool isManager)
        {
            if (model == null || model.Expenses == null || model.Expenses.Count == 0) return Fail("Expense details are required.");
            if (!ValidTravel(model.Expenses, out var error)) return Fail(error);

            string json;
            try { json = ToExpenseJson(model.Expenses, "TicketExpenseReceipts")!; }
            catch (Exception ex) { return Fail(ex.Message); }

            var response = await Mutate("ADD_EXPENSE", employeeId, isManager,
                P("@TicketId", model.TicketId), P("@ExpensesJson", json));

            if (response.Status && response.Data is Dictionary<string, object?> row && Int(row, "hasPending") == 1)
                await Notify(Int(row, "approverId"), model.TicketId, "Expense Approval Pending",
                    $"An expense on ticket {Str(row, "ticketNo")} needs your approval.", "MANAGER");
            return response;
        }

        public async Task<ApiResponse> CompleteTicket(TicketCompleteRequest model, int employeeId, bool isManager)
        {
            if (model == null) return Fail("Request is required.");
            if (string.IsNullOrWhiteSpace(model.ResolutionRemarks)) return Fail("Resolution remarks are required.");

            string? files;
            try { files = SaveFiles(model.Attachments); }
            catch (Exception ex) { return Fail(ex.Message); }

            var response = await Mutate("COMPLETE_TICKET", employeeId, isManager,
                P("@TicketId", model.TicketId),
                P("@Remarks", model.ResolutionRemarks.Trim()),
                P("@AttachmentsJson", files));

            if (response.Status && response.Data is Dictionary<string, object?> row)
            {
                var msg = $"Ticket {Str(row, "ticketNo")} has been completed.";
                var raisedBy = Int(row, "raisedBy");
                var approver = Int(row, "approverId");
                if (raisedBy != employeeId) await Notify(raisedBy, model.TicketId, "Ticket Completed", msg, "RAISER");
                if (approver != employeeId && approver != raisedBy) await Notify(approver, model.TicketId, "Ticket Completed", msg, "MANAGER");
            }
            return response;
        }

        public async Task<ApiResponse> AddAttachments(TicketAttachmentRequest model, int employeeId, bool isManager)
        {
            if (model == null || model.Attachments == null || model.Attachments.Count == 0) return Fail("Please select a file.");
            string? files;
            try { files = SaveFiles(model.Attachments); }
            catch (Exception ex) { return Fail(ex.Message); }

            return await Mutate("ADD_ATTACHMENTS", employeeId, isManager,
                P("@TicketId", model.TicketId), P("@AttachmentsJson", files));
        }

        public Task<ApiResponse> SetServiceMode(TicketServiceModeRequest model, int employeeId, bool isManager) =>
            model == null ? Task.FromResult(Fail("Request is required.")) :
            Mutate("SET_SERVICE_MODE", employeeId, isManager,
                P("@TicketId", model.TicketId),
                P("@ServiceMode", model.ServiceMode?.Trim().ToUpperInvariant()));

        public Task<ApiResponse> CancelTicket(TicketCancelRequest model, int employeeId, bool isManager) =>
            model == null ? Task.FromResult(Fail("Request is required.")) :
            Mutate("CANCEL_TICKET", employeeId, isManager, P("@TicketId", model.TicketId), P("@Remarks", model.Remarks?.Trim()));
        #endregion

        #region Manager
        public async Task<ApiResponse> GetManagerDashboard(int employeeId, bool isManager)
        {
            try
            {
                var ds = await Exec("MANAGER_DASHBOARD", employeeId, isManager);
                return Ok("Ticket dashboard.", new
                {
                    Counts = ds.Tables[0].Rows.Count > 0 ? Rows(ds.Tables[0])[0] : null,
                    Team = Rows(ds.Tables[1])
                });
            }
            catch (SqlException ex) { return Fail(ex.Message); }
            catch (Exception ex) { _logger.LogError(ex, "Ticket dashboard failed"); return Fail(ex.Message); }
        }

        public Task<ApiResponse> GetTeam(int employeeId, bool isManager) =>
            Query("TEAM_LIST", employeeId, isManager, false);

        public async Task<ApiResponse> GetPendingApprovals(int employeeId, bool isManager)
        {
            try
            {
                var ds = await Exec("PENDING_APPROVALS", employeeId, isManager);
                var tickets = Rows(ds.Tables[0]);
                var expenses = Rows(ds.Tables[1]);
                foreach (var t in tickets)
                {
                    var id = Int(t, "ticketId");
                    t["pendingExpenses"] = expenses.Where(x => Int(x, "ref_TicketId") == id).ToList();
                }
                return Ok("Pending approvals.", tickets);
            }
            catch (SqlException ex) { return Fail(ex.Message); }
            catch (Exception ex) { _logger.LogError(ex, "Ticket approvals failed"); return Fail(ex.Message); }
        }
        #endregion

        #region Helpers
        private Task<DataSet> Exec(string status, int employeeId, bool isManager, params SqlParameter[] extra)
        {
            var all = new List<SqlParameter>
            {
                new SqlParameter("@Status", status),
                new SqlParameter("@EmployeeId", employeeId),
                new SqlParameter("@IsManager", isManager)
            };
            all.AddRange(extra);
            return _database.ExecuteDataSetAsync(SP_NAME, all.ToArray());
        }

        private async Task<ApiResponse> Query(string status, int employeeId, bool isManager, bool single, params SqlParameter[] extra)
        {
            try
            {
                var ds = await Exec(status, employeeId, isManager, extra);
                var rows = ds.Tables.Count > 0 ? Rows(ds.Tables[0]) : new List<Dictionary<string, object?>>();
                return Ok(rows.Count > 0 ? "Records found." : "No records found.",
                    single ? (rows.Count > 0 ? rows[0] : null) : rows);
            }
            catch (SqlException ex) { return Fail(ex.Message); }
            catch (Exception ex) { _logger.LogError(ex, "Ticket {Status} failed", status); return Fail(ex.Message); }
        }

        /// <summary>Mutation: SP returns Status / Message row; errors come as THROW messages.</summary>
        private async Task<ApiResponse> Mutate(string status, int employeeId, bool isManager, params SqlParameter[] extra)
        {
            try
            {
                var ds = await Exec(status, employeeId, isManager, extra);
                if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0) return Fail("No response from database.");
                var row = Rows(ds.Tables[0])[0];
                var ok = Convert.ToInt32(row.GetValueOrDefault("status") ?? 0) == 1;
                var message = row.GetValueOrDefault("message")?.ToString() ?? (ok ? "Saved." : "Failed.");
                row.Remove("status");
                row.Remove("message");
                return new ApiResponse { Status = ok, Message = message, Data = row };
            }
            catch (SqlException ex) { return Fail(ex.Message); }
            catch (Exception ex) { _logger.LogError(ex, "Ticket {Status} failed", status); return Fail(ex.Message); }
        }

        /// <summary>Rows as dictionaries with camelCase keys and DBNull → null.</summary>
        private static List<Dictionary<string, object?>> Rows(DataTable table)
        {
            var list = new List<Dictionary<string, object?>>();
            foreach (DataRow dr in table.Rows)
            {
                var row = new Dictionary<string, object?>();
                foreach (DataColumn col in table.Columns)
                {
                    var name = col.ColumnName;
                    var key = char.ToLowerInvariant(name[0]) + name.Substring(1);
                    row[key] = dr[col] == DBNull.Value ? null : dr[col];
                }
                list.Add(row);
            }
            return list;
        }

        private static SqlParameter P(string name, object? value) => new SqlParameter(name, value ?? DBNull.Value);

        private static int Int(Dictionary<string, object?> row, string key) =>
            row.TryGetValue(key, out var v) && v != null && int.TryParse(v.ToString(), out var i) ? i : 0;

        private static string? Str(Dictionary<string, object?> row, string key) =>
            row.TryGetValue(key, out var v) ? v?.ToString() : null;

        private static ApiResponse Ok(string message, object? data) => new ApiResponse { Status = true, Message = message, Data = data! };
        private static ApiResponse Fail(string message) => new ApiResponse { Status = false, Message = message };

        private static string NormalizeList(string? listType)
        {
            var t = (listType ?? "ASSIGNED").Trim().ToUpperInvariant();
            return t is "ASSIGNED" or "RAISED" or "TEAM_OPEN" or "TEAM_CLOSED" ? t : "ASSIGNED";
        }

        private static bool ValidTravel(List<TicketExpenseItem>? items, out string error)
        {
            error = string.Empty;
            if (items == null) return true;
            foreach (var x in items)
            {
                var mode = (x.TravelMode ?? "").Trim().ToUpperInvariant();
                if (!TravelModes.Contains(mode)) { error = "Invalid travel mode."; return false; }
                if (x.Amount < 0) { error = "Amount cannot be negative."; return false; }
                x.TravelMode = mode;
            }
            return true;
        }

        /// <summary>Expense list → JSON for the SP; receipts are saved when <paramref name="receiptFolder"/> is set.</summary>
        private string? ToExpenseJson(List<TicketExpenseItem>? items, string? receiptFolder)
        {
            if (items == null || items.Count == 0) return null;
            var rows = items.Select(x =>
            {
                string? path = null;
                if (receiptFolder != null && x.Receipt != null && !string.IsNullOrWhiteSpace(x.Receipt.Base64Content))
                    path = SaveBase64(x.Receipt.Base64Content, x.Receipt.FileName, receiptFolder);
                return new
                {
                    ticketActivityId = x.TicketActivityId,
                    category = string.IsNullOrWhiteSpace(x.Category) ? null : x.Category.Trim().ToUpperInvariant(),
                    travelMode = x.TravelMode,
                    fromLocation = x.FromLocation?.Trim(),
                    toLocation = x.ToLocation?.Trim(),
                    km = x.Km,
                    amount = x.Amount,
                    remarks = x.Remarks?.Trim(),
                    receiptFileName = path == null ? null : x.Receipt!.FileName,
                    receiptPath = path
                };
            }).ToList();
            return JsonConvert.SerializeObject(rows);
        }

        private string? SaveFiles(List<TicketFileItem>? files)
        {
            if (files == null || files.Count == 0) return null;
            var saved = files
                .Where(f => !string.IsNullOrWhiteSpace(f.Base64Content))
                .Select(f => new
                {
                    attachmentType = string.IsNullOrWhiteSpace(f.AttachmentType) ? "OTHER" : f.AttachmentType.Trim().ToUpperInvariant(),
                    fileName = f.FileName,
                    filePath = SaveBase64(f.Base64Content, f.FileName, "TicketAttachments")
                }).ToList();
            return saved.Count == 0 ? null : JsonConvert.SerializeObject(saved);
        }

        private static string SaveBase64(string base64, string originalFileName, string folderName)
        {
            if (string.IsNullOrWhiteSpace(originalFileName)) throw new Exception("File name is required.");
            if (base64.Contains(',')) base64 = base64.Substring(base64.IndexOf(',') + 1);

            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension)) throw new Exception("Unsupported file type.");

            byte[] bytes;
            try { bytes = Convert.FromBase64String(base64); }
            catch (FormatException) { throw new Exception("Attachment data is not valid Base64."); }
            if (bytes.Length > MaxFileSizeBytes) throw new Exception("File exceeds 10MB limit.");

            var folder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", folderName);
            Directory.CreateDirectory(folder);
            var name = string.Concat(Path.GetFileNameWithoutExtension(originalFileName).Split(Path.GetInvalidFileNameChars()));
            var unique = $"{name}_{DateTime.Now:yyyyMMddHHmmssfff}{extension}";
            File.WriteAllBytes(Path.Combine(folder, unique), bytes);
            return Path.Combine("Uploads", folderName, unique).Replace("\\", "/");
        }

        private static string? FileUrl(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return null;
            var relative = filePath.Replace("Uploads/", "").Replace("Uploads\\", "");
            return "/Uploads/" + string.Join("/", relative.Split('/').Select(Uri.EscapeDataString));
        }

        /// <summary>Push + email notification, best-effort (same pattern as Lead assignment).</summary>
        private async Task Notify(int employeeId, int ticketId, string title, string message, string recipientType)
        {
            if (employeeId <= 0)
            {
                _logger.LogWarning("Ticket notification skipped: no recipient (check Reporting Manager). TicketId {TicketId}, {Title}", ticketId, title);
                return;
            }
            try
            {
                await _scheduler.SendEmployeeNotificationAsync(null, employeeId, ticketId, title, message, ENTITY_TYPE, recipientType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ticket push notification failed. TicketId {TicketId}, EmployeeId {EmployeeId}", ticketId, employeeId);
            }
            try
            {
                await _scheduler.SendEmployeeEmailNotificationAsync(null, employeeId, ticketId, title, message, ENTITY_TYPE, recipientType,
                    actionType: recipientType == "MANAGER" ? "APPROVAL" : "ASSIGNED");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ticket email notification failed. TicketId {TicketId}, EmployeeId {EmployeeId}", ticketId, employeeId);
            }
        }
        #endregion
    }
}
