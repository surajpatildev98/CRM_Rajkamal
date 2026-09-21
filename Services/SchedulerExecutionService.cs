using CRMWebServices.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CRMWebServices.Services
{
    public class SchedulerExecutionService : ISchedulerExecutionService
    {
        private const string SP_NAME = "USP_CRM_SchedulerManagement";

        private readonly IDatabaseService _database;
        private readonly IFirebaseNotificationService _firebase;
        private readonly IEmailService _email;
        private readonly IConfiguration _config;
        private readonly ILogger<SchedulerExecutionService> _logger;

        public SchedulerExecutionService(IDatabaseService database, IFirebaseNotificationService firebase, IEmailService email, IConfiguration config, ILogger<SchedulerExecutionService> logger)
        {
            _database = database;
            _firebase = firebase;
            _email = email;
            _config = config;
            _logger = logger;
        }

        public async Task ProcessDueItemsAsync(CancellationToken cancellationToken)
        {
            var dueItems = await GetDueItemsAsync();

            if (dueItems.Rows.Count == 0)
                return;

            _logger.LogInformation("Scheduler found {Count} due items.", dueItems.Rows.Count);

            foreach (DataRow row in dueItems.Rows)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                long schedulerJobItemId = Convert.ToInt64(row["SchedulerJobItemId"]);
                await ProcessItemAsync(schedulerJobItemId);
            }
        }

        private async Task<DataTable> GetDueItemsAsync()
        {
            return await _database.ExecuteDataTableAsync(SP_NAME,

                new SqlParameter("@Status", "GET_DUE_ITEMS")
            );
        }

        private async Task ProcessItemAsync(long schedulerJobItemId)
        {
            try
            {
                var claimResult = await ClaimJobItemAsync(schedulerJobItemId);

                if (claimResult == null || claimResult.Rows.Count == 0)
                {
                    return;
                }

                bool isClaimed = Convert.ToBoolean(claimResult.Rows[0]["IsClaimed"]);

                if (!isClaimed)
                {
                    // Another worker already claimed it.
                    return;
                }

                var item = claimResult.Rows[0];
                string entityType = item["EntityType"]?.ToString() ?? "";
                int entityId = Convert.ToInt32(item["EntityId"]);

                switch (entityType.ToUpperInvariant())
                {
                    case "LEAD":
                        await ProcessLeadAsync(schedulerJobItemId, entityId);
                        break;

                    default:

                        _logger.LogWarning("Unsupported scheduler EntityType {EntityType} for JobItem {JobItemId}.",entityType,schedulerJobItemId);
                        await ExpireJobItemAsync(schedulerJobItemId);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduler item {JobItemId} failed.", schedulerJobItemId);
                await RescheduleJobItemAsync(schedulerJobItemId);
            }
        }

        private async Task ProcessLeadAsync(long schedulerJobItemId, int leadId)
        {
            // 1. Check current Lead status 
            var statusTable = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status", "CHECK_LEAD_STATUS"),
                    new SqlParameter("@EntityId", leadId)
                );

            if (statusTable.Rows.Count == 0)
            {
                await ExpireJobItemAsync(schedulerJobItemId);
                return;
            }

            bool isUnassigned = Convert.ToBoolean(statusTable.Rows[0]["IsUnassigned"]);


            //int createdByEmployeeId = Convert.ToInt32(statusTable.Rows[0]["CreatedBy"]);


            // 2. Lead has now been assigned    
            if (!isUnassigned)
            {
                _logger.LogInformation("Lead {LeadId} is already assigned. Completing scheduler item {JobItemId}.", leadId, schedulerJobItemId);

                await CompleteJobItemAsync(schedulerJobItemId);
                return;
            }

            // 3. Get Creator + Reporting Manager       
            var recipients = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status", "GET_LEAD_RECIPIENTS"),
                    new SqlParameter("@EntityId", leadId)
                );

            if (recipients.Rows.Count == 0)
            {
                _logger.LogWarning("No recipients found for Lead {LeadId}.", leadId);
                await RescheduleJobItemAsync(schedulerJobItemId);
                return;
            }

            bool hasFailure = false;

            // 4. Send to every recipient   
            foreach (DataRow recipient in recipients.Rows)
            {
                int employeeId = Convert.ToInt32(recipient["EmployeeId"]);
                string recipientType = recipient["RecipientType"]?.ToString() ?? "RECIPIENT";

                try
                {
                    // Firebase
                    //await SendFirebaseNotificationsAsync(schedulerJobItemId, leadId, employeeId, recipientType);
                    await NotifyUnassignedLeadAsync(leadId);

                }
                catch (Exception ex)
                {
                    hasFailure = true;
                    _logger.LogError(ex, "Firebase processing failed for Employee {EmployeeId}, Lead {LeadId}.", employeeId, leadId);
                }

                try
                {
                    // Email
                    await SendEmailNotificationAsync(schedulerJobItemId, leadId, employeeId, recipientType);
                }
                catch (Exception ex)
                {
                    hasFailure = true;
                    _logger.LogError(ex, "Email processing failed for Employee {EmployeeId}, Lead {LeadId}.", employeeId, leadId);
                }
            }

            // 5. Scheduler item lifecycle
            if (hasFailure)
            {
                await RescheduleJobItemAsync(schedulerJobItemId);
            }
            else
            {
                await CompleteJobItemAsync(schedulerJobItemId);
            }
        }

        public async Task SendEmployeeNotificationAsync(long? schedulerJobItemId, int employeeId, int entityId, string title, string message, string entityType, string recipientType)
        {
            var devices = await _database.ExecuteDataTableAsync("USP_CRM_SchedulerManagement",
                new SqlParameter("@Status", "GET_EMPLOYEE_DEVICES"),
                new SqlParameter("@EmployeeId", employeeId)
     );

            if (devices == null || devices.Rows.Count == 0)
            {
                _logger.LogInformation("No active device found for EmployeeId {EmployeeId}.",employeeId);
                return;
            }

            DataRow device = devices.Rows[0];

            string deviceToken =device["DeviceToken"]?.ToString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(deviceToken))
            {
                _logger.LogWarning("No Firebase token found for EmployeeId {EmployeeId}.",employeeId);
                return;
            }

            try
            {
                long notificationId = 0;                

                if (schedulerJobItemId.HasValue)
                {
                    var notification = await CreateNotificationAsync(
                        schedulerJobItemId,
                        employeeId,
                        deviceToken,
                        "PUSH",
                        title,
                        message);

                    if (notification == null ||
                        notification.Rows.Count == 0)
                    {
                        _logger.LogWarning("Could not create notification record for EmployeeId {EmployeeId}.",employeeId);
                        return;
                    }
                    notificationId = Convert.ToInt64(notification.Rows[0]["SchedulerNotificationId"]);
                }
                
                // SEND FIREBASE NOTIFICATION
               
                var result = await _firebase.SendNotificationAsync(
                    deviceToken,
                    title,
                    message,
                    new Dictionary<string, string>
                    {
                        ["EntityType"] = entityType,
                        ["EntityId"] = entityId.ToString(),
                        ["RecipientType"] = recipientType
                    });
                              
                // UPDATE SCHEDULER NOTIFICATION
                // ONLY IF IT WAS CREATED
                if (result.Success)
                {
                    if (schedulerJobItemId.HasValue && notificationId > 0)
                    {
                        await MarkNotificationSentAsync(notificationId,result.MessageId);
                    }

                    _logger.LogInformation("Firebase notification sent successfully to EmployeeId {EmployeeId}.",employeeId);
                }
                else
                {
                    if (schedulerJobItemId.HasValue && notificationId > 0)
                    {
                        await MarkNotificationFailedAsync(notificationId,result.ErrorMessage);
                    }

                    _logger.LogWarning("Firebase notification failed for EmployeeId {EmployeeId}. Error: {Error}",employeeId,result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Notification failed for EmployeeId {EmployeeId}.",employeeId);

                throw;
            }

        }
        public async Task NotifyUnassignedLeadAsync(int leadId)
        {
            _logger.LogInformation("Starting unassigned lead notification. LeadId: {LeadId}", leadId);

            var recipients = await _database.ExecuteDataTableAsync(
                SP_NAME,
                new SqlParameter("@Status", "GET_LEAD_RECIPIENTS"),
                new SqlParameter("@EntityId", leadId)
            );

            if (recipients == null || recipients.Rows.Count == 0)
            {
                _logger.LogWarning("No recipients found for Lead {LeadId}.", leadId);

                return;
            }

            _logger.LogInformation("Found {Count} recipient(s) for Lead {LeadId}.", recipients.Rows.Count, leadId);

            foreach (DataRow recipient in recipients.Rows)
            {
                int employeeId = Convert.ToInt32(recipient["EmployeeId"]);
                string recipientType = recipient["RecipientType"]?.ToString() ?? "";

                if (!string.Equals(recipientType, "CREATOR", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(recipientType, "MANAGER", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string title;
                string message;

                if (string.Equals(recipientType, "CREATOR", StringComparison.OrdinalIgnoreCase))
                {
                    title = "Lead Created";
                    message = $"Your lead #{leadId} is currently unassigned.";
                }
                else
                {
                    title = "Unassigned Lead";
                    message = $"Lead #{leadId} created by your team is currently unassigned.";
                }

                try
                {
                    await SendEmployeeNotificationAsync(
                        schedulerJobItemId: null,
                        employeeId: employeeId,
                        entityId: leadId,
                        title: title,
                        message: message,
                        entityType: "LEAD",
                        recipientType: recipientType);

                    //Added By Suraj on 01 - 09 - 2026: Send Email Notification for Unassigned Lead
                    await SendEmployeeEmailNotificationAsync(
                        schedulerJobItemId: null,
                        employeeId: employeeId,
                        entityId: leadId,
                        title: "Unassigned Lead",
                        message: $"Lead #{leadId} is still unassigned.",
                        entityType: "LEAD",
                        recipientType: "MANAGER",
                        actionType: "UNASSIGNED");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to notify EmployeeId {EmployeeId} for Lead {LeadId}.", employeeId, leadId);
                }
            }
        }
       
        private async Task<DataTable> ClaimJobItemAsync(long schedulerJobItemId)
        {
            return await _database.ExecuteDataTableAsync(SP_NAME,

                new SqlParameter("@Status", "CLAIM_JOB_ITEM"),
                new SqlParameter("@SchedulerJobItemId", schedulerJobItemId),
                new SqlParameter("@UpdatedBy", DBNull.Value)
            );
        }

        private async Task MarkNotificationSentAsync(long notificationId, string? firebaseMessageId)
        {
            await _database.ExecuteDataTableAsync(SP_NAME,
                new SqlParameter("@Status", "MARK_NOTIFICATION_SENT"),
                new SqlParameter("@SchedulerNotificationId", notificationId),
                new SqlParameter("@FirebaseMessageId", (object?)firebaseMessageId ?? DBNull.Value),
                new SqlParameter("@UpdatedBy", DBNull.Value)
            );
        }

        private async Task MarkNotificationFailedAsync(long notificationId, string? errorMessage)
        {
            await _database.ExecuteDataTableAsync(SP_NAME,

                new SqlParameter("@Status", "MARK_NOTIFICATION_FAILED"),
                new SqlParameter("@SchedulerNotificationId", notificationId),
                new SqlParameter("@ErrorMessage", (object?)errorMessage ?? DBNull.Value),
                new SqlParameter("@UpdatedBy", DBNull.Value)
            );
        }

        private async Task CompleteJobItemAsync(long schedulerJobItemId)
        {
            await _database.ExecuteDataTableAsync(SP_NAME,
                new SqlParameter("@Status", "COMPLETE_JOB_ITEM"),
                new SqlParameter("@SchedulerJobItemId", schedulerJobItemId),
                new SqlParameter("@UpdatedBy", DBNull.Value)
            );
        }
        private async Task RescheduleJobItemAsync(long schedulerJobItemId)
        {
            await _database.ExecuteDataTableAsync(SP_NAME,
                new SqlParameter("@Status", "RESCHEDULE_JOB_ITEM"),
                new SqlParameter("@SchedulerJobItemId", schedulerJobItemId),
                new SqlParameter("@UpdatedBy", DBNull.Value)
            );
        }
        private async Task ExpireJobItemAsync(long schedulerJobItemId)
        {
            await _database.ExecuteDataTableAsync(SP_NAME,
                new SqlParameter("@Status", "EXPIRE_JOB_ITEM"),
                new SqlParameter("@SchedulerJobItemId", schedulerJobItemId),
                new SqlParameter("@UpdatedBy", DBNull.Value)
            );
        }
        private async Task<DataTable?> GetEmployeeEmailAsync(int employeeId)
        {
            return await _database.ExecuteDataTableAsync(SP_NAME,
                new SqlParameter("@Status", "GET_EMPLOYEE_EMAIL"),
                new SqlParameter("@EmployeeId", employeeId)
            );
        }
        private async Task SendEmailNotificationAsync(long schedulerJobItemId, int leadId, int employeeId, string recipientType)
        {
            var employeeTable = await GetEmployeeEmailAsync(employeeId);

            if (employeeTable == null || employeeTable.Rows.Count == 0)
            {
                _logger.LogWarning("No active email found for EmployeeId {EmployeeId}.", employeeId);
                return;
            }

            var employeeRow = employeeTable.Rows[0];

            string email = employeeRow["OfficialEmail"]?.ToString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Official email is empty for EmployeeId {EmployeeId}.", employeeId);
                return;
            }

            string subject = "CRM - Unassigned Lead Notification";

            string message = $"Lead #{leadId} is currently unassigned. " +
                "Please review and assign the lead.";

            string body = $@"
                <html>
                <body>
                    <p>Hello,</p>

                    <p>
                        Lead <strong>#{leadId}</strong>
                        is currently unassigned.
                    </p>

                    <p>
                        Please review and assign the lead.
                    </p>

                    <br />

                    <p>
                        CRM System
                    </p>
                </body>
                </html>";


            // Create notification history record


            var notificationTable = await CreateNotificationAsync(
                    schedulerJobItemId,
                    employeeId,
                    null,
                    "EMAIL",
                    subject,
                    message);

            if (notificationTable == null || notificationTable.Rows.Count == 0)
            {
                _logger.LogWarning("Notification record was not created for EmployeeId {EmployeeId}.", employeeId);
                return;
            }

            long notificationId = Convert.ToInt64(notificationTable.Rows[0]["SchedulerNotificationId"]);

            try
            {
                // Send Email
                var result =
                    await _email.SendEmailAsync(email, subject, body);

                if (result.Success)
                {
                    await MarkNotificationSentAsync(notificationId, null);
                    _logger.LogInformation("Email notification sent successfully to EmployeeId {EmployeeId}.", employeeId);
                }
                else
                {
                    await MarkNotificationFailedAsync(notificationId, result.ErrorMessage);
                    _logger.LogWarning("Email notification failed for EmployeeId {EmployeeId}. Error: {Error}", employeeId, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                await MarkNotificationFailedAsync(notificationId, ex.Message);
                throw;
            }
        }

        public async Task SendEmployeeEmailNotificationAsync(long? schedulerJobItemId, int employeeId, int entityId,string title, string message, string entityType,string recipientType, string actionType)
        {
            var employeeTable = await GetEmployeeEmailAsync(employeeId);

            if (employeeTable == null || employeeTable.Rows.Count == 0)
            {
                _logger.LogWarning("No active email found for EmployeeId {EmployeeId}.", employeeId);
                return;
            }

            string email = employeeTable.Rows[0]["OfficialEmail"]?.ToString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Official email is empty for EmployeeId {EmployeeId}.", employeeId);
                return;
            }

            string color = ActionColors.TryGetValue(actionType, out var c) ? c : "#333333";
            string logoUrl = $"{_config["EmailSettings:PublicBaseUrl"]}/assets/companylogo.jpg";
            string body = BuildEmailBody(title, color, message, logoUrl);

            var notificationTable = await CreateNotificationAsync(
                    schedulerJobItemId, employeeId, null, "EMAIL", title, message);

            if (notificationTable == null || notificationTable.Rows.Count == 0)
            {
                _logger.LogWarning("Notification record was not created for EmployeeId {EmployeeId}.", employeeId);
                return;
            }

            long notificationId = Convert.ToInt64(notificationTable.Rows[0]["SchedulerNotificationId"]);

            try
            {
                var result = await _email.SendEmailAsync(email, title, body);

                if (result.Success)
                {
                    await MarkNotificationSentAsync(notificationId, null);
                    _logger.LogInformation("Email notification sent successfully to EmployeeId {EmployeeId}.", employeeId);
                }
                else
                {
                    await MarkNotificationFailedAsync(notificationId, result.ErrorMessage);
                    _logger.LogWarning("Email notification failed for EmployeeId {EmployeeId}. Error: {Error}", employeeId, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                await MarkNotificationFailedAsync(notificationId, ex.Message);
                throw;
            }
        }
        private async Task<DataTable?> CreateNotificationAsync(long? schedulerJobItemId, int employeeId, string? deviceToken, string notificationType, string title, string message)
        {
            var dt = await _database.ExecuteDataTableAsync(SP_NAME,

                    new SqlParameter("@Status", "CREATE_NOTIFICATION"),
                    new SqlParameter("@SchedulerJobItemId", (object?)schedulerJobItemId ?? DBNull.Value),
                    new SqlParameter("@RecipientEmployeeId", employeeId),
                    new SqlParameter("@DeviceToken", (object?)deviceToken ?? DBNull.Value),
                    new SqlParameter("@NotificationType", notificationType),
                    new SqlParameter("@Title", title),
                    new SqlParameter("@Message", message),
                    new SqlParameter("@CreatedBy", DBNull.Value)
                );

            return dt != null && dt.Rows.Count > 0 ? dt : null;
        }

        private static readonly Dictionary<string, string> ActionColors = new()
        {
            ["CREATED"] = "#6A1B9A",
            ["ASSIGNED"] = "#2E7D32",
            ["TRANSFERRED"] = "#1565C0",
            ["UNASSIGNED"] = "#C8161D",
        };

        private string BuildEmailBody(string headerText, string color, string message, string logoUrl)
        {
            return $@"
                <html>
                <body style=""margin:0;padding:0;background-color:#f4f4f7;font-family:Segoe UI,Arial,sans-serif;"">
                    <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f4f4f7;padding:30px 0;"">
                        <tr>
                            <td align=""center"">
                                <table role=""presentation"" width=""480"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 6px rgba(0,0,0,0.08);"">

                                    <!-- Header -->
                                    <tr>
                                        <td style=""padding:24px 32px;border-bottom:3px solid {color};text-align:center;"">
                                            <img src=""{logoUrl}"" alt=""Rajkamal Bar Scan Systems Pvt. Ltd."" width=""220"" style=""display:block;height:auto;max-width:220px;margin:0 auto 12px auto;"" />
                                            <span style=""font-size:16px;font-weight:600;color:{color};"">{headerText}</span>
                                        </td>
                                    </tr>

                                    <!-- Body -->
                                    <tr>
                                        <td style=""padding:32px;"">
                                            <p style=""margin:0 0 20px 0;font-size:15px;color:#333333;"">Hello,</p>

                                            <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
                                                <tr>
                                                    <td align=""center"" style=""padding:8px 0 4px 0;"">
                                                        <span style=""display:inline-block;background-color:{color}1A;border:1px dashed {color};border-radius:6px;padding:14px 24px;font-size:15px;font-weight:600;color:{color};line-height:1.5;"">
                                                            {message}
                                                        </span>
                                                    </td>
                                                </tr>
                                            </table>

                                            <p style=""margin:24px 0 0 0;font-size:13px;color:#999999;"">
                                                This is an automated message from the CRM System.
                                            </p>
                                        </td>
                                    </tr>

                                    <!-- Footer -->
                                    <tr>
                                        <td style=""background-color:#fafafa;padding:20px 32px;border-top:1px solid #eeeeee;"">
                                            <p style=""margin:0;font-size:12px;color:#999999;"">
                                                Rajkamal Bar Scan Systems Pvt. Ltd. — Please do not reply to this email.
                                            </p>
                                        </td>
                                    </tr>

                                </table>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>";
        }
    }
}