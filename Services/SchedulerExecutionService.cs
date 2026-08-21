using CRMWebServices.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CRMWebServices.Services
{
    public class SchedulerExecutionService: ISchedulerExecutionService
    {
        private const string SP_NAME ="USP_CRM_SchedulerManagement";

        private readonly IDatabaseService _database;
        private readonly IFirebaseNotificationService _firebase;
        private readonly IEmailService _email;
        private readonly ILogger<SchedulerExecutionService> _logger;

        public SchedulerExecutionService(IDatabaseService database,IFirebaseNotificationService firebase,IEmailService email,ILogger<SchedulerExecutionService> logger)
        {
            _database = database;
            _firebase = firebase;
            _email = email;
            _logger = logger;
        }

        public async Task ProcessDueItemsAsync(CancellationToken cancellationToken)
        {
            var dueItems =await GetDueItemsAsync();

            if (dueItems.Rows.Count == 0)
                return;

            _logger.LogInformation("Scheduler found {Count} due items.",dueItems.Rows.Count);

            foreach (DataRow row in dueItems.Rows)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                long schedulerJobItemId =Convert.ToInt64(row["SchedulerJobItemId"]);
                await ProcessItemAsync(schedulerJobItemId);
            }
        }

        private async Task<DataTable> GetDueItemsAsync()
        {
            return await _database.ExecuteDataTableAsync(SP_NAME,

                new SqlParameter("@Status","GET_DUE_ITEMS")
            );
        }

        private async Task ProcessItemAsync(long schedulerJobItemId)
        {
            try
            {
                var claimResult =await ClaimJobItemAsync(schedulerJobItemId);

                if (claimResult == null ||claimResult.Rows.Count == 0)
                {
                    return;
                }

                bool isClaimed =Convert.ToBoolean(claimResult.Rows[0]["IsClaimed"]);

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

                        await ProcessLeadAsync(schedulerJobItemId,entityId);
                        break;

                    default:

                        _logger.LogWarning(
                            "Unsupported scheduler EntityType {EntityType} for JobItem {JobItemId}.",
                            entityType,
                            schedulerJobItemId);

                        await ExpireJobItemAsync(
                            schedulerJobItemId);

                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Scheduler item {JobItemId} failed.",schedulerJobItemId);
                await RescheduleJobItemAsync(schedulerJobItemId);
            }
        }

        private async Task ProcessLeadAsync(long schedulerJobItemId,int leadId)
        {
            // -------------------------------------------------
            // 1. Check current Lead status
            // -------------------------------------------------

            var statusTable =await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status","CHECK_LEAD_STATUS"),
                    new SqlParameter("@EntityId",leadId)
                );

            if (statusTable.Rows.Count == 0)
            {
                await ExpireJobItemAsync(
                    schedulerJobItemId);

                return;
            }

            bool isUnassigned =Convert.ToBoolean(statusTable.Rows[0]["IsUnassigned"]);

            // -------------------------------------------------
            // 2. Lead has now been assigned
            // -------------------------------------------------

            if (!isUnassigned)
            {
                _logger.LogInformation("Lead {LeadId} is already assigned. Completing scheduler item {JobItemId}.",leadId,schedulerJobItemId);

                await CompleteJobItemAsync(schedulerJobItemId);

                return;
            }

            // -------------------------------------------------
            // 3. Get Creator + Reporting Manager
            // -------------------------------------------------

            var recipients =await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status","GET_LEAD_RECIPIENTS"),
                    new SqlParameter("@EntityId",leadId)
                );

            if (recipients.Rows.Count == 0)
            {
                _logger.LogWarning("No recipients found for Lead {LeadId}.",leadId);
                await RescheduleJobItemAsync(schedulerJobItemId);

                return;
            }

            bool hasFailure = false;

            // -------------------------------------------------
            // 4. Send to every recipient
            // -------------------------------------------------

            foreach (DataRow recipient in recipients.Rows)
            {
                int employeeId =Convert.ToInt32(recipient["EmployeeId"]);
                string recipientType =recipient["RecipientType"]?.ToString()?? "RECIPIENT";

                try
                {
                    // Firebase
                    await SendFirebaseNotificationsAsync(schedulerJobItemId,leadId,employeeId,recipientType);
                }
                catch (Exception ex)
                {
                    hasFailure = true;

                    _logger.LogError(ex,"Firebase processing failed for Employee {EmployeeId}, Lead {LeadId}.",employeeId,leadId);
                }

                try
                {
                    // Email
                    await SendEmailNotificationAsync(schedulerJobItemId,leadId,employeeId,recipientType);
                }
                catch (Exception ex)
                {
                    hasFailure = true;

                    _logger.LogError(ex,"Email processing failed for Employee {EmployeeId}, Lead {LeadId}.",employeeId,leadId);
                }
            }

            // -------------------------------------------------
            // 5. Scheduler item lifecycle
            // -------------------------------------------------

            if (hasFailure)
            {
                await RescheduleJobItemAsync(
                    schedulerJobItemId);
            }
            else
            {
                await CompleteJobItemAsync(
                    schedulerJobItemId);
            }
        }
        private async Task SendFirebaseNotificationsAsync(long schedulerJobItemId, int leadId, int employeeId, string recipientType)
        {
            var devices = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status","GET_EMPLOYEE_DEVICES"),
                    new SqlParameter("@EmployeeId",employeeId)
                );

            if (devices == null || devices.Rows.Count == 0)
            {
                _logger.LogInformation("No active devices found for EmployeeId {EmployeeId}.", employeeId);
                return;
            }

            foreach (DataRow device in devices.Rows)
            {
                string deviceToken = device["DeviceToken"]?.ToString() ?? "";

                if (string.IsNullOrWhiteSpace(deviceToken))
                    continue;

                var notification = await CreateNotificationAsync(
                        schedulerJobItemId,
                        employeeId,
                        deviceToken,
                        "PUSH",
                        "Unassigned Lead",
                        $"Lead #{leadId} is still unassigned.");

                if (notification == null ||
                    notification.Rows.Count == 0)
                {
                    _logger.LogWarning("Could not create notification record for EmployeeId {EmployeeId}.", employeeId);
                    continue;
                }

                long notificationId = Convert.ToInt64(notification.Rows[0]["SchedulerNotificationId"]);

                var result = await _firebase.SendNotificationAsync(deviceToken,
                        "Unassigned Lead",
                        $"Lead #{leadId} is still unassigned.",
                        new Dictionary<string, string>
                        {
                            ["EntityType"] = "LEAD",
                            ["EntityId"] = leadId.ToString(),
                            ["RecipientType"] = recipientType
                        });

                if (result.Success)
                {
                    await MarkNotificationSentAsync(notificationId, result.MessageId);
                }
                else
                {
                    await MarkNotificationFailedAsync(notificationId, result.ErrorMessage);
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

            // --------------------------------------------------------
            // Create notification history record
            // --------------------------------------------------------

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
                // ----------------------------------------------------
                // Send Email
                // ----------------------------------------------------

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

        private async Task<DataTable?> CreateNotificationAsync(long schedulerJobItemId, int employeeId, string? deviceToken, string notificationType, string title, string message)
        {
            var dt = await _database.ExecuteDataTableAsync(SP_NAME,

                    new SqlParameter("@Status", "CREATE_NOTIFICATION"),
                    new SqlParameter("@SchedulerJobItemId", schedulerJobItemId),
                    new SqlParameter("@RecipientEmployeeId", employeeId),
                    new SqlParameter("@DeviceToken", (object?)deviceToken ?? DBNull.Value),
                    new SqlParameter("@NotificationType", notificationType),
                    new SqlParameter("@Title", title),
                    new SqlParameter("@Message", message),
                    new SqlParameter("@CreatedBy", DBNull.Value)
                );

            return dt != null && dt.Rows.Count > 0
                ? dt
                : null;
        }
    }
}