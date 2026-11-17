/* =====================================================================
   AMC module - USP_AMC_ContractManagement + permissions (UAT)
   Needs AMC_Tables_01_UAT.sql. Rollback: AMC_SP_98_Rollback.sql
   Access (permission codes) is checked in the controller, not here.
   ===================================================================== */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.USP_AMC_ContractManagement
    @Status             VARCHAR(30),
    @EmployeeId         INT,
    @AmcContractId      INT            = NULL,
    -- list filters
    @Search             NVARCHAR(100)  = NULL,
    @Filter             VARCHAR(20)    = NULL,   -- ALL | ACTIVE | EXPIRING | EXPIRED | RENEWED | CANCELLED
    @AmcTypeFilter      VARCHAR(10)    = NULL,
    -- contract fields
    @AmcType            VARCHAR(10)    = NULL,
    @CoverageType       VARCHAR(20)    = NULL,
    @CustomerSource     VARCHAR(3)     = NULL,
    @SourceCustomerId   INT            = NULL,
    @Ref_CustomerId     INT            = NULL,
    @CustomerName       NVARCHAR(255)  = NULL,
    @ContactMobile      VARCHAR(20)    = NULL,
    @ContactEmail       NVARCHAR(150)  = NULL,
    @DurationMonths     INT            = NULL,
    @StartDate          DATE           = NULL,
    @TotalVisits        INT            = NULL,
    @InvoiceNo          NVARCHAR(50)   = NULL,
    @InvoiceDate        DATE           = NULL,
    @InvoiceFilePath    NVARCHAR(500)  = NULL,
    @InvoiceFileName    NVARCHAR(255)  = NULL,
    @Amount             DECIMAL(18,2)  = NULL,
    @GstPercent         DECIMAL(5,2)   = NULL,
    @PaymentStatus      VARCHAR(10)    = NULL,
    @PaymentMode        VARCHAR(30)    = NULL,
    @PaymentDate        DATE           = NULL,
    @PaymentRemark      NVARCHAR(500)  = NULL,
    @Remarks            NVARCHAR(1000) = NULL,
    @ItemsJson          NVARCHAR(MAX)  = NULL,   -- [{categoryId,categoryName,offeringId,offeringName,productId,productName,serialNo,licenseKey,softwareVersion,noOfUsers,remark}]
    @CoverageIds        VARCHAR(500)   = NULL,   -- '1,3,4'
    @Reason             NVARCHAR(500)  = NULL,
    -- coverage check / ticket link
    @SerialNo           NVARCHAR(100)  = NULL,
    @ProductId          BIGINT         = NULL,
    @TicketId           INT            = NULL,
    -- coverage master
    @CoverageServiceId  INT            = NULL,
    @ServiceName        NVARCHAR(100)  = NULL,
    @IsActive           BIT            = NULL,
    @ReminderDays       INT            = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Today DATE = CAST(GETDATE() AS DATE);

    /* ---------- MASTERS: duration dropdown + coverage chips ---------- */
    IF (@Status = 'MASTERS')
    BEGIN
        SELECT DurationMonths, DisplayName FROM dbo.MST_AmcDuration WHERE IsActive = 1 ORDER BY SortOrder;
        SELECT CoverageServiceId, ServiceName, AmcType FROM dbo.MST_AmcCoverageService WHERE IsActive = 1 ORDER BY SortOrder, ServiceName;
        RETURN;
    END

    /* ---------- Expiry reminders (scheduler entity AMC_EXPIRY, runs daily) ----------
       Reminder points 30/15/10/5/1 days. Each contract gets the smallest point >= days left that was
       not sent yet, so a missed day is caught up next run and nothing is sent twice. */
    IF (@Status = 'GET_DUE_REMINDERS')
    BEGIN
        SELECT c.AmcContractId, c.AmcNo, c.AmcType, c.CoverageType, c.CustomerName, c.ContactEmail, c.ContactMobile,
               c.StartDate, c.EndDate, c.TotalAmount, c.InvoiceNo, DATEDIFF(DAY, @Today, c.EndDate) AS DaysLeft, r.ReminderDays
        INTO #Due
        FROM dbo.AMC_Contract c
        CROSS APPLY (SELECT MIN(v.d) AS ReminderDays FROM (VALUES (30),(15),(10),(5),(1)) v(d)
                     WHERE v.d >= DATEDIFF(DAY, @Today, c.EndDate)) r
        WHERE c.IsActive = 1 AND c.Status = 'ACTIVE'
          AND c.EndDate >= @Today AND c.EndDate <= DATEADD(DAY, 30, @Today)
          AND NOT EXISTS (SELECT 1 FROM dbo.AMC_ContractLog l
                          WHERE l.Ref_AmcContractId = c.AmcContractId AND l.Action = 'REMINDER_SENT' AND l.ReminderDays = r.ReminderDays);

        SELECT * FROM #Due ORDER BY EndDate, AmcContractId;
        SELECT i.Ref_AmcContractId AS AmcContractId, i.ProductName, i.SerialNo, i.LicenseKey
        FROM dbo.AMC_ContractItem i JOIN #Due d ON d.AmcContractId = i.Ref_AmcContractId
        WHERE i.IsActive = 1 ORDER BY i.Ref_AmcContractId, i.AmcContractItemId;
        RETURN;
    END

    /* Internal CC = active employees allowed AMC_ExpiryReminder (Role Access / User Override). */
    IF (@Status = 'GET_REMINDER_RECIPIENTS')
    BEGIN
        SELECT e.EmployeeId, e.OfficialEmail,
               LTRIM(RTRIM(ISNULL(e.FirstName, '') + ' ' + ISNULL(e.LastName, ''))) AS EmployeeName
        FROM dbo.ORG_Employee e
        CROSS APPLY dbo.FN_ORG_EmployeeEffectivePermission(e.EmployeeId) f
        WHERE e.IsActive = 1 AND f.PermissionCode = 'AMC_ExpiryReminder' AND f.IsAllowed = 1;
        RETURN;
    END

    IF (@Status = 'MARK_REMINDER_SENT')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.AMC_ContractLog WHERE Ref_AmcContractId = @AmcContractId
                       AND Action = 'REMINDER_SENT' AND ReminderDays = @ReminderDays)
            INSERT INTO dbo.AMC_ContractLog (Ref_AmcContractId, Action, ReminderDays, Remark, ActionBy)
            VALUES (@AmcContractId, 'REMINDER_SENT', @ReminderDays, @Reason, NULL);
        SELECT 1 AS Status, 'Reminder logged.' AS Message;
        RETURN;
    END

    /* ---------- Coverage master (admin) ---------- */
    IF (@Status = 'COVERAGE_LIST')
    BEGIN
        SELECT CoverageServiceId, ServiceName, AmcType, SortOrder, IsActive FROM dbo.MST_AmcCoverageService ORDER BY SortOrder, ServiceName;
        RETURN;
    END

    IF (@Status = 'COVERAGE_SAVE')
    BEGIN
        SET @ServiceName = NULLIF(LTRIM(RTRIM(@ServiceName)), '');
        SET @AmcType = UPPER(LTRIM(RTRIM(ISNULL(@AmcType, 'BOTH'))));
        IF (@ServiceName IS NULL) THROW 51030, 'Service name is required.', 1;
        IF (@AmcType NOT IN ('HARDWARE','SOFTWARE','BOTH')) THROW 51031, 'AMC type must be Hardware, Software or Both.', 1;
        IF EXISTS (SELECT 1 FROM dbo.MST_AmcCoverageService WHERE ServiceName = @ServiceName AND AmcType = @AmcType
                   AND CoverageServiceId <> ISNULL(@CoverageServiceId, 0))
            THROW 51032, 'This coverage service already exists.', 1;

        IF (ISNULL(@CoverageServiceId, 0) = 0)
        BEGIN
            INSERT INTO dbo.MST_AmcCoverageService (ServiceName, AmcType, SortOrder, IsActive, CreatedBy)
            VALUES (@ServiceName, @AmcType, (SELECT ISNULL(MAX(SortOrder), 0) + 1 FROM dbo.MST_AmcCoverageService), ISNULL(@IsActive, 1), @EmployeeId);
            SET @CoverageServiceId = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM dbo.MST_AmcCoverageService WHERE CoverageServiceId = @CoverageServiceId)
                THROW 51033, 'Coverage service not found.', 1;
            UPDATE dbo.MST_AmcCoverageService
            SET ServiceName = @ServiceName, AmcType = @AmcType, IsActive = ISNULL(@IsActive, IsActive), UpdatedOn = GETDATE(), UpdatedBy = @EmployeeId
            WHERE CoverageServiceId = @CoverageServiceId;
        END
        SELECT 1 AS Status, 'Coverage service saved.' AS Message, @CoverageServiceId AS CoverageServiceId;
        RETURN;
    END

    /* Contract rows with derived status, days left and visits used. */
    IF (@Status IN ('GET_LIST','COUNTS'))
    BEGIN
        SELECT c.AmcContractId, c.AmcNo, c.AmcType, c.CoverageType, c.CustomerSource, c.SourceCustomerId, c.CustomerName,
               c.DurationMonths, c.StartDate, c.EndDate, c.TotalVisits, c.TotalAmount, c.PaymentStatus, c.Status,
               DATEDIFF(DAY, @Today, c.EndDate) AS DaysLeft,
               CASE WHEN c.Status <> 'ACTIVE' THEN c.Status
                    WHEN c.EndDate < @Today THEN 'EXPIRED'
                    WHEN c.EndDate <= DATEADD(DAY, 30, @Today) THEN 'EXPIRING'
                    WHEN c.StartDate > @Today THEN 'UPCOMING'
                    ELSE 'ACTIVE' END AS DisplayStatus,
               (SELECT COUNT(*) FROM dbo.TKT_Ticket t
                 WHERE t.Ref_AmcContractId = c.AmcContractId AND t.ServiceMode = 'VISIT' AND t.Status = 'COMPLETED') AS VisitsUsed,
               (SELECT COUNT(*) FROM dbo.AMC_ContractItem i WHERE i.Ref_AmcContractId = c.AmcContractId AND i.IsActive = 1) AS ItemCount
        INTO #List
        FROM dbo.AMC_Contract c
        WHERE c.IsActive = 1
          AND (@AmcTypeFilter IS NULL OR c.AmcType = @AmcTypeFilter)
          AND (@Search IS NULL OR c.CustomerName LIKE '%' + @Search + '%' OR c.AmcNo LIKE '%' + @Search + '%' OR c.InvoiceNo LIKE '%' + @Search + '%'
               OR EXISTS (SELECT 1 FROM dbo.AMC_ContractItem i WHERE i.Ref_AmcContractId = c.AmcContractId AND i.IsActive = 1
                          AND (i.SerialNo LIKE '%' + @Search + '%' OR i.ProductName LIKE '%' + @Search + '%')));

        IF (@Status = 'COUNTS')
            SELECT SUM(CASE WHEN DisplayStatus = 'EXPIRING' THEN 1 ELSE 0 END) AS Expiring,
                   SUM(CASE WHEN DisplayStatus = 'EXPIRED'  THEN 1 ELSE 0 END) AS Expired,
                   SUM(CASE WHEN DisplayStatus IN ('ACTIVE','UPCOMING') THEN 1 ELSE 0 END) AS Active,
                   SUM(CASE WHEN DisplayStatus = 'RENEWED'  THEN 1 ELSE 0 END) AS Renewed,
                   SUM(CASE WHEN DisplayStatus = 'CANCELLED' THEN 1 ELSE 0 END) AS Cancelled,
                   COUNT(*) AS Total
            FROM #List;
        ELSE
            SELECT * FROM #List
            WHERE ISNULL(@Filter, 'ALL') = 'ALL'
               OR DisplayStatus = @Filter
               OR (@Filter = 'ACTIVE' AND DisplayStatus IN ('ACTIVE','UPCOMING','EXPIRING'))
            ORDER BY CASE DisplayStatus WHEN 'EXPIRING' THEN 0 WHEN 'EXPIRED' THEN 1 ELSE 2 END, EndDate, AmcContractId DESC;
        RETURN;
    END

    /* ---------- DETAILS ---------- */
    IF (@Status = 'DETAILS')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.AMC_Contract WHERE AmcContractId = @AmcContractId AND IsActive = 1)
            THROW 51001, 'AMC not found.', 1;

        SELECT c.*,
               DATEDIFF(DAY, @Today, c.EndDate) AS DaysLeft,
               CASE WHEN c.Status <> 'ACTIVE' THEN c.Status
                    WHEN c.EndDate < @Today THEN 'EXPIRED'
                    WHEN c.EndDate <= DATEADD(DAY, 30, @Today) THEN 'EXPIRING'
                    WHEN c.StartDate > @Today THEN 'UPCOMING'
                    ELSE 'ACTIVE' END AS DisplayStatus,
               (SELECT COUNT(*) FROM dbo.TKT_Ticket t
                 WHERE t.Ref_AmcContractId = c.AmcContractId AND t.ServiceMode = 'VISIT' AND t.Status = 'COMPLETED') AS VisitsUsed,
               rf.AmcNo AS RenewedFromAmcNo, rt.AmcNo AS RenewedToAmcNo,
               LTRIM(RTRIM(ISNULL(e.FirstName, '') + ' ' + ISNULL(e.LastName, ''))) AS CreatedByName
        FROM dbo.AMC_Contract c
        LEFT JOIN dbo.AMC_Contract rf ON rf.AmcContractId = c.Ref_RenewedFromAmcId
        LEFT JOIN dbo.AMC_Contract rt ON rt.AmcContractId = c.RenewedToAmcId
        LEFT JOIN dbo.ORG_Employee e  ON e.EmployeeId = c.CreatedBy
        WHERE c.AmcContractId = @AmcContractId;

        SELECT AmcContractItemId, Ref_ProductCategoryId AS CategoryId, CategoryName, Ref_OfferingId AS OfferingId, OfferingName,
               Ref_ProductId AS ProductId, ProductName, SerialNo, LicenseKey, SoftwareVersion, NoOfUsers, ItemRemark
        FROM dbo.AMC_ContractItem WHERE Ref_AmcContractId = @AmcContractId AND IsActive = 1 ORDER BY AmcContractItemId;

        SELECT s.CoverageServiceId, s.ServiceName
        FROM dbo.AMC_ContractCoverage cc JOIN dbo.MST_AmcCoverageService s ON s.CoverageServiceId = cc.Ref_CoverageServiceId
        WHERE cc.Ref_AmcContractId = @AmcContractId AND cc.Ref_AmcContractItemId IS NULL ORDER BY s.SortOrder;

        SELECT t.TicketId, t.TicketNo, t.Issue, t.ServiceMode, t.Status, t.AmcCoverageStatus, t.RaisedOn, t.CompletedOn,
               CAST(CASE WHEN t.ServiceMode = 'VISIT' AND t.Status = 'COMPLETED' THEN 1 ELSE 0 END AS BIT) AS CountsAsVisit
        FROM dbo.TKT_Ticket t WHERE t.Ref_AmcContractId = @AmcContractId ORDER BY t.TicketId DESC;

        SELECT l.Action, l.ReminderDays, l.Remark, l.ActionOn,
               LTRIM(RTRIM(ISNULL(e.FirstName, '') + ' ' + ISNULL(e.LastName, ''))) AS ActionByName
        FROM dbo.AMC_ContractLog l LEFT JOIN dbo.ORG_Employee e ON e.EmployeeId = l.ActionBy
        WHERE l.Ref_AmcContractId = @AmcContractId ORDER BY l.AmcContractLogId DESC;
        RETURN;
    END

    /* ---------- COVERAGE_CHECK: AMC badge on ticket screen ---------- */
    IF (@Status = 'COVERAGE_CHECK')
    BEGIN
        SET @SerialNo = NULLIF(LTRIM(RTRIM(@SerialNo)), '');
        SELECT TOP 1 c.AmcContractId, c.AmcNo, c.AmcType, c.CoverageType, c.EndDate, c.TotalVisits,
               (SELECT COUNT(*) FROM dbo.TKT_Ticket t
                 WHERE t.Ref_AmcContractId = c.AmcContractId AND t.ServiceMode = 'VISIT' AND t.Status = 'COMPLETED') AS VisitsUsed
        INTO #Hit
        FROM dbo.AMC_Contract c
        JOIN dbo.AMC_ContractItem i ON i.Ref_AmcContractId = c.AmcContractId AND i.IsActive = 1
        WHERE c.IsActive = 1 AND c.Status = 'ACTIVE' AND @Today BETWEEN c.StartDate AND c.EndDate
          AND ((@SerialNo IS NOT NULL AND i.SerialNo = @SerialNo)
            OR (c.AmcType = 'SOFTWARE' AND @ProductId IS NOT NULL AND i.Ref_ProductId = @ProductId
                AND c.CustomerSource = @CustomerSource AND c.SourceCustomerId = @SourceCustomerId))
        ORDER BY CASE WHEN i.SerialNo = @SerialNo THEN 0 ELSE 1 END, c.EndDate DESC;

        IF EXISTS (SELECT 1 FROM #Hit)
            SELECT *, CASE WHEN TotalVisits IS NOT NULL AND VisitsUsed >= TotalVisits THEN 'VISIT_LIMIT_EXCEEDED' ELSE 'COVERED' END AS CoverageStatus
            FROM #Hit;
        ELSE
            -- last expired/cancelled AMC for the "Chargeable" message
            SELECT TOP 1 * FROM (
                SELECT CAST(NULL AS INT) AS AmcContractId, c.AmcNo AS LastAmcNo, c.EndDate AS LastEndDate, c.Status AS LastStatus,
                       'NOT_COVERED' AS CoverageStatus
                FROM dbo.AMC_Contract c
                JOIN dbo.AMC_ContractItem i ON i.Ref_AmcContractId = c.AmcContractId AND i.IsActive = 1
                WHERE c.IsActive = 1
                  AND ((@SerialNo IS NOT NULL AND i.SerialNo = @SerialNo)
                    OR (@ProductId IS NOT NULL AND i.Ref_ProductId = @ProductId AND c.CustomerSource = @CustomerSource AND c.SourceCustomerId = @SourceCustomerId))
                UNION ALL SELECT NULL, NULL, NULL, NULL, 'NOT_COVERED'
            ) x ORDER BY LastEndDate DESC;
        RETURN;
    END

    /* ---------- LINK_TICKET: called after a ticket is created ---------- */
    IF (@Status = 'LINK_TICKET')
    BEGIN
        DECLARE @LinkId INT, @LinkTotal INT, @LinkUsed INT;
        SELECT TOP 1 @LinkId = c.AmcContractId, @LinkTotal = c.TotalVisits
        FROM dbo.TKT_Ticket t
        JOIN dbo.TKT_TicketItem ti ON ti.Ref_TicketId = t.TicketId
        JOIN dbo.AMC_ContractItem i ON i.IsActive = 1
        JOIN dbo.AMC_Contract c ON c.AmcContractId = i.Ref_AmcContractId AND c.IsActive = 1 AND c.Status = 'ACTIVE'
             AND CAST(t.RaisedOn AS DATE) BETWEEN c.StartDate AND c.EndDate
        WHERE t.TicketId = @TicketId
          AND ((NULLIF(ti.SerialNo, '') IS NOT NULL AND i.SerialNo = ti.SerialNo)
            OR (c.AmcType = 'SOFTWARE' AND ti.Ref_ProductId IS NOT NULL AND i.Ref_ProductId = ti.Ref_ProductId
                AND c.CustomerSource = t.CustomerSource AND c.SourceCustomerId = t.SourceCustomerId))
        ORDER BY CASE WHEN i.SerialNo = ti.SerialNo THEN 0 ELSE 1 END, c.EndDate DESC;

        IF (@LinkId IS NOT NULL)
            SELECT @LinkUsed = COUNT(*) FROM dbo.TKT_Ticket
            WHERE Ref_AmcContractId = @LinkId AND ServiceMode = 'VISIT' AND Status = 'COMPLETED';

        UPDATE dbo.TKT_Ticket
        SET Ref_AmcContractId = @LinkId,
            AmcCoverageStatus = CASE WHEN @LinkId IS NULL THEN 'NOT_COVERED'
                                     WHEN @LinkTotal IS NOT NULL AND @LinkUsed >= @LinkTotal THEN 'VISIT_LIMIT_EXCEEDED'
                                     ELSE 'COVERED' END
        WHERE TicketId = @TicketId;

        SELECT 1 AS Status, 'Ticket AMC link updated.' AS Message, @LinkId AS AmcContractId;
        RETURN;
    END

    /* ---------- CANCEL ---------- */
    IF (@Status = 'CANCEL')
    BEGIN
        SET @Reason = NULLIF(LTRIM(RTRIM(@Reason)), '');
        IF (@Reason IS NULL) THROW 51002, 'Please enter the cancel reason.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.AMC_Contract WHERE AmcContractId = @AmcContractId AND IsActive = 1)
            THROW 51001, 'AMC not found.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.AMC_Contract WHERE AmcContractId = @AmcContractId AND Status = 'ACTIVE')
            THROW 51003, 'Only an active AMC can be cancelled.', 1;

        BEGIN TRAN;
            UPDATE dbo.AMC_Contract
            SET Status = 'CANCELLED', CancelReason = @Reason, CancelledBy = @EmployeeId, CancelledOn = GETDATE(),
                UpdatedOn = GETDATE(), UpdatedBy = @EmployeeId
            WHERE AmcContractId = @AmcContractId;
            INSERT INTO dbo.AMC_ContractLog (Ref_AmcContractId, Action, Remark, ActionBy) VALUES (@AmcContractId, 'CANCELLED', @Reason, @EmployeeId);
        COMMIT;
        SELECT 1 AS Status, 'AMC cancelled.' AS Message, @AmcContractId AS AmcContractId;
        RETURN;
    END

    /* ---------- ATTACH_INVOICE ---------- */
    IF (@Status = 'ATTACH_INVOICE')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.AMC_Contract WHERE AmcContractId = @AmcContractId AND IsActive = 1)
            THROW 51001, 'AMC not found.', 1;
        IF (@InvoiceFilePath IS NULL) THROW 51004, 'Please attach the invoice PDF.', 1;

        BEGIN TRAN;
            UPDATE dbo.AMC_Contract
            SET InvoiceFilePath = @InvoiceFilePath, InvoiceFileName = @InvoiceFileName,
                InvoiceNo = ISNULL(NULLIF(LTRIM(RTRIM(@InvoiceNo)), ''), InvoiceNo),
                InvoiceDate = ISNULL(@InvoiceDate, InvoiceDate),
                UpdatedOn = GETDATE(), UpdatedBy = @EmployeeId
            WHERE AmcContractId = @AmcContractId;
            INSERT INTO dbo.AMC_ContractLog (Ref_AmcContractId, Action, Remark, ActionBy) VALUES (@AmcContractId, 'INVOICE_ATTACHED', @InvoiceFileName, @EmployeeId);
        COMMIT;
        SELECT 1 AS Status, 'Invoice attached.' AS Message, @AmcContractId AS AmcContractId;
        RETURN;
    END

    /* ---------- CREATE / UPDATE / RENEW share validation ---------- */
    IF (@Status IN ('CREATE','UPDATE','RENEW'))
    BEGIN
        DECLARE @OldId INT = NULL;   -- UPDATE: the row itself; RENEW: the contract being renewed
        IF (@Status IN ('UPDATE','RENEW'))
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM dbo.AMC_Contract WHERE AmcContractId = @AmcContractId AND IsActive = 1)
                THROW 51001, 'AMC not found.', 1;
            IF NOT EXISTS (SELECT 1 FROM dbo.AMC_Contract WHERE AmcContractId = @AmcContractId AND Status = 'ACTIVE')
                THROW 51005, 'Only an active AMC can be edited or renewed.', 1;
            SET @OldId = @AmcContractId;
            -- Renew keeps the customer of the old contract.
            IF (@Status = 'RENEW')
                SELECT @CustomerSource = CustomerSource, @SourceCustomerId = SourceCustomerId, @Ref_CustomerId = Ref_CustomerId,
                       @CustomerName = CustomerName
                FROM dbo.AMC_Contract WHERE AmcContractId = @OldId;
        END

        SET @AmcType = UPPER(LTRIM(RTRIM(@AmcType)));
        SET @CoverageType = UPPER(LTRIM(RTRIM(@CoverageType)));
        SET @PaymentStatus = ISNULL(NULLIF(UPPER(LTRIM(RTRIM(@PaymentStatus))), ''), 'PENDING');
        SET @GstPercent = ISNULL(@GstPercent, 0);

        IF (@CustomerSource NOT IN ('CRM','WMS') OR ISNULL(@SourceCustomerId, 0) <= 0 OR NULLIF(LTRIM(RTRIM(@CustomerName)), '') IS NULL)
            THROW 51010, 'Please select a customer.', 1;
        IF (ISNULL(@AmcType, '') NOT IN ('HARDWARE','SOFTWARE'))
            THROW 51011, 'AMC type must be Hardware or Software.', 1;
        IF (ISNULL(@CoverageType, '') NOT IN ('COMPREHENSIVE','NON_COMPREHENSIVE'))
            THROW 51012, 'Coverage must be Comprehensive or Non-comprehensive.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.MST_AmcDuration WHERE DurationMonths = @DurationMonths AND IsActive = 1)
            THROW 51013, 'Please select a valid duration.', 1;
        IF (@StartDate IS NULL) THROW 51014, 'Start date is required.', 1;
        IF (@AmcType = 'HARDWARE' AND ISNULL(@TotalVisits, 0) <= 0)
            THROW 51015, 'No. of visits is required for Hardware AMC.', 1;
        IF (@TotalVisits IS NOT NULL AND @TotalVisits < 0) THROW 51016, 'No. of visits cannot be negative.', 1;
        IF (ISNULL(@Amount, -1) < 0) THROW 51017, 'Please enter a valid amount.', 1;
        IF (@GstPercent < 0 OR @GstPercent > 100) THROW 51018, 'Please enter a valid GST %.', 1;
        IF (@PaymentStatus NOT IN ('PENDING','PARTIAL','PAID')) THROW 51019, 'Invalid payment status.', 1;
        IF (@ItemsJson IS NULL OR NOT EXISTS (SELECT 1 FROM OPENJSON(@ItemsJson)))
            THROW 51020, 'Please add at least one item.', 1;

        DECLARE @EndDate DATE = DATEADD(DAY, -1, DATEADD(MONTH, @DurationMonths, @StartDate));
        DECLARE @GstAmount DECIMAL(18,2) = ROUND(@Amount * @GstPercent / 100, 2);
        DECLARE @TotalAmount DECIMAL(18,2) = @Amount + @GstAmount;

        IF (@Status = 'RENEW' AND @StartDate <= (SELECT StartDate FROM dbo.AMC_Contract WHERE AmcContractId = @OldId))
            THROW 51021, 'Renewal start date must be after the old AMC start date.', 1;

        SELECT IDENTITY(INT, 1, 1) AS RowNo,
               j.categoryId, NULLIF(LTRIM(RTRIM(j.categoryName)), '') AS categoryName,
               j.offeringId, NULLIF(LTRIM(RTRIM(j.offeringName)), '') AS offeringName,
               j.productId, NULLIF(LTRIM(RTRIM(j.productName)), '') AS productName,
               NULLIF(LTRIM(RTRIM(j.serialNo)), '') AS serialNo,
               NULLIF(LTRIM(RTRIM(j.licenseKey)), '') AS licenseKey,
               NULLIF(LTRIM(RTRIM(j.softwareVersion)), '') AS softwareVersion,
               j.noOfUsers, NULLIF(LTRIM(RTRIM(j.remark)), '') AS remark
        INTO #Items
        FROM OPENJSON(@ItemsJson) WITH (
            categoryId INT, categoryName NVARCHAR(200), offeringId INT, offeringName NVARCHAR(200),
            productId BIGINT, productName NVARCHAR(255), serialNo NVARCHAR(100), licenseKey NVARCHAR(200),
            softwareVersion NVARCHAR(50), noOfUsers INT, remark NVARCHAR(500)) j;

        IF EXISTS (SELECT 1 FROM #Items WHERE productId IS NULL AND productName IS NULL)
            THROW 51022, 'Please select the product for every item.', 1;
        IF (@AmcType = 'HARDWARE' AND EXISTS (SELECT 1 FROM #Items WHERE serialNo IS NULL))
            THROW 51023, 'Serial No. is required for every Hardware item.', 1;
        IF EXISTS (SELECT serialNo FROM #Items WHERE serialNo IS NOT NULL GROUP BY serialNo HAVING COUNT(*) > 1)
            THROW 51024, 'The same Serial No. is entered twice.', 1;

        -- Hardware: a serial cannot be in two active AMCs with overlapping dates.
        DECLARE @Clash NVARCHAR(400);
        SELECT TOP 1 @Clash = 'Serial No. ' + i.serialNo + ' is already under ' + c.AmcNo + ' (till '
                              + CONVERT(VARCHAR(11), c.EndDate, 106) + ').'
        FROM #Items i
        JOIN dbo.AMC_ContractItem ci ON ci.SerialNo = i.serialNo AND ci.IsActive = 1
        JOIN dbo.AMC_Contract c ON c.AmcContractId = ci.Ref_AmcContractId AND c.IsActive = 1 AND c.Status = 'ACTIVE'
        WHERE @AmcType = 'HARDWARE' AND c.AmcType = 'HARDWARE'
          AND c.AmcContractId <> ISNULL(@OldId, 0)
          AND c.StartDate <= @EndDate AND c.EndDate >= @StartDate;
        IF (@Clash IS NOT NULL) THROW 51025, @Clash, 1;

        IF EXISTS (SELECT 1 FROM STRING_SPLIT(ISNULL(@CoverageIds, ''), ',') s
                   WHERE LTRIM(RTRIM(s.value)) <> ''
                     AND NOT EXISTS (SELECT 1 FROM dbo.MST_AmcCoverageService m
                                     WHERE m.CoverageServiceId = TRY_CAST(s.value AS INT) AND m.IsActive = 1))
            THROW 51026, 'Invalid coverage service selected.', 1;

        IF (@CustomerSource = 'CRM' AND @Ref_CustomerId IS NULL)
            SELECT TOP 1 @Ref_CustomerId = CustomerId FROM dbo.CUS_Customer
            WHERE Ref_LeadOrganizationId = @SourceCustomerId AND IsActive = 1 ORDER BY CustomerId;

        DECLARE @Id INT, @AmcNo VARCHAR(20), @Year INT = YEAR(GETDATE()), @Next INT;

        BEGIN TRAN;
            IF (@Status = 'UPDATE')
            BEGIN
                SET @Id = @OldId;
                UPDATE dbo.AMC_Contract
                SET AmcType = @AmcType, CoverageType = @CoverageType,
                    CustomerSource = @CustomerSource, SourceCustomerId = @SourceCustomerId, Ref_CustomerId = @Ref_CustomerId,
                    CustomerName = LTRIM(RTRIM(@CustomerName)), ContactMobile = @ContactMobile, ContactEmail = @ContactEmail,
                    DurationMonths = @DurationMonths, StartDate = @StartDate, EndDate = @EndDate, TotalVisits = @TotalVisits,
                    InvoiceNo = @InvoiceNo, InvoiceDate = @InvoiceDate,
                    InvoiceFilePath = ISNULL(@InvoiceFilePath, InvoiceFilePath), InvoiceFileName = ISNULL(@InvoiceFileName, InvoiceFileName),
                    Amount = @Amount, GstPercent = @GstPercent, GstAmount = @GstAmount, TotalAmount = @TotalAmount,
                    PaymentStatus = @PaymentStatus, PaymentMode = @PaymentMode, PaymentDate = @PaymentDate, PaymentRemark = @PaymentRemark,
                    Remarks = @Remarks, UpdatedOn = GETDATE(), UpdatedBy = @EmployeeId
                WHERE AmcContractId = @Id;
                SELECT @AmcNo = AmcNo FROM dbo.AMC_Contract WHERE AmcContractId = @Id;
                UPDATE dbo.AMC_ContractItem SET IsActive = 0, UpdatedOn = GETDATE(), UpdatedBy = @EmployeeId
                WHERE Ref_AmcContractId = @Id AND IsActive = 1;
                DELETE FROM dbo.AMC_ContractCoverage WHERE Ref_AmcContractId = @Id;
            END
            ELSE
            BEGIN
                UPDATE dbo.AMC_NumberCounter WITH (UPDLOCK, HOLDLOCK)
                SET @Next = LastNumber = LastNumber + 1 WHERE AmcYear = @Year;
                IF (@Next IS NULL)
                BEGIN
                    SET @Next = 1;
                    INSERT INTO dbo.AMC_NumberCounter (AmcYear, LastNumber) VALUES (@Year, 1);
                END
                SET @AmcNo = 'AMC-' + CAST(@Year AS VARCHAR(4)) + '-' + RIGHT('0000' + CAST(@Next AS VARCHAR(10)), 4);

                INSERT INTO dbo.AMC_Contract
                    (AmcNo, AmcType, CoverageType, CustomerSource, SourceCustomerId, Ref_CustomerId, CustomerName, ContactMobile, ContactEmail,
                     DurationMonths, StartDate, EndDate, TotalVisits, InvoiceNo, InvoiceDate, InvoiceFilePath, InvoiceFileName,
                     Amount, GstPercent, GstAmount, TotalAmount, PaymentStatus, PaymentMode, PaymentDate, PaymentRemark,
                     Ref_RenewedFromAmcId, Remarks, CreatedBy)
                VALUES
                    (@AmcNo, @AmcType, @CoverageType, @CustomerSource, @SourceCustomerId, @Ref_CustomerId, LTRIM(RTRIM(@CustomerName)), @ContactMobile, @ContactEmail,
                     @DurationMonths, @StartDate, @EndDate, @TotalVisits, @InvoiceNo, @InvoiceDate, @InvoiceFilePath, @InvoiceFileName,
                     @Amount, @GstPercent, @GstAmount, @TotalAmount, @PaymentStatus, @PaymentMode, @PaymentDate, @PaymentRemark,
                     CASE WHEN @Status = 'RENEW' THEN @OldId END, @Remarks, @EmployeeId);
                SET @Id = SCOPE_IDENTITY();

                IF (@Status = 'RENEW')
                BEGIN
                    UPDATE dbo.AMC_Contract SET Status = 'RENEWED', RenewedToAmcId = @Id, UpdatedOn = GETDATE(), UpdatedBy = @EmployeeId
                    WHERE AmcContractId = @OldId;
                    INSERT INTO dbo.AMC_ContractLog (Ref_AmcContractId, Action, Remark, ActionBy)
                    VALUES (@OldId, 'RENEWED', 'Renewed as ' + @AmcNo, @EmployeeId);
                END
            END

            INSERT INTO dbo.AMC_ContractItem
                (Ref_AmcContractId, Ref_ProductCategoryId, CategoryName, Ref_OfferingId, OfferingName, Ref_ProductId, ProductName,
                 SerialNo, LicenseKey, SoftwareVersion, NoOfUsers, ItemRemark, CreatedBy)
            SELECT @Id, categoryId, categoryName, offeringId, offeringName, productId, productName,
                   serialNo, licenseKey, softwareVersion, noOfUsers, remark, @EmployeeId
            FROM #Items ORDER BY RowNo;

            INSERT INTO dbo.AMC_ContractCoverage (Ref_AmcContractId, Ref_CoverageServiceId, CreatedBy)
            SELECT DISTINCT @Id, TRY_CAST(value AS INT), @EmployeeId
            FROM STRING_SPLIT(ISNULL(@CoverageIds, ''), ',') WHERE TRY_CAST(value AS INT) IS NOT NULL;

            INSERT INTO dbo.AMC_ContractLog (Ref_AmcContractId, Action, Remark, ActionBy)
            VALUES (@Id, CASE @Status WHEN 'UPDATE' THEN 'EDITED' ELSE 'CREATED' END,
                    CASE WHEN @Status = 'RENEW' THEN 'Renewal of ' + (SELECT AmcNo FROM dbo.AMC_Contract WHERE AmcContractId = @OldId) END,
                    @EmployeeId);
        COMMIT;

        SELECT 1 AS Status,
               CASE @Status WHEN 'CREATE' THEN 'AMC created.' WHEN 'UPDATE' THEN 'AMC updated.' ELSE 'AMC renewed.' END AS Message,
               @Id AS AmcContractId, @AmcNo AS AmcNo, @EndDate AS EndDate, @TotalAmount AS TotalAmount;
        RETURN;
    END

    ;THROW 51099, 'Invalid status.', 1;
END
GO

/* ---------- Permissions: module "AMC" under SUPPORT, granted to Admin (role 1) only ----------
   Other roles / users get access from Role Access or User Override. */
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ModuleId INT = (SELECT ModuleId FROM dbo.ORG_Module WHERE ModuleCode = 'AMC');
IF (@ModuleId IS NULL)
BEGIN
    INSERT INTO dbo.ORG_Module (ModuleCode, ModuleName, IsActive, CreatedOn, CreatedBy) VALUES ('AMC', 'AMC', 1, GETDATE(), 1);
    SET @ModuleId = SCOPE_IDENTITY();
    INSERT INTO dbo.ORG_SuperModuleMapping (SuperModuleId, ModuleId, CreatedOn, CreatedBy)
    SELECT SuperModuleId, @ModuleId, GETDATE(), 1 FROM dbo.ORG_SuperModule WHERE SuperModuleCode = 'SUPPORT';
    INSERT INTO dbo.ORG_RoleModuleAccess (Ref_RoleId, Ref_ModuleId, IsEnabled, CreatedOn, CreatedBy) VALUES (1, @ModuleId, 1, GETDATE(), 1);
END

INSERT INTO dbo.ORG_Permission (ModuleName, PermissionCode, PermissionName, Description, IsActive, CreatedOn, CreatedBy, Ref_ModuleId)
SELECT 'AMC', v.Code, v.Name, v.Descr, 1, GETDATE(), 1, @ModuleId
FROM (VALUES
    ('AMC_View',     'View AMC',          'AMC list and details'),
    ('AMC_Create',   'Create AMC',        'Create AMC and Excel upload'),
    ('AMC_Edit',     'Edit AMC',          'Edit AMC and attach invoice'),
    ('AMC_Renew',    'Renew AMC',         'Renewals screen: renew'),
    ('AMC_Cancel',   'Cancel AMC',        'Renewals screen: cancel'),
    ('AMC_Coverage', 'AMC Coverage Master','Manage coverage services')
) v (Code, Name, Descr)
WHERE NOT EXISTS (SELECT 1 FROM dbo.ORG_Permission p WHERE p.PermissionCode = v.Code);

-- Scheduler scope: shown in Scheduler > Create Job (EntityType AMC_EXPIRY). Employees allowed this
-- permission are CC'd on customer expiry reminders and get the push notification.
INSERT INTO dbo.ORG_Permission (ModuleName, PermissionCode, PermissionName, Description, IsActive, CreatedOn, CreatedBy, Ref_ModuleId,
                                IsSchedulerSupported, SchedulerEntityType)
SELECT 'AMC', 'AMC_ExpiryReminder', 'AMC Expiry Reminder', 'CC on AMC expiry reminder mails (30/15/10/5/1 days)', 1, GETDATE(), 1, @ModuleId,
       1, 'AMC_EXPIRY'
WHERE NOT EXISTS (SELECT 1 FROM dbo.ORG_Permission p WHERE p.PermissionCode = 'AMC_ExpiryReminder');

INSERT INTO dbo.ORG_RolePermission (Ref_RoleId, Ref_PermissionId, IsAllowed, CreatedOn, CreatedBy)
SELECT 1, p.PermissionId, 1, GETDATE(), 1
FROM dbo.ORG_Permission p
WHERE p.Ref_ModuleId = @ModuleId
  AND NOT EXISTS (SELECT 1 FROM dbo.ORG_RolePermission rp WHERE rp.Ref_RoleId = 1 AND rp.Ref_PermissionId = p.PermissionId);
COMMIT;
GO
