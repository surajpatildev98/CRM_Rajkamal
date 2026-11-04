/* =========================================================
   ATTENDANCE REGULARIZATION
   Employee requests a correction for a missed / wrong punch,
   the reporting manager (or reporting HR manager) approves it.
   On approval the punches are written with PunchSource
   'REGULARIZATION' and the day is recalculated the same way
   USP_ORG_Attendance MANUAL_PUNCH does.

   Safe to run more than once.
   ========================================================= */

IF OBJECT_ID('dbo.ORG_AttendanceRegularization', 'U') IS NULL
BEGIN

    CREATE TABLE dbo.ORG_AttendanceRegularization
    (
        RegularizationId    INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_ORG_AttendanceRegularization PRIMARY KEY,

        RequestNo           VARCHAR(30)    NULL,

        Ref_EmployeeId      INT            NOT NULL
            CONSTRAINT FK_ORG_AttReg_Employee REFERENCES dbo.ORG_Employee (EmployeeId),

        AttendanceDate      DATE           NOT NULL,

        /* MISSED_IN | MISSED_OUT | MISSED_BOTH | WRONG_PUNCH */
        RegularizationType  VARCHAR(20)    NOT NULL
            CONSTRAINT CK_ORG_AttReg_Type CHECK (RegularizationType IN ('MISSED_IN', 'MISSED_OUT', 'MISSED_BOTH', 'WRONG_PUNCH')),

        /* snapshot of what was recorded when the request was raised */
        CurrentInTime       DATETIME       NULL,
        CurrentOutTime      DATETIME       NULL,

        RequestedInTime     DATETIME       NULL,
        RequestedOutTime    DATETIME       NULL,

        Reason              NVARCHAR(500)  NOT NULL,

        /* PENDING | APPROVED | REJECTED | CANCELLED */
        RequestStatus       VARCHAR(20)    NOT NULL
            CONSTRAINT DF_ORG_AttReg_Status DEFAULT ('PENDING')
            CONSTRAINT CK_ORG_AttReg_Status CHECK (RequestStatus IN ('PENDING', 'APPROVED', 'REJECTED', 'CANCELLED')),

        AppliedOn           DATETIME       NOT NULL
            CONSTRAINT DF_ORG_AttReg_AppliedOn DEFAULT (GETDATE()),

        ReviewedBy          INT            NULL,
        ReviewedOn          DATETIME       NULL,
        ReviewerRemarks     NVARCHAR(500)  NULL,

        CancelReason        NVARCHAR(500)  NULL,
        CancelledOn         DATETIME       NULL,

        /* attendance row the approval wrote to */
        Ref_AttendanceId    INT            NULL,

        CreatedOn           DATETIME       NOT NULL
            CONSTRAINT DF_ORG_AttReg_CreatedOn DEFAULT (GETDATE()),
        CreatedBy           INT            NULL,
        UpdatedOn           DATETIME       NULL,
        UpdatedBy           INT            NULL
    );

    /* only one open request per employee per day */
    CREATE UNIQUE INDEX UX_ORG_AttReg_OnePending
        ON dbo.ORG_AttendanceRegularization (Ref_EmployeeId, AttendanceDate)
        WHERE RequestStatus = 'PENDING';

    CREATE INDEX IX_ORG_AttReg_Employee
        ON dbo.ORG_AttendanceRegularization (Ref_EmployeeId, AttendanceDate DESC);

END;
GO


CREATE OR ALTER PROCEDURE dbo.USP_ORG_AttendanceRegularization
(
    @Status             VARCHAR(50),

    @LoggedInEmployeeId INT = NULL,
    @RegularizationId   INT = NULL,

    @AttendanceDate     DATE = NULL,
    @RegularizationType VARCHAR(20) = NULL,
    @RequestedInTime    DATETIME = NULL,
    @RequestedOutTime   DATETIME = NULL,

    @Reason             NVARCHAR(500) = NULL,
    @Remarks            NVARCHAR(500) = NULL
)
AS
BEGIN

    SET NOCOUNT ON;

    DECLARE
        @Now   DATETIME = GETDATE(),
        @Today DATE     = CAST(GETDATE() AS DATE);

    SET @Reason  = NULLIF(LTRIM(RTRIM(@Reason)), '');
    SET @Remarks = NULLIF(LTRIM(RTRIM(@Remarks)), '');

    IF @LoggedInEmployeeId IS NULL
       OR NOT EXISTS (SELECT 1 FROM ORG_Employee WHERE EmployeeId = @LoggedInEmployeeId)
    BEGIN
        SELECT CAST(0 AS BIT) AS Status, 'Employee not found.' AS Message;
        RETURN;
    END;


    /* =========================================================
       1. LISTS
          GET_MY   : the employee's own requests
          GET_TEAM : requests of people who report to him
                     (reporting manager or reporting HR manager)
       ========================================================= */

    IF (@Status IN ('GET_MY', 'GET_TEAM'))
    BEGIN

        SELECT
            R.RegularizationId,
            R.RequestNo,
            E.EmployeeId,
            E.EmployeeCode,
            LTRIM(RTRIM(
                ISNULL(E.FirstName, '') +
                CASE WHEN ISNULL(E.MiddleName, '') <> '' THEN ' ' + E.MiddleName ELSE '' END +
                CASE WHEN ISNULL(E.LastName, '')   <> '' THEN ' ' + E.LastName   ELSE '' END
            )) AS EmployeeName,
            R.AttendanceDate,
            R.RegularizationType,
            R.CurrentInTime,
            R.CurrentOutTime,
            R.RequestedInTime,
            R.RequestedOutTime,
            R.Reason,
            R.RequestStatus,
            R.AppliedOn,
            R.ReviewedBy,
            LTRIM(RTRIM(
                ISNULL(RV.FirstName, '') +
                CASE WHEN ISNULL(RV.LastName, '') <> '' THEN ' ' + RV.LastName ELSE '' END
            )) AS ReviewedByName,
            R.ReviewedOn,
            /* a cancelled request shows the employee's cancel reason */
            CASE
                WHEN R.RequestStatus = 'CANCELLED' THEN R.CancelReason
                ELSE R.ReviewerRemarks
            END AS ReviewerRemarks
        FROM ORG_AttendanceRegularization R
        INNER JOIN ORG_Employee E
            ON E.EmployeeId = R.Ref_EmployeeId
        LEFT JOIN ORG_Employee RV
            ON RV.EmployeeId = R.ReviewedBy
        WHERE
            (
                @Status = 'GET_MY'
                AND R.Ref_EmployeeId = @LoggedInEmployeeId
            )
            OR
            (
                @Status = 'GET_TEAM'
                AND R.Ref_EmployeeId <> @LoggedInEmployeeId
                AND (
                    E.Ref_ReportingManagerId = @LoggedInEmployeeId
                    OR E.Ref_ReportingHRManagerId = @LoggedInEmployeeId
                )
            )
        ORDER BY
            CASE WHEN R.RequestStatus = 'PENDING' THEN 0 ELSE 1 END,
            R.AttendanceDate DESC,
            R.RegularizationId DESC;

        RETURN;

    END


    /* =========================================================
       2. APPLY
       ========================================================= */

    ELSE IF (@Status = 'APPLY')
    BEGIN

        DECLARE
            @CurrentIn       DATETIME = NULL,
            @CurrentOut      DATETIME = NULL,
            @EffectiveIn     DATETIME = NULL,
            @EffectiveOut    DATETIME = NULL,
            @NewId           INT      = NULL,
            @NewRequestNo    VARCHAR(30);

        SET @RegularizationType = UPPER(LTRIM(RTRIM(@RegularizationType)));

        IF NOT EXISTS (SELECT 1 FROM ORG_Employee WHERE EmployeeId = @LoggedInEmployeeId AND IsActive = 1)
        BEGIN
            SELECT CAST(0 AS BIT) AS Status, 'Employee is inactive.' AS Message;
            RETURN;
        END;

        IF @AttendanceDate IS NULL
        BEGIN
            SELECT CAST(0 AS BIT) AS Status, 'Attendance date is required.' AS Message;
            RETURN;
        END;

        IF @AttendanceDate > @Today
        BEGIN
            SELECT CAST(0 AS BIT) AS Status, 'Attendance cannot be regularized for a future date.' AS Message;
            RETURN;
        END;

        IF @AttendanceDate < DATEADD(DAY, -90, @Today)
        BEGIN
            SELECT CAST(0 AS BIT) AS Status, 'Regularization is allowed only for the last 90 days.' AS Message;
            RETURN;
        END;

        IF @RegularizationType IS NULL
           OR @RegularizationType NOT IN ('MISSED_IN', 'MISSED_OUT', 'MISSED_BOTH', 'WRONG_PUNCH')
        BEGIN
            SELECT CAST(0 AS BIT) AS Status, 'Invalid regularization type.' AS Message;
            RETURN;
        END;

        IF @Reason IS NULL
        BEGIN
            SELECT CAST(0 AS BIT) AS Status, 'Reason is required.' AS Message;
            RETURN;
        END;

        /* only keep the times this type is about */
        IF @RegularizationType = 'MISSED_IN'  SET @RequestedOutTime = NULL;
        IF @RegularizationType = 'MISSED_OUT' SET @RequestedInTime  = NULL;

        IF @RegularizationType IN ('MISSED_IN', 'MISSED_BOTH') AND @RequestedInTime IS NULL
        BEGIN
            SELECT CAST(0 AS BIT) AS Status, 'Punch in time is required.' AS Message;
            RETURN;
        END;

        IF @RegularizationType IN ('MISSED_OUT', 'MISSED_BOTH') AND @RequestedOutTime IS NULL
        BEGIN
            SELECT CAST(0 AS BIT) AS Status, 'Punch out time is required.' AS Message;
            RETURN;
        END;

        IF @RegularizationType = 'WRONG_PUNCH' AND @RequestedInTime IS NULL AND @RequestedOutTime IS NULL
        BEGIN
            SELECT CAST(0 AS BIT) AS Status, 'Corrected punch time is required.' AS Message;
            RETURN;
        END;

        IF @RequestedInTime IS NOT NULL
           AND CAST(@RequestedInTime AS DATE) <> @AttendanceDate
        BEGIN
            SELECT CAST(0 AS BIT) AS Status, 'Punch in time must be on the attendance date.' AS Message;
            RETURN;
        END;

        /* night shift: out may fall on the next day */
        IF @RequestedOutTime IS NOT NULL
           AND (
                CAST(@RequestedOutTime AS DATE) < @AttendanceDate
                OR CAST(@RequestedOutTime AS DATE) > DATEADD(DAY, 1, @AttendanceDate)
           )
        BEGIN
            SELECT CAST(0 AS BIT) AS Status, 'Punch out time must be on the attendance date or the next day.' AS Message;
            RETURN;
        END;

        IF @RequestedInTime > @Now OR @RequestedOutTime > @Now
        BEGIN
            SELECT CAST(0 AS BIT) AS Status, 'Punch time cannot be in the future.' AS Message;
            RETURN;
        END;

        /* what is recorded today */
        SELECT TOP 1
            @CurrentIn  = FirstInTime,
            @CurrentOut = LastOutTime
        FROM ORG_Attendance
        WHERE Ref_EmployeeId = @LoggedInEmployeeId
          AND AttendanceDate = @AttendanceDate
        ORDER BY AttendanceId DESC;

        IF @RegularizationType = 'MISSED_IN' AND @CurrentIn IS NOT NULL
        BEGIN
            SELECT CAST(0 AS BIT) AS Status, 'Punch in is already recorded for this date. Use Wrong Punch Time instead.' AS Message;
            RETURN;
        END;

        IF @RegularizationType = 'MISSED_OUT' AND @CurrentOut IS NOT NULL
        BEGIN
            SELECT CAST(0 AS BIT) AS Status, 'Punch out is already recorded for this date. Use Wrong Punch Time instead.' AS Message;
            RETURN;
        END;

        IF @RegularizationType = 'MISSED_BOTH' AND (@CurrentIn IS NOT NULL OR @CurrentOut IS NOT NULL)
        BEGIN
            SELECT CAST(0 AS BIT) AS Status, 'Punches are already recorded for this date. Use Missed Punch In / Out or Wrong Punch Time instead.' AS Message;
            RETURN;
        END;

        IF @RegularizationType = 'WRONG_PUNCH'
        BEGIN

            IF @RequestedInTime IS NOT NULL AND @CurrentIn IS NULL
            BEGIN
                SELECT CAST(0 AS BIT) AS Status, 'Punch in is not recorded for this date. Use Missed Punch In instead.' AS Message;
                RETURN;
            END;

            IF @RequestedOutTime IS NOT NULL AND @CurrentOut IS NULL
            BEGIN
                SELECT CAST(0 AS BIT) AS Status, 'Punch out is not recorded for this date. Use Missed Punch Out instead.' AS Message;
                RETURN;
            END;

            /* the app sends minutes only, so compare to the minute */
            IF (@RequestedInTime  IS NULL OR DATEDIFF(MINUTE, @CurrentIn,  @RequestedInTime)  = 0)
               AND (@RequestedOutTime IS NULL OR DATEDIFF(MINUTE, @CurrentOut, @RequestedOutTime) = 0)
            BEGIN
                SELECT CAST(0 AS BIT) AS Status, 'The corrected time is the same as the recorded time.' AS Message;
                RETURN;
            END;

        END;

        SET @EffectiveIn  = ISNULL(@RequestedInTime,  @CurrentIn);
        SET @EffectiveOut = ISNULL(@RequestedOutTime, @CurrentOut);

        IF @EffectiveIn IS NOT NULL
           AND @EffectiveOut IS NOT NULL
           AND @EffectiveOut <= @EffectiveIn
        BEGIN
            SELECT CAST(0 AS BIT) AS Status, 'Punch out must be after punch in.' AS Message;
            RETURN;
        END;

        IF EXISTS
        (
            SELECT 1
            FROM ORG_AttendanceRegularization
            WHERE Ref_EmployeeId = @LoggedInEmployeeId
              AND AttendanceDate = @AttendanceDate
              AND RequestStatus = 'PENDING'
        )
        BEGIN
            SELECT CAST(0 AS BIT) AS Status, 'A regularization request is already pending for this date.' AS Message;
            RETURN;
        END;

        IF NOT EXISTS
        (
            SELECT 1
            FROM ORG_Employee
            WHERE EmployeeId = @LoggedInEmployeeId
              AND (Ref_ReportingManagerId IS NOT NULL OR Ref_ReportingHRManagerId IS NOT NULL)
        )
        BEGIN
            SELECT CAST(0 AS BIT) AS Status, 'Reporting manager is not assigned. Please contact HR.' AS Message;
            RETURN;
        END;

        BEGIN TRY

            INSERT INTO ORG_AttendanceRegularization
            (
                Ref_EmployeeId,
                AttendanceDate,
                RegularizationType,
                CurrentInTime,
                CurrentOutTime,
                RequestedInTime,
                RequestedOutTime,
                Reason,
                RequestStatus,
                AppliedOn,
                CreatedOn,
                CreatedBy
            )
            VALUES
            (
                @LoggedInEmployeeId,
                @AttendanceDate,
                @RegularizationType,
                @CurrentIn,
                @CurrentOut,
                @RequestedInTime,
                @RequestedOutTime,
                @Reason,
                'PENDING',
                @Now,
                @Now,
                @LoggedInEmployeeId
            );

            SET @NewId = SCOPE_IDENTITY();
            SET @NewRequestNo = 'REG' + RIGHT('000000' + CAST(@NewId AS VARCHAR(10)), 6);

            UPDATE ORG_AttendanceRegularization
            SET RequestNo = @NewRequestNo
            WHERE RegularizationId = @NewId;

        END TRY
        BEGIN CATCH

            /* 2601 = the one-pending-per-day index (double tap) */
            SELECT
                CAST(0 AS BIT) AS Status,
                CASE
                    WHEN ERROR_NUMBER() IN (2601, 2627)
                        THEN 'A regularization request is already pending for this date.'
                    ELSE ERROR_MESSAGE()
                END AS Message;
            RETURN;

        END CATCH;

        SELECT
            CAST(1 AS BIT) AS Status,
            'Regularization request submitted successfully.' AS Message,
            @NewId AS RegularizationId,
            @NewRequestNo AS RequestNo;

        RETURN;

    END


    /* =========================================================
       3. CANCEL (by the employee, while pending)
       ========================================================= */

    ELSE IF (@Status = 'CANCEL')
    BEGIN

        IF @Reason IS NULL
        BEGIN
            SELECT CAST(0 AS BIT) AS Status, 'Cancellation reason is required.' AS Message;
            RETURN;
        END;

        IF NOT EXISTS
        (
            SELECT 1
            FROM ORG_AttendanceRegularization
            WHERE RegularizationId = @RegularizationId
              AND Ref_EmployeeId = @LoggedInEmployeeId
        )
        BEGIN
            SELECT CAST(0 AS BIT) AS Status, 'Regularization request not found.' AS Message;
            RETURN;
        END;

        UPDATE ORG_AttendanceRegularization
        SET
            RequestStatus = 'CANCELLED',
            CancelReason  = @Reason,
            CancelledOn   = @Now,
            UpdatedOn     = @Now,
            UpdatedBy     = @LoggedInEmployeeId
        WHERE RegularizationId = @RegularizationId
          AND RequestStatus = 'PENDING';

        IF @@ROWCOUNT = 0
        BEGIN
            SELECT CAST(0 AS BIT) AS Status, 'Only a pending request can be cancelled.' AS Message;
            RETURN;
        END;

        SELECT CAST(1 AS BIT) AS Status, 'Regularization request cancelled.' AS Message;
        RETURN;

    END


    /* =========================================================
       4. APPROVE / REJECT (by the reporting manager / HR manager)
       ========================================================= */

    ELSE IF (@Status IN ('APPROVE', 'REJECT'))
    BEGIN

        DECLARE
            @EmployeeId       INT          = NULL,
            @ReqDate          DATE         = NULL,
            @ReqIn            DATETIME     = NULL,
            @ReqOut           DATETIME     = NULL,
            @ReqStatus        VARCHAR(20)  = NULL,
            @RequestNo        VARCHAR(30)  = NULL,
            @ManagerId        INT          = NULL,
            @HRManagerId      INT          = NULL;

        SELECT
            @EmployeeId  = R.Ref_EmployeeId,
            @ReqDate     = R.AttendanceDate,
            @ReqIn       = R.RequestedInTime,
            @ReqOut      = R.RequestedOutTime,
            @ReqStatus   = R.RequestStatus,
            @RequestNo   = R.RequestNo,
            @ManagerId   = E.Ref_ReportingManagerId,
            @HRManagerId = E.Ref_ReportingHRManagerId
        FROM ORG_AttendanceRegularization R
        INNER JOIN ORG_Employee E
            ON E.EmployeeId = R.Ref_EmployeeId
        WHERE R.RegularizationId = @RegularizationId;

        IF @EmployeeId IS NULL
        BEGIN
            SELECT CAST(0 AS BIT) AS Status, 'Regularization request not found.' AS Message;
            RETURN;
        END;

        IF @ReqStatus <> 'PENDING'
        BEGIN
            SELECT CAST(0 AS BIT) AS Status, 'This request is already ' + LOWER(@ReqStatus) + '.' AS Message;
            RETURN;
        END;

        IF @EmployeeId = @LoggedInEmployeeId
        BEGIN
            SELECT CAST(0 AS BIT) AS Status, 'You cannot review your own request.' AS Message;
            RETURN;
        END;

        IF ISNULL(@ManagerId, 0) <> @LoggedInEmployeeId
           AND ISNULL(@HRManagerId, 0) <> @LoggedInEmployeeId
        BEGIN
            SELECT CAST(0 AS BIT) AS Status, 'You are not authorised to review this request.' AS Message;
            RETURN;
        END;


        /* ---------- REJECT ---------- */

        IF @Status = 'REJECT'
        BEGIN

            IF @Reason IS NULL
            BEGIN
                SELECT CAST(0 AS BIT) AS Status, 'Rejection reason is required.' AS Message;
                RETURN;
            END;

            UPDATE ORG_AttendanceRegularization
            SET
                RequestStatus   = 'REJECTED',
                ReviewedBy      = @LoggedInEmployeeId,
                ReviewedOn      = @Now,
                ReviewerRemarks = @Reason,
                UpdatedOn       = @Now,
                UpdatedBy       = @LoggedInEmployeeId
            WHERE RegularizationId = @RegularizationId
              AND RequestStatus = 'PENDING';

            IF @@ROWCOUNT = 0
            BEGIN
                SELECT CAST(0 AS BIT) AS Status, 'This request is no longer pending.' AS Message;
                RETURN;
            END;

            SELECT CAST(1 AS BIT) AS Status, 'Regularization request rejected.' AS Message;
            RETURN;

        END;


        /* ---------- APPROVE ---------- */

        DECLARE
            @AttendanceId        INT          = NULL,
            @PresentStatusId     INT          = NULL,
            @PunchId             INT          = NULL,
            @LastPunchType       CHAR(1)      = NULL,

            @ShiftId             INT          = NULL,
            @ShiftStartTime      TIME         = NULL,
            @ShiftEndTime        TIME         = NULL,
            @GraceInMinutes      INT          = 0,
            @GraceOutMinutes     INT          = 0,
            @IsNightShift        BIT          = 0,

            @FirstInTime         DATETIME     = NULL,
            @LastOutTime         DATETIME     = NULL,
            @LateMinutes         INT          = 0,
            @EarlyExitMinutes    INT          = 0,
            @WorkedMinutes       INT          = 0,
            @TotalMinutes        INT          = 0,
            @WorkingHours        DECIMAL(10,2) = 0,
            @BreakHoursCalc      DECIMAL(10,2) = 0,
            @ShiftStartDateTime  DATETIME     = NULL,
            @ShiftEndDateTime    DATETIME     = NULL,
            @PunchRemarks        NVARCHAR(500);

        SELECT TOP 1 @PresentStatusId = AttendanceStatusId
        FROM ORG_AttendanceStatus
        WHERE StatusCode = 'P' AND IsActive = 1;

        IF @PresentStatusId IS NULL
        BEGIN
            SELECT CAST(0 AS BIT) AS Status, 'Present attendance status is not configured.' AS Message;
            RETURN;
        END;

        /* shift that was effective on that day (not required) */
        SELECT TOP 1
            @ShiftId         = ES.Ref_ShiftId,
            @ShiftStartTime  = S.StartTime,
            @ShiftEndTime    = S.EndTime,
            @GraceInMinutes  = ISNULL(S.GraceInMinutes, 0),
            @GraceOutMinutes = ISNULL(S.GraceOutMinutes, 0),
            @IsNightShift    = ISNULL(S.IsNightShift, 0)
        FROM ORG_Employee_Shift ES
        INNER JOIN ORG_Shift S
            ON S.ShiftId = ES.Ref_ShiftId
        WHERE ES.Ref_EmployeeId = @EmployeeId
          AND ES.EffectiveFrom <= @ReqDate
          AND (ES.EffectiveTo IS NULL OR ES.EffectiveTo >= @ReqDate)
        ORDER BY
            ES.IsActive DESC,
            ES.EffectiveFrom DESC,
            ES.EmployeeShiftId DESC;

        SET @PunchRemarks = 'Regularized (' + ISNULL(@RequestNo, CAST(@RegularizationId AS VARCHAR(10))) + ')';

        BEGIN TRY

            BEGIN TRANSACTION;

            /* claim the request first so two approvers cannot both apply it */
            UPDATE ORG_AttendanceRegularization
            SET
                RequestStatus   = 'APPROVED',
                ReviewedBy      = @LoggedInEmployeeId,
                ReviewedOn      = @Now,
                ReviewerRemarks = @Remarks,
                UpdatedOn       = @Now,
                UpdatedBy       = @LoggedInEmployeeId
            WHERE RegularizationId = @RegularizationId
              AND RequestStatus = 'PENDING';

            IF @@ROWCOUNT = 0
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT CAST(0 AS BIT) AS Status, 'This request is no longer pending.' AS Message;
                RETURN;
            END;

            /* ---------- attendance row for the day ---------- */

            SELECT TOP 1 @AttendanceId = AttendanceId
            FROM ORG_Attendance WITH (UPDLOCK, HOLDLOCK)
            WHERE Ref_EmployeeId = @EmployeeId
              AND AttendanceDate = @ReqDate
            ORDER BY AttendanceId DESC;

            IF @AttendanceId IS NULL
            BEGIN

                INSERT INTO ORG_Attendance
                (
                    Ref_EmployeeId,
                    AttendanceDate,
                    Ref_AttendanceStatusId,
                    Ref_ShiftId,
                    Remarks,
                    CreatedOn,
                    CreatedBy
                )
                VALUES
                (
                    @EmployeeId,
                    @ReqDate,
                    @PresentStatusId,
                    @ShiftId,
                    @PunchRemarks,
                    @Now,
                    @LoggedInEmployeeId
                );

                SET @AttendanceId = SCOPE_IDENTITY();

            END;

            /* ---------- IN : correct the first IN, or add it ---------- */

            IF @ReqIn IS NOT NULL
            BEGIN

                SET @PunchId = NULL;

                SELECT TOP 1 @PunchId = AttendancePunchId
                FROM ORG_AttendancePunch
                WHERE Ref_AttendanceId = @AttendanceId
                  AND PunchType = 'I'
                ORDER BY PunchDateTime, AttendancePunchId;

                IF @PunchId IS NOT NULL
                BEGIN

                    UPDATE ORG_AttendancePunch
                    SET
                        Remarks       = LEFT(@PunchRemarks + ' was ' + CONVERT(VARCHAR(5), PunchDateTime, 108), 500),
                        PunchDateTime = @ReqIn,
                        PunchSource   = 'REGULARIZATION'
                    WHERE AttendancePunchId = @PunchId;

                END
                ELSE
                BEGIN

                    INSERT INTO ORG_AttendancePunch
                    (
                        Ref_AttendanceId,
                        Ref_EmployeeId,
                        PunchDateTime,
                        PunchType,
                        PunchSource,
                        Remarks,
                        CreatedOn,
                        CreatedBy
                    )
                    VALUES
                    (
                        @AttendanceId,
                        @EmployeeId,
                        @ReqIn,
                        'I',
                        'REGULARIZATION',
                        @PunchRemarks,
                        @Now,
                        @LoggedInEmployeeId
                    );

                END;

            END;

            /* ---------- OUT : correct the last OUT, or add one ---------- */

            IF @ReqOut IS NOT NULL
            BEGIN

                SET @PunchId = NULL;
                SET @LastPunchType = NULL;

                SELECT TOP 1
                    @PunchId       = AttendancePunchId,
                    @LastPunchType = PunchType
                FROM ORG_AttendancePunch
                WHERE Ref_AttendanceId = @AttendanceId
                ORDER BY PunchDateTime DESC, AttendancePunchId DESC;

                IF @LastPunchType = 'O'
                BEGIN

                    UPDATE ORG_AttendancePunch
                    SET
                        Remarks       = LEFT(@PunchRemarks + ' was ' + CONVERT(VARCHAR(5), PunchDateTime, 108), 500),
                        PunchDateTime = @ReqOut,
                        PunchSource   = 'REGULARIZATION'
                    WHERE AttendancePunchId = @PunchId;

                END
                ELSE
                BEGIN

                    INSERT INTO ORG_AttendancePunch
                    (
                        Ref_AttendanceId,
                        Ref_EmployeeId,
                        PunchDateTime,
                        PunchType,
                        PunchSource,
                        Remarks,
                        CreatedOn,
                        CreatedBy
                    )
                    VALUES
                    (
                        @AttendanceId,
                        @EmployeeId,
                        @ReqOut,
                        'O',
                        'REGULARIZATION',
                        @PunchRemarks,
                        @Now,
                        @LoggedInEmployeeId
                    );

                END;

            END;

            /* ---------- punches must still go IN, OUT, IN, OUT ... ---------- */

            IF EXISTS
            (
                SELECT 1
                FROM
                (
                    SELECT
                        PunchType,
                        LAG(PunchType) OVER (ORDER BY PunchDateTime, AttendancePunchId) AS PrevType,
                        ROW_NUMBER()   OVER (ORDER BY PunchDateTime, AttendancePunchId) AS Rn
                    FROM ORG_AttendancePunch
                    WHERE Ref_AttendanceId = @AttendanceId
                ) P
                WHERE (P.Rn = 1 AND P.PunchType <> 'I')
                   OR P.PunchType = P.PrevType
            )
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT CAST(0 AS BIT) AS Status,
                       'The requested time overlaps other punches recorded for this day. Please reject it and ask the employee to correct the time.' AS Message;
                RETURN;
            END;

            /* ---------- recalculate the day (same rules as MANUAL_PUNCH) ---------- */

            SELECT
                @FirstInTime = MIN(CASE WHEN PunchType = 'I' THEN PunchDateTime END),
                @LastOutTime = MAX(CASE WHEN PunchType = 'O' THEN PunchDateTime END)
            FROM ORG_AttendancePunch
            WHERE Ref_AttendanceId = @AttendanceId;

            /* late : first IN after shift start + grace */
            IF @ShiftStartTime IS NOT NULL AND @FirstInTime IS NOT NULL
            BEGIN
                SET @ShiftStartDateTime =
                    DATEADD(MINUTE, @GraceInMinutes,
                        DATEADD(DAY, DATEDIFF(DAY, 0, @ReqDate), CAST(@ShiftStartTime AS DATETIME)));

                IF @FirstInTime > @ShiftStartDateTime
                    SET @LateMinutes = DATEDIFF(MINUTE, @ShiftStartDateTime, @FirstInTime);
            END;

            /* early exit : last OUT before shift end - grace */
            IF @ShiftEndTime IS NOT NULL AND @LastOutTime IS NOT NULL
            BEGIN
                SET @ShiftEndDateTime =
                    DATEADD(DAY, DATEDIFF(DAY, 0, @ReqDate), CAST(@ShiftEndTime AS DATETIME));

                IF @IsNightShift = 1
                   AND @ShiftStartTime IS NOT NULL
                   AND @ShiftEndTime <= @ShiftStartTime
                    SET @ShiftEndDateTime = DATEADD(DAY, 1, @ShiftEndDateTime);

                SET @ShiftEndDateTime = DATEADD(MINUTE, -@GraceOutMinutes, @ShiftEndDateTime);

                IF @LastOutTime < @ShiftEndDateTime
                    SET @EarlyExitMinutes = DATEDIFF(MINUTE, @LastOutTime, @ShiftEndDateTime);
            END;

            /* working = sum of IN -> OUT segments, break = gaps between them */
            ;WITH PunchSeq AS
            (
                SELECT
                    PunchType,
                    PunchDateTime,
                    LEAD(PunchType)     OVER (ORDER BY PunchDateTime, AttendancePunchId) AS NextType,
                    LEAD(PunchDateTime) OVER (ORDER BY PunchDateTime, AttendancePunchId) AS NextTime
                FROM ORG_AttendancePunch
                WHERE Ref_AttendanceId = @AttendanceId
            )
            SELECT @WorkedMinutes = ISNULL(SUM(DATEDIFF(MINUTE, PunchDateTime, NextTime)), 0)
            FROM PunchSeq
            WHERE PunchType = 'I'
              AND NextType = 'O';

            IF @FirstInTime IS NOT NULL AND @LastOutTime IS NOT NULL
                SET @TotalMinutes = DATEDIFF(MINUTE, @FirstInTime, @LastOutTime);

            SET @WorkingHours = CAST(@WorkedMinutes / 60.0 AS DECIMAL(10,2));

            SET @BreakHoursCalc =
                CAST(
                    CASE
                        WHEN @TotalMinutes > @WorkedMinutes THEN (@TotalMinutes - @WorkedMinutes) / 60.0
                        ELSE 0
                    END
                AS DECIMAL(10,2));

            UPDATE A
            SET
                FirstInTime      = @FirstInTime,
                LastOutTime      = @LastOutTime,
                LateMinutes      = @LateMinutes,
                EarlyExitMinutes = @EarlyExitMinutes,
                WorkingHours     = @WorkingHours,
                BreakHours       = @BreakHoursCalc,
                Ref_ShiftId      = ISNULL(A.Ref_ShiftId, @ShiftId),
                /* an absent day becomes present; leave / holiday / half day stay as they are */
                Ref_AttendanceStatusId =
                    CASE
                        WHEN AST.StatusCode IS NULL OR AST.StatusCode = 'A' THEN @PresentStatusId
                        ELSE A.Ref_AttendanceStatusId
                    END,
                UpdatedOn        = @Now,
                UpdatedBy        = @LoggedInEmployeeId
            FROM ORG_Attendance A
            LEFT JOIN ORG_AttendanceStatus AST
                ON AST.AttendanceStatusId = A.Ref_AttendanceStatusId
            WHERE A.AttendanceId = @AttendanceId;

            UPDATE ORG_AttendanceRegularization
            SET Ref_AttendanceId = @AttendanceId
            WHERE RegularizationId = @RegularizationId;

            COMMIT TRANSACTION;

        END TRY
        BEGIN CATCH

            IF @@TRANCOUNT > 0
                ROLLBACK TRANSACTION;

            SELECT CAST(0 AS BIT) AS Status, ERROR_MESSAGE() AS Message;
            RETURN;

        END CATCH;

        SELECT
            CAST(1 AS BIT) AS Status,
            'Regularization approved and attendance updated.' AS Message,
            @AttendanceId AS AttendanceId;

        RETURN;

    END


    ELSE
    BEGIN
        SELECT CAST(0 AS BIT) AS Status, 'Invalid regularization status.' AS Message;
        RETURN;
    END

END
GO
