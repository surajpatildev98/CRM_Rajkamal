/* =====================================================================
   AMC expiry reminder - scheduler job + first job item (re-runnable)
   Needs AMC_SP_02_UAT.sql (permission AMC_ExpiryReminder, EntityType AMC_EXPIRY).
   The job runs once a day at 09:00; after each run the worker schedules the next day itself.
   Rollback: AMC_Reminder_97_Rollback.sql
   ===================================================================== */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

DECLARE @JobId INT = (SELECT TOP 1 SchedulerJobId FROM dbo.CRM_SchedulerJob WHERE ScopeCode = 'AMC_ExpiryReminder' ORDER BY SchedulerJobId);
DECLARE @FirstRun DATETIME2(3) = DATEADD(HOUR, 9, CAST(CAST(DATEADD(DAY, 1, GETDATE()) AS DATE) AS DATETIME2(3)));

IF (@JobId IS NULL)
BEGIN
    DECLARE @Job TABLE (SchedulerJobId INT, JobCode VARCHAR(100), JobName VARCHAR(200), JobType VARCHAR(100), Description VARCHAR(500),
        EntityType VARCHAR(50), ScopeCode VARCHAR(100), ScopeName VARCHAR(200), IsActive BIT, FrequencyType VARCHAR(20), FrequencyValue INT,
        StartDate DATETIME2(3), EndDate DATETIME2(3), MaxAttempts INT, StopAfterMinutes INT, NextRunOn DATETIME2(3), LastRunOn DATETIME2(3),
        IsRunning BIT, CreatedOn DATETIME2(3), CreatedBy INT, UpdatedOn DATETIME2(3), UpdatedBy INT);
    INSERT INTO @Job
    EXEC dbo.USP_CRM_SchedulerManagement @Status = 'CREATE_JOB', @JobCode = 'AMC_EXPIRY_REMINDER', @JobName = 'AMC Expiry Reminder',
         @JobType = 'EMAIL', @Description = 'Daily 09:00: customer reminder mail 30/15/10/5/1 days before AMC end date, CC employees with AMC_ExpiryReminder.',
         @EntityType = 'AMC_EXPIRY', @ScopeCode = 'AMC_ExpiryReminder', @IsActive = 1, @FrequencyType = 'DAYS', @FrequencyValue = 1,
         @StartDate = @FirstRun, @MaxAttempts = 3, @CreatedBy = 1;
    SELECT @JobId = SchedulerJobId FROM @Job;
END

-- One daily item (EntityId 0 = all contracts). CREATE_JOB_ITEM does nothing if one is already pending.
EXEC dbo.USP_CRM_SchedulerManagement @Status = 'CREATE_JOB_ITEM', @SchedulerJobId = @JobId, @EntityType = 'AMC_EXPIRY',
     @EntityId = 0, @ScheduledOn = @FirstRun, @CreatedBy = 1, @SuppressResultSet = 1;

SELECT j.SchedulerJobId, j.JobCode, j.EntityType, j.ScopeCode, j.FrequencyType, j.FrequencyValue, i.SchedulerJobItemId, i.Status, i.NextRunOn
FROM dbo.CRM_SchedulerJob j
LEFT JOIN dbo.CRM_SchedulerJobItem i ON i.SchedulerJobId = j.SchedulerJobId AND i.Status = 'PENDING'
WHERE j.SchedulerJobId = @JobId;
