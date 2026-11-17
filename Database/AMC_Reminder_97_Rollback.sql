/* Rollback for AMC_Reminder_03_UAT.sql - removes the AMC expiry scheduler job, its items and notifications.
   Run before AMC_SP_98_Rollback.sql. */
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @JobIds TABLE (Id INT);
INSERT INTO @JobIds SELECT SchedulerJobId FROM dbo.CRM_SchedulerJob WHERE ScopeCode = 'AMC_ExpiryReminder';
DELETE n FROM dbo.CRM_SchedulerNotification n
JOIN dbo.CRM_SchedulerJobItem i ON i.SchedulerJobItemId = n.SchedulerJobItemId
WHERE i.SchedulerJobId IN (SELECT Id FROM @JobIds);
DELETE FROM dbo.CRM_SchedulerJobItem WHERE SchedulerJobId IN (SELECT Id FROM @JobIds);
DELETE FROM dbo.CRM_SchedulerJob WHERE SchedulerJobId IN (SELECT Id FROM @JobIds);
COMMIT;
