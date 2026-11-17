SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* =====================================================================
   Daily Login Activity  (Main menu card for Administrator / HR Manager)
   - ORG_EmployeeLoginLog : one row per successful login (written by api/auth/login)
   - USP_ORG_LoginActivity
       @Status = 'LOG_LOGIN'          -> insert a login row
       @Status = 'CAN_VIEW'           -> CanView bit (role code SUPERADMIN / ADMIN / HRMANAGER)
       @Status = 'GET_DAY_SUMMARY'    -> T0 counts, T1 logged-in list, T2 not-logged-in list
   Only new objects; no existing table or SP is changed.
   Rollback: DROP PROCEDURE dbo.USP_ORG_LoginActivity; DROP TABLE dbo.ORG_EmployeeLoginLog;
   ===================================================================== */

IF OBJECT_ID('dbo.ORG_EmployeeLoginLog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ORG_EmployeeLoginLog
    (
        EmployeeLoginLogId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ORG_EmployeeLoginLog PRIMARY KEY,
        Ref_EmployeeId     INT           NOT NULL,
        LoginDate          DATE          NOT NULL,
        LoginTime          DATETIME2     NOT NULL CONSTRAINT DF_ORG_EmployeeLoginLog_LoginTime DEFAULT SYSDATETIME(),
        DeviceId           NVARCHAR(200) NULL,
        DeviceType         NVARCHAR(50)  NULL,
        AppVersion         NVARCHAR(50)  NULL
    );
    CREATE INDEX IX_ORG_EmployeeLoginLog_Date_Employee
        ON dbo.ORG_EmployeeLoginLog (LoginDate, Ref_EmployeeId) INCLUDE (LoginTime);
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_ORG_LoginActivity
    @Status     VARCHAR(50),
    @EmployeeId INT           = NULL,
    @Date       DATE          = NULL,
    @DeviceId   NVARCHAR(200) = NULL,
    @DeviceType NVARCHAR(50)  = NULL,
    @AppVersion NVARCHAR(50)  = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @Status = 'LOG_LOGIN'
    BEGIN
        INSERT INTO dbo.ORG_EmployeeLoginLog (Ref_EmployeeId, LoginDate, LoginTime, DeviceId, DeviceType, AppVersion)
        VALUES (@EmployeeId, CAST(SYSDATETIME() AS DATE), SYSDATETIME(), @DeviceId, @DeviceType, @AppVersion);
        RETURN;
    END

    IF @Status = 'CAN_VIEW'
    BEGIN
        -- Super Admin / Admin / HR Manager by role code (active roles only)
        SELECT CAST(CASE WHEN EXISTS (
                   SELECT 1
                   FROM dbo.ORG_EmployeeRole ER
                   INNER JOIN dbo.ORG_Role R ON R.RoleId = ER.Ref_RoleId
                   WHERE ER.Ref_EmployeeId = @EmployeeId
                     AND ER.IsActive = 1
                     AND R.IsActive = 1
                     AND UPPER(LTRIM(RTRIM(R.RoleCode))) IN ('SUPERADMIN', 'ADMIN', 'HRMANAGER')
               ) THEN 1 ELSE 0 END AS BIT) AS CanView;
        RETURN;
    END

    IF @Status = 'GET_DAY_SUMMARY'
    BEGIN
        SET @Date = ISNULL(@Date, CAST(SYSDATETIME() AS DATE));

        ;WITH FirstLogin AS (
            SELECT Ref_EmployeeId, MIN(LoginTime) AS FirstLoginTime, MAX(LoginTime) AS LastLoginTime, COUNT(*) AS LoginCount
            FROM dbo.ORG_EmployeeLoginLog
            WHERE LoginDate = @Date
            GROUP BY Ref_EmployeeId
        )
        SELECT E.EmployeeId,
               E.EmployeeCode,
               LTRIM(RTRIM(ISNULL(E.FirstName, '') + ' ' + ISNULL(E.LastName, ''))) AS EmployeeName,
               D.DesignationName,
               F.FirstLoginTime,
               F.LastLoginTime,
               F.LoginCount
        INTO #Day
        FROM dbo.ORG_Employee E
        LEFT JOIN dbo.ORG_Designation D ON D.DesignationId = E.Ref_DesignationId
        LEFT JOIN FirstLogin F ON F.Ref_EmployeeId = E.EmployeeId
        WHERE E.IsActive = 1;

        -- T0: summary
        SELECT @Date AS [Date],
               COUNT(*) AS TotalEmployees,
               SUM(CASE WHEN FirstLoginTime IS NOT NULL THEN 1 ELSE 0 END) AS LoggedIn,
               SUM(CASE WHEN FirstLoginTime IS NULL THEN 1 ELSE 0 END) AS NotLoggedIn
        FROM #Day;

        -- T1: logged in (earliest first)
        SELECT EmployeeId, EmployeeCode, EmployeeName, DesignationName, FirstLoginTime, LastLoginTime, LoginCount
        FROM #Day WHERE FirstLoginTime IS NOT NULL
        ORDER BY FirstLoginTime;

        -- T2: not logged in
        SELECT EmployeeId, EmployeeCode, EmployeeName, DesignationName
        FROM #Day WHERE FirstLoginTime IS NULL
        ORDER BY EmployeeName;

        RETURN;
    END
END
GO
