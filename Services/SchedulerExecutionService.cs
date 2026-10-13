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
        private readonly ISchedulerEmailComposer _emailComposer;

        public SchedulerExecutionService(IDatabaseService database, IFirebaseNotificationService firebase, IEmailService email, IConfiguration config, ILogger<SchedulerExecutionService> logger, ISchedulerEmailComposer emailComposer )
        {
            _database = database;
            _firebase = firebase;
            _email = email;
            _config = config;
            _logger = logger;
            _emailComposer = emailComposer;
        }

        #region Process Due Items AND Claim Job Item
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

                    case "EMPLOYEE_PROBATION":
                        await ProcessEmployeeProbationAsync(schedulerJobItemId, entityId);
                        break;

                    case "EMPLOYEE_BIRTHDAY":
                        await ProcessEmployeeBirthdayAsync(schedulerJobItemId);
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
        #endregion

        #region Process Lead 
        private async Task ProcessLeadAsync(long schedulerJobItemId, int leadId)
        {
            try
            {
                // 1. Check current Lead status
                var statusTable = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status", "CHECK_LEAD_STATUS"),
                    new SqlParameter("@EntityId", leadId)
                );

                if (statusTable == null || statusTable.Rows.Count == 0)
                {
                    await ExpireJobItemAsync(schedulerJobItemId);
                    return;
                }

                bool isUnassigned = Convert.ToBoolean(statusTable.Rows[0]["IsUnassigned"]);

                // 2. Lead is assigned -> COMPLETE
                if (!isUnassigned)
                {
                    _logger.LogInformation("Lead {LeadId} is assigned. Completing scheduler item {JobItemId}.",leadId,schedulerJobItemId);
                    await CompleteJobItemAsync(schedulerJobItemId);
                    return;
                }

                // 3. Lead is still unassigned -> send notification
                _logger.LogInformation("Lead {LeadId} is still unassigned. Sending notification for JobItem {JobItemId}.",leadId,schedulerJobItemId);
                await NotifyUnassignedLeadAsync(leadId);

                // 4. IMPORTANT:
                // Successful notification does NOT complete the scheduler item.
                // Lead is still unassigned, so schedule the next attempt.
                await RescheduleJobItemAsync(schedulerJobItemId);

                _logger.LogInformation("Lead {LeadId} is still unassigned. Scheduler item {JobItemId} rescheduled.",leadId,schedulerJobItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Error processing unassigned Lead {LeadId}, JobItem {JobItemId}.",leadId,schedulerJobItemId);
                // Even if notification fails, retry according to scheduler configuration.
                await RescheduleJobItemAsync(schedulerJobItemId);
            }
        }
        #endregion

        #region Send Employee Notification [Firebase]
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
                    var notification = await CreateNotificationAsync(schedulerJobItemId,employeeId,deviceToken,"PUSH",title,message);

                    if (notification == null ||
                        notification.Rows.Count == 0)
                    {
                        _logger.LogWarning("Could not create notification record for EmployeeId {EmployeeId}.",employeeId);
                        return;
                    }
                    notificationId = Convert.ToInt64(notification.Rows[0]["SchedulerNotificationId"]);
                }
                
                // SEND FIREBASE NOTIFICATION
               
                var result = await _firebase.SendNotificationAsync(deviceToken,title,message,
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
        #endregion

        #region Notify Unassigned Lead at the time of Lead Creation
        public async Task NotifyUnassignedLeadAsync(int leadId)
        {
            _logger.LogInformation("Starting unassigned lead notification. LeadId: {LeadId}", leadId);

            var recipients = await _database.ExecuteDataTableAsync(SP_NAME,
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

                    await SendEmployeeEmailNotificationAsync(
                        schedulerJobItemId: null,
                        employeeId: employeeId,
                        entityId: leadId,
                        title: title,
                        message: message,
                        entityType: "LEAD",
                        recipientType: recipientType,
                        actionType: "UNASSIGNED",
                        templateData: new Dictionary<string, string?> { ["LeadId"] = leadId.ToString() });
                }                
                catch (Exception ex)
                {
                    _logger.LogError(ex,"Failed to notify EmployeeId {EmployeeId} for Lead {LeadId}.",employeeId,leadId);
                    throw;
                }
            }
        }
        #endregion

        #region Database Operations For Scheduler
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
        #endregion       

        #region Email Notification for Employee Probation Reminder       
     
        public async Task SendEmployeeEmailNotificationAsync(long? schedulerJobItemId, int employeeId, int entityId, string title, string message, string entityType, string recipientType, string actionType, IReadOnlyDictionary<string, string?>? templateData = null)
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

            string body = _emailComposer.Compose(actionType, title, message, recipientType, templateData);

            //var notificationTable = await CreateNotificationAsync(schedulerJobItemId, employeeId, null, "EMAIL", title, message);

            var notificationTable = await CreateNotificationAsync(schedulerJobItemId,employeeId,entityId,entityType,null,"EMAIL",title,message);

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
        #endregion

        #region Create Notification
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

        private async Task<DataTable?> CreateNotificationAsync(long? schedulerJobItemId,int employeeId,int entityId,string entityType,string? deviceToken,string notificationType,string title,string message)
        {
            var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                new SqlParameter("@Status", "CREATE_NOTIFICATION"),
                new SqlParameter("@SchedulerJobItemId",(object?)schedulerJobItemId ?? DBNull.Value),
                new SqlParameter("@RecipientEmployeeId", employeeId),
                new SqlParameter("@DeviceToken",(object?)deviceToken ?? DBNull.Value),
                new SqlParameter("@NotificationType", notificationType),
                new SqlParameter("@Title", title),
                new SqlParameter("@Message", message),
                new SqlParameter("@EntityType", entityType),
                new SqlParameter("@EntityId", entityId),
                new SqlParameter("@CreatedBy", DBNull.Value)
            );

            return dt != null && dt.Rows.Count > 0 ? dt : null;
        }
        #endregion

        #region Process Employee Probation Reminder
        private async Task ProcessEmployeeProbationAsync(long schedulerJobItemId,int employeeProbationId)
        {
            try
            {
                _logger.LogInformation("Processing employee probation reminder. EmployeeProbationId: {EmployeeProbationId}, JobItemId: {JobItemId}",employeeProbationId,schedulerJobItemId);

                var recipients = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status", "GET_EMPLOYEE_PROBATION_RECIPIENTS"),
                    new SqlParameter("@EntityId", employeeProbationId)
                );

                if (recipients == null || recipients.Rows.Count == 0)
                {
                    _logger.LogWarning("No recipients found for EmployeeProbationId {EmployeeProbationId}.",employeeProbationId);
                    await RescheduleJobItemAsync(schedulerJobItemId);
                    return;
                }

                foreach (DataRow recipient in recipients.Rows)
                {
                    int employeeId = Convert.ToInt32(recipient["EmployeeId"]);
                    string recipientType =recipient["RecipientType"]?.ToString() ?? "RECIPIENT";
                    string employeeName =recipient["EmployeeName"]?.ToString() ?? "Employee";
                    string probationEndDate =recipient["ProbationEndDate"] == DBNull.Value? "": Convert.ToDateTime(recipient["ProbationEndDate"]).ToString("dd-MM-yyyy");
                    string title = "Probation Period Ending";
                    string message =$"Probation period of {employeeName} is ending on {probationEndDate}.";

                    // Firebase notification
                    await SendEmployeeNotificationAsync(
                        schedulerJobItemId: schedulerJobItemId,
                        employeeId: employeeId,
                        entityId: employeeProbationId,
                        title: title,
                        message: message,
                        entityType: "EMPLOYEE_PROBATION",
                        recipientType: recipientType
                    );

                    // Email notification
                    await SendEmployeeEmailNotificationAsync(
                        schedulerJobItemId: schedulerJobItemId,
                        employeeId: employeeId,
                        entityId: employeeProbationId,
                        title: title,
                        message: message,
                        entityType: "EMPLOYEE_PROBATION",
                        recipientType: recipientType,
                        actionType: "PROBATION_EXPIRY",
                        templateData: new Dictionary<string, string?>
                        {
                            ["EmployeeName"] = employeeName,
                            ["ProbationEndDate"] = probationEndDate
                        }
                    );
                }

                await CompleteJobItemAsync(schedulerJobItemId);

                _logger.LogInformation("Employee probation reminder completed successfully. EmployeeProbationId: {EmployeeProbationId}, JobItemId: {JobItemId}",employeeProbationId,schedulerJobItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Error processing employee probation. EmployeeProbationId: {EmployeeProbationId}, JobItemId: {JobItemId}",employeeProbationId,schedulerJobItemId);
                throw;
            }
        }
        #endregion

        #region Process Employee Birthday Reminder
        private async Task ProcessEmployeeBirthdayAsync(long schedulerJobItemId)
        {
            try
            {
                _logger.LogInformation("Processing employee birthday reminders. JobItemId: {JobItemId}",schedulerJobItemId);

                var birthdays = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status","GET_TODAY_EMPLOYEE_BIRTHDAYS")
                );

                if (birthdays == null || birthdays.Rows.Count == 0)
                {
                    _logger.LogInformation("No employee birthdays found for today.");
                    await CompleteJobItemAsync(schedulerJobItemId);
                    return;
                }

                foreach (DataRow row in birthdays.Rows)
                {
                    int employeeId =Convert.ToInt32(row["EmployeeId"]);
                    string employeeName =row["EmployeeName"]?.ToString()?? "Employee";
                    int? managerId =row["Ref_ReportingManagerId"] == DBNull.Value? null: Convert.ToInt32(row["Ref_ReportingManagerId"]);
                    int? hrManagerId =row["Ref_ReportingHRManagerId"] == DBNull.Value? null: Convert.ToInt32(row["Ref_ReportingHRManagerId"]);
                    string title ="Employee Birthday Reminder";
                    string message =$"Today is {employeeName}'s birthday.";
                   
                    // REPORTING MANAGER         
                    if (managerId.HasValue)
                    {
                        await SendEmployeeNotificationAsync(
                            schedulerJobItemId: schedulerJobItemId,
                            employeeId: managerId.Value,
                            entityId: employeeId,
                            title: title,
                            message: message,
                            entityType: "EMPLOYEE_BIRTHDAY",
                            recipientType: "MANAGER"
                        );

                        await SendEmployeeEmailNotificationAsync(
                            schedulerJobItemId: schedulerJobItemId,
                            employeeId: managerId.Value,
                            entityId: employeeId,
                            title: title,
                            message: message,
                            entityType: "EMPLOYEE_BIRTHDAY",
                            recipientType: "MANAGER",
                            actionType: "BIRTHDAY",
                            templateData: new Dictionary<string, string?>
                            {
                                ["EmployeeName"] = employeeName,
                                ["BirthdayDate"] = DateTime.Today.ToString("dd-MM-yyyy")
                            }
                        );


                    }
                                       
                    // REPORTING HR MANAGER              
                    if (hrManagerId.HasValue &&
                        hrManagerId.Value != managerId)
                    {
                        await SendEmployeeNotificationAsync(
                            schedulerJobItemId: schedulerJobItemId,
                            employeeId: hrManagerId.Value,
                            entityId: employeeId,
                            title: title,
                            message: message,
                            entityType: "EMPLOYEE_BIRTHDAY",
                            recipientType: "HR_MANAGER"
                        );

                        await SendEmployeeEmailNotificationAsync(
                            schedulerJobItemId: schedulerJobItemId,
                            employeeId: hrManagerId.Value,
                            entityId: employeeId,
                            title: title,
                            message: message,
                            entityType: "EMPLOYEE_BIRTHDAY",
                            recipientType: "HR_MANAGER",
                            actionType: "BIRTHDAY",
                            templateData: new Dictionary<string, string?>
                            {
                                ["EmployeeName"] = employeeName,
                                ["BirthdayDate"] = DateTime.Today.ToString("dd-MM-yyyy")
                            }
                        );

                        // BIRTHDAY EMPLOYEE
                        // BIRTHDAY EMPLOYEE
                        await SendEmployeeEmailNotificationAsync(
                            schedulerJobItemId: schedulerJobItemId,
                            employeeId: employeeId,
                            entityId: employeeId,
                            title: $"🎉 Happy Birthday, {employeeName}!",
                            message: $"Happy Birthday, {employeeName}!",
                            entityType: "EMPLOYEE_BIRTHDAY",
                            recipientType: "BIRTHDAY_EMPLOYEE",
                            actionType: "BIRTHDAY_WISH",
                            templateData: new Dictionary<string, string?>
                            {
                                ["EmployeeName"] = employeeName,
                                ["HeaderLabel"] = "Birthday Wishes"
                            }
                        );
                    }
                }

                await CompleteJobItemAsync(schedulerJobItemId);

                _logger.LogInformation("Employee birthday reminders completed. JobItemId: {JobItemId}",schedulerJobItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Error processing employee birthday reminders. JobItemId: {JobItemId}",schedulerJobItemId);
                throw;
            }
        }
        #endregion

    }
}