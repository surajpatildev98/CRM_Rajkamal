SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* =====================================================================
   WFH confirmation (Manual Punch screen)
   Step 1 (this file): add ORG_Attendance.Ref_WfhConfirmedById + FK.
   Step 2: apply the @WfhConfirmedById changes to USP_ORG_Attendance
           (MANUAL_PUNCH stores it; GET returns it).
   Rollback:
     ALTER TABLE dbo.ORG_Attendance DROP CONSTRAINT FK_ORG_Attendance_WfhConfirmedBy;
     ALTER TABLE dbo.ORG_Attendance DROP COLUMN Ref_WfhConfirmedById;
   ===================================================================== */

-- Remove objects from earlier drafts of this script, if they were run.
IF OBJECT_ID('dbo.USP_ORG_AttendanceWfhApproval', 'P') IS NOT NULL
    DROP PROCEDURE dbo.USP_ORG_AttendanceWfhApproval;
IF OBJECT_ID('dbo.ORG_AttendanceWfhApproval', 'U') IS NOT NULL
    DROP TABLE dbo.ORG_AttendanceWfhApproval;
GO

IF COL_LENGTH('dbo.ORG_Attendance', 'Ref_WfhConfirmedById') IS NULL
    ALTER TABLE dbo.ORG_Attendance ADD Ref_WfhConfirmedById INT NULL;
GO

IF OBJECT_ID('dbo.FK_ORG_Attendance_WfhConfirmedBy', 'F') IS NULL
    ALTER TABLE dbo.ORG_Attendance WITH CHECK
        ADD CONSTRAINT FK_ORG_Attendance_WfhConfirmedBy
        FOREIGN KEY (Ref_WfhConfirmedById) REFERENCES dbo.ORG_Employee (EmployeeId);
GO
