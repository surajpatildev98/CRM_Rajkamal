/* =====================================================================
   Ticket Support module (ModuleCode Ticket_support, ModuleId 4013)
   ---------------------------------------------------------------------
   Flows (see Ticket_employee_flow / Ticket_manager_flow /
   Ticket_activity_flow designs):
     * Support Manager (ORG_Role 9) raises a ticket for a mapped employee
       (ORG_Employee.Ref_ReportingManagerId = manager) or an employee
       raises his own ticket.
     * Service mode ONLINE or VISIT. A VISIT ticket raised by an employee
       needs Support Manager approval.
     * Travel / visit expenses: BUS, TRAIN, AUTO, PERSONAL_BIKE are auto
       approved; PERSONAL_CAR, STAY, OTHER need Support Manager approval.
     * Complete is blocked while the ticket or any expense is pending.
   Approver = the Support Manager the assignee reports to.
   Requires: Database/CustomerSync_00..05_*.sql (USP_CRM_CustomerManagement).
   Run on Rkl_EnterpriseSuiteDB_Live.
   ===================================================================== */

/* ============================ TABLES ============================ */

IF OBJECT_ID('dbo.TKT_Ticket') IS NULL
CREATE TABLE dbo.TKT_Ticket
(
    TicketId            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TKT_Ticket PRIMARY KEY,
    TicketNo            VARCHAR(20)    NOT NULL,
    -- Customer: identified by source + original id (ids overlap between CRM and WMS).
    CustomerSource      VARCHAR(3)     NOT NULL,          -- CRM | WMS
    SourceCustomerId    INT            NOT NULL,          -- CRM: LeadOrganizationId, WMS: CustomerMasterID
    Ref_CustomerId      INT            NULL,              -- CUS_Customer.CustomerId when known
    CustomerName        NVARCHAR(255)  NOT NULL,
    Issue               NVARCHAR(1000) NOT NULL,
    Priority            VARCHAR(10)    NOT NULL CONSTRAINT DF_TKT_Ticket_Priority DEFAULT ('MEDIUM'), -- LOW | MEDIUM | HIGH
    ServiceMode         VARCHAR(10)    NOT NULL,          -- ONLINE | VISIT
    RaisedOn            DATETIME       NOT NULL,
    ExpectedDate        DATE           NOT NULL,
    RaisedBy            INT            NOT NULL,
    IsRaisedByManager   BIT            NOT NULL CONSTRAINT DF_TKT_Ticket_IsRaisedByManager DEFAULT (0),
    AssignedTo          INT            NOT NULL,
    Status              VARCHAR(20)    NOT NULL,          -- OPEN | APPROVAL_PENDING | IN_PROGRESS | COMPLETED | REJECTED | CANCELLED
    ApprovalStatus      VARCHAR(20)    NOT NULL,          -- NOT_REQUIRED | PENDING | APPROVED | REJECTED
    ApprovedBy          INT            NULL,
    ApprovedOn          DATETIME       NULL,
    ApprovalRemark      NVARCHAR(500)  NULL,
    CheckInOn           DATETIME       NULL,
    CheckInLatitude     DECIMAL(9,6)   NULL,
    CheckInLongitude    DECIMAL(9,6)   NULL,
    CheckInAddress      NVARCHAR(500)  NULL,
    ResolutionRemarks   NVARCHAR(1000) NULL,
    CompletedOn         DATETIME       NULL,
    CreatedOn           DATETIME       NOT NULL CONSTRAINT DF_TKT_Ticket_CreatedOn DEFAULT (GETDATE()),
    CreatedBy           INT            NOT NULL,
    UpdatedOn           DATETIME       NULL,
    UpdatedBy           INT            NULL,
    CONSTRAINT UQ_TKT_Ticket_TicketNo UNIQUE (TicketNo),
    CONSTRAINT CK_TKT_Ticket_CustomerSource CHECK (CustomerSource IN ('CRM','WMS')),
    CONSTRAINT CK_TKT_Ticket_ServiceMode CHECK (ServiceMode IN ('ONLINE','CARRY_IN','VISIT')),
    CONSTRAINT CK_TKT_Ticket_Priority CHECK (Priority IN ('LOW','MEDIUM','HIGH'))
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TKT_Ticket_AssignedTo')
    CREATE INDEX IX_TKT_Ticket_AssignedTo ON dbo.TKT_Ticket (AssignedTo, Status);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TKT_Ticket_RaisedBy')
    CREATE INDEX IX_TKT_Ticket_RaisedBy ON dbo.TKT_Ticket (RaisedBy, Status);
GO

IF OBJECT_ID('dbo.TKT_TicketActivity') IS NULL
CREATE TABLE dbo.TKT_TicketActivity
(
    TicketActivityId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TKT_TicketActivity PRIMARY KEY,
    Ref_TicketId        INT            NOT NULL CONSTRAINT FK_TKT_TicketActivity_Ticket REFERENCES dbo.TKT_Ticket (TicketId),
    ActivityType        VARCHAR(20)    NOT NULL,          -- SITE_VISIT | CALL | ONLINE
    Title               NVARCHAR(200)  NOT NULL,
    ActivityOn          DATETIME       NOT NULL,
    Location            NVARCHAR(500)  NULL,
    Latitude            DECIMAL(9,6)   NULL,
    Longitude           DECIMAL(9,6)   NULL,
    ContactPerson       NVARCHAR(150)  NULL,
    Remarks             NVARCHAR(1000) NULL,
    ActivityStatus      VARCHAR(20)    NOT NULL,          -- PLANNED | COMPLETED
    CreatedOn           DATETIME       NOT NULL CONSTRAINT DF_TKT_TicketActivity_CreatedOn DEFAULT (GETDATE()),
    CreatedBy           INT            NOT NULL,
    CONSTRAINT CK_TKT_TicketActivity_Type CHECK (ActivityType IN ('SITE_VISIT','CALL','ONLINE')),
    CONSTRAINT CK_TKT_TicketActivity_Status CHECK (ActivityStatus IN ('PLANNED','COMPLETED'))
);
GO

-- Full readable address of the activity location (reverse geocoded on the phone).
-- Latitude / Longitude stay as the raw GPS values.
IF COL_LENGTH('dbo.TKT_TicketActivity', 'LocationAddress') IS NULL
    ALTER TABLE dbo.TKT_TicketActivity ADD LocationAddress NVARCHAR(500) NULL;
GO

-- Travel requests (sent before the visit) and visit expenses (added on an activity).
IF OBJECT_ID('dbo.TKT_TicketExpense') IS NULL
CREATE TABLE dbo.TKT_TicketExpense
(
    TicketExpenseId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TKT_TicketExpense PRIMARY KEY,
    Ref_TicketId        INT            NOT NULL CONSTRAINT FK_TKT_TicketExpense_Ticket REFERENCES dbo.TKT_Ticket (TicketId),
    Ref_TicketActivityId INT           NULL CONSTRAINT FK_TKT_TicketExpense_Activity REFERENCES dbo.TKT_TicketActivity (TicketActivityId),
    ExpenseSource       VARCHAR(10)    NOT NULL,          -- TRAVEL (request before visit) | VISIT (on an activity)
    Category            VARCHAR(20)    NOT NULL,          -- TRANSPORT | STAY | FOOD | OTHER
    TravelMode          VARCHAR(20)    NOT NULL,          -- BUS | TRAIN | AUTO | PERSONAL_BIKE | PERSONAL_CAR | STAY | OTHER
    FromLocation        NVARCHAR(200)  NULL,
    ToLocation          NVARCHAR(200)  NULL,
    Km                  DECIMAL(9,2)   NULL,
    Amount              DECIMAL(12,2)  NOT NULL,
    Remarks             NVARCHAR(500)  NULL,
    ReceiptFileName     NVARCHAR(255)  NULL,
    ReceiptPath         NVARCHAR(500)  NULL,
    ApprovalStatus      VARCHAR(20)    NOT NULL,          -- AUTO_OK | PENDING | APPROVED | REJECTED
    ApprovedBy          INT            NULL,
    ApprovedOn          DATETIME       NULL,
    ApprovalRemark      NVARCHAR(500)  NULL,
    CreatedOn           DATETIME       NOT NULL CONSTRAINT DF_TKT_TicketExpense_CreatedOn DEFAULT (GETDATE()),
    CreatedBy           INT            NOT NULL,
    CONSTRAINT CK_TKT_TicketExpense_Source CHECK (ExpenseSource IN ('TRAVEL','VISIT')),
    CONSTRAINT CK_TKT_TicketExpense_Mode CHECK (TravelMode IN ('BUS','TRAIN','AUTO','PERSONAL_BIKE','PERSONAL_CAR','STAY','OTHER')),
    CONSTRAINT CK_TKT_TicketExpense_Amount CHECK (Amount >= 0)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TKT_TicketExpense_Ticket')
    CREATE INDEX IX_TKT_TicketExpense_Ticket ON dbo.TKT_TicketExpense (Ref_TicketId, ApprovalStatus);
GO

IF OBJECT_ID('dbo.TKT_TicketAttachment') IS NULL
CREATE TABLE dbo.TKT_TicketAttachment
(
    TicketAttachmentId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TKT_TicketAttachment PRIMARY KEY,
    Ref_TicketId        INT            NOT NULL CONSTRAINT FK_TKT_TicketAttachment_Ticket REFERENCES dbo.TKT_Ticket (TicketId),
    AttachmentType      VARCHAR(20)    NOT NULL,          -- PHOTO | SIGNATURE | BILL | OTHER
    FileName            NVARCHAR(255)  NOT NULL,
    FilePath            NVARCHAR(500)  NOT NULL,
    CreatedOn           DATETIME       NOT NULL CONSTRAINT DF_TKT_TicketAttachment_CreatedOn DEFAULT (GETDATE()),
    CreatedBy           INT            NOT NULL
);
GO

IF OBJECT_ID('dbo.TKT_TicketStatusLog') IS NULL
CREATE TABLE dbo.TKT_TicketStatusLog
(
    TicketStatusLogId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TKT_TicketStatusLog PRIMARY KEY,
    Ref_TicketId        INT            NOT NULL CONSTRAINT FK_TKT_TicketStatusLog_Ticket REFERENCES dbo.TKT_Ticket (TicketId),
    FromStatus          VARCHAR(20)    NULL,
    ToStatus            VARCHAR(20)    NOT NULL,
    Remarks             NVARCHAR(500)  NULL,
    CreatedOn           DATETIME       NOT NULL CONSTRAINT DF_TKT_TicketStatusLog_CreatedOn DEFAULT (GETDATE()),
    CreatedBy           INT            NOT NULL
);
GO

/* ---- Service Mode: add CARRY_IN (works like ONLINE: no travel / approval / check-in) ---- */
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_TKT_Ticket_ServiceMode'
           AND definition NOT LIKE '%CARRY_IN%')
    ALTER TABLE dbo.TKT_Ticket DROP CONSTRAINT CK_TKT_Ticket_ServiceMode;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_TKT_Ticket_ServiceMode')
    ALTER TABLE dbo.TKT_Ticket ADD CONSTRAINT CK_TKT_Ticket_ServiceMode
        CHECK (ServiceMode IN ('ONLINE','CARRY_IN','VISIT'));
GO

/* ---- Check-out (after check-in) ---- */
IF COL_LENGTH('dbo.TKT_Ticket', 'CheckOutOn') IS NULL
    ALTER TABLE dbo.TKT_Ticket ADD CheckOutOn DATETIME NULL,
                                   CheckOutLatitude DECIMAL(9,6) NULL,
                                   CheckOutLongitude DECIMAL(9,6) NULL,
                                   CheckOutAddress NVARCHAR(500) NULL;
/* ---- Close request (assignee -> raiser) ---- */
IF COL_LENGTH('dbo.TKT_Ticket', 'CloseRequestedOn') IS NULL
    ALTER TABLE dbo.TKT_Ticket ADD CloseRequestedOn DATETIME NULL,
                                   CloseDecisionRemark NVARCHAR(500) NULL;
/* ---- Planned activity completed later ---- */
IF COL_LENGTH('dbo.TKT_TicketActivity', 'CompletedOn') IS NULL
    ALTER TABLE dbo.TKT_TicketActivity ADD CompletedOn DATETIME NULL;
GO

/* ---- Service Type (Warranty / AMC / Billable / Testing / Other) ---- */
IF COL_LENGTH('dbo.TKT_Ticket', 'ServiceTypes') IS NULL
    ALTER TABLE dbo.TKT_Ticket ADD ServiceTypes VARCHAR(100) NULL;          -- comma list: WARRANTY,AMC,BILLABLE,TESTING,OTHER
IF COL_LENGTH('dbo.TKT_Ticket', 'ServiceTypeOther') IS NULL
    ALTER TABLE dbo.TKT_Ticket ADD ServiceTypeOther NVARCHAR(200) NULL;     -- text when OTHER is ticked
GO

/* ---- Requirement / Offering items (Category > Offering > Product + Serial No) ---- */
IF OBJECT_ID('dbo.TKT_TicketItem') IS NULL
CREATE TABLE dbo.TKT_TicketItem
(
    TicketItemId            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TKT_TicketItem PRIMARY KEY,
    Ref_TicketId            INT            NOT NULL CONSTRAINT FK_TKT_TicketItem_Ticket REFERENCES dbo.TKT_Ticket (TicketId),
    Ref_ProductCategoryId   INT            NULL,
    CategoryName            NVARCHAR(200)  NULL,
    Ref_OfferingId          INT            NULL,
    OfferingName            NVARCHAR(200)  NULL,
    Ref_ProductId           BIGINT         NULL,
    ProductName             NVARCHAR(255)  NULL,
    SerialNo                NVARCHAR(100)  NULL,
    ItemRemark              NVARCHAR(500)  NULL,
    CreatedOn               DATETIME       NOT NULL CONSTRAINT DF_TKT_TicketItem_CreatedOn DEFAULT (GETDATE()),
    CreatedBy               INT            NOT NULL
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TKT_TicketItem_Ticket')
    CREATE INDEX IX_TKT_TicketItem_Ticket ON dbo.TKT_TicketItem (Ref_TicketId);
GO

/* ========================= PROCEDURE ========================= */
CREATE OR ALTER PROCEDURE dbo.USP_TKT_TicketManagement
    @Status             VARCHAR(50),
    @EmployeeId         INT            = NULL,   -- acting employee (from JWT)
    @IsManager          BIT            = 0,      -- acting employee has ORG_Role 9 (from JWT)
    @TicketId           INT            = NULL,
    @ListType           VARCHAR(20)    = NULL,   -- ASSIGNED | RAISED | TEAM_OPEN | TEAM_CLOSED
    @CustomerSource     VARCHAR(3)     = NULL,
    @SourceCustomerId   INT            = NULL,
    @Ref_CustomerId     INT            = NULL,
    @CustomerName       NVARCHAR(255)  = NULL,
    @Issue              NVARCHAR(1000) = NULL,
    @Priority           VARCHAR(10)    = NULL,
    @ServiceMode        VARCHAR(10)    = NULL,
    @ExpectedDate       DATE           = NULL,
    @AssignedTo         INT            = NULL,
    @Decision           VARCHAR(10)    = NULL,   -- APPROVE | REJECT
    @Remarks            NVARCHAR(1000) = NULL,
    @TicketExpenseId    INT            = NULL,
    @Latitude           DECIMAL(9,6)   = NULL,
    @Longitude          DECIMAL(9,6)   = NULL,
    @Address            NVARCHAR(500)  = NULL,
    @ExpensesJson       NVARCHAR(MAX)  = NULL,   -- [{ticketActivityId,category,travelMode,fromLocation,toLocation,km,amount,remarks,receiptFileName,receiptPath}]
    @AttachmentsJson    NVARCHAR(MAX)  = NULL,   -- [{attachmentType,fileName,filePath}]
    @ActivityType       VARCHAR(20)    = NULL,
    @Title              NVARCHAR(200)  = NULL,
    @ActivityOn         DATETIME       = NULL,
    @ContactPerson      NVARCHAR(150)  = NULL,
    @ActivityStatus     VARCHAR(20)    = NULL,
    @ServiceTypes       VARCHAR(100)   = NULL,   -- comma list: WARRANTY,AMC,BILLABLE,TESTING,OTHER
    @ServiceTypeOther   NVARCHAR(200)  = NULL,
    @ItemsJson          NVARCHAR(MAX)  = NULL,   -- [{categoryId,categoryName,offeringId,offeringName,productId,productName,serialNo,remark}]
    @TicketActivityId   INT            = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now DATETIME = GETDATE();
    DECLARE @Today DATE = CAST(GETDATE() AS DATE);

    /* Expense modes that need Support Manager approval. */
    DECLARE @ApprovalModes TABLE (TravelMode VARCHAR(20) PRIMARY KEY);
    INSERT INTO @ApprovalModes VALUES ('PERSONAL_CAR'), ('STAY'), ('OTHER');

    /* ===================== TICKET_EMAIL_DATA (internal: email body) =====================
       Called only by the API when sending a notification email, after the action
       itself passed the access check. @EmployeeId = email recipient. */
    IF (@Status = 'TICKET_EMAIL_DATA')
    BEGIN
        SELECT T.TicketNo, T.CustomerName, T.Issue, T.Priority, T.ServiceMode, T.ExpectedDate, T.RaisedOn,
               T.Status AS TicketStatus, T.ServiceTypes, T.ServiceTypeOther, T.ResolutionRemarks,
               ISNULL(T.CloseDecisionRemark, T.ApprovalRemark) AS DecisionRemark,
               LTRIM(RTRIM(ISNULL(RB.FirstName, '') + ' ' + ISNULL(RB.LastName, ''))) AS RaisedByName,
               LTRIM(RTRIM(ISNULL(AE.FirstName, '') + ' ' + ISNULL(AE.LastName, ''))) AS AssignedToName,
               LTRIM(RTRIM(ISNULL(RC.FirstName, '') + ' ' + ISNULL(RC.LastName, ''))) AS RecipientName
        FROM dbo.TKT_Ticket T
        LEFT JOIN dbo.ORG_Employee RB ON RB.EmployeeId = T.RaisedBy
        LEFT JOIN dbo.ORG_Employee AE ON AE.EmployeeId = T.AssignedTo
        LEFT JOIN dbo.ORG_Employee RC ON RC.EmployeeId = @EmployeeId
        WHERE T.TicketId = @TicketId;
        RETURN;
    END;

    /* ---- Access: raiser, assignee, or the assignee's Support Manager ---- */
    DECLARE @CanView BIT = 0, @IsApprover BIT = 0;
    DECLARE @TStatus VARCHAR(20), @TApproval VARCHAR(20), @TAssignedTo INT, @TRaisedBy INT, @TMode VARCHAR(10);
    -- Support Manager working his own ticket: nothing needs approval.
    DECLARE @SelfManaged BIT = 0;

    IF (@TicketId IS NOT NULL)
    BEGIN
        SELECT @TStatus = T.Status, @TApproval = T.ApprovalStatus, @TAssignedTo = T.AssignedTo,
               @TRaisedBy = T.RaisedBy, @TMode = T.ServiceMode,
               @SelfManaged = CASE WHEN T.IsRaisedByManager = 1 AND T.AssignedTo = T.RaisedBy THEN 1 ELSE 0 END,
               @IsApprover = CASE WHEN @IsManager = 1 AND E.Ref_ReportingManagerId = @EmployeeId THEN 1 ELSE 0 END
        FROM dbo.TKT_Ticket T
        JOIN dbo.ORG_Employee E ON E.EmployeeId = T.AssignedTo
        WHERE T.TicketId = @TicketId;

        IF (@TStatus IS NULL)
            THROW 50001, 'Ticket not found.', 1;

        IF (@TAssignedTo = @EmployeeId OR @TRaisedBy = @EmployeeId OR @IsApprover = 1)
            SET @CanView = 1;

        IF (@CanView = 0)
            THROW 50002, 'You are not allowed to access this ticket.', 1;
    END;

    /* ===================== CREATE_TICKET ===================== */
    IF (@Status = 'CREATE_TICKET')
    BEGIN
        IF (@CustomerSource NOT IN ('CRM','WMS') OR ISNULL(@SourceCustomerId, 0) <= 0 OR NULLIF(LTRIM(RTRIM(@CustomerName)), '') IS NULL)
            THROW 50010, 'Please select a company.', 1;
        IF (NULLIF(LTRIM(RTRIM(@Issue)), '') IS NULL)
            THROW 50011, 'Issue is required.', 1;
        SET @ServiceMode = NULLIF(LTRIM(RTRIM(@ServiceMode)), '');
        -- Manager assigning to a mapped employee: the employee chooses
        -- Online / Visit on the ticket later (SET_SERVICE_MODE).
        IF (@IsManager = 1 AND @AssignedTo IS NOT NULL AND @AssignedTo <> @EmployeeId)
            SET @ServiceMode = ISNULL(@ServiceMode, 'ONLINE');
        IF (ISNULL(@ServiceMode, '') NOT IN ('ONLINE','CARRY_IN','VISIT'))
            THROW 50012, 'Service mode must be Online, Carry In or Visit.', 1;
        IF (@ExpectedDate IS NULL OR @ExpectedDate < @Today)
            THROW 50013, 'Service expected date cannot be in the past.', 1;
        SET @Priority = ISNULL(NULLIF(@Priority, ''), 'MEDIUM');
        IF (@Priority NOT IN ('LOW','MEDIUM','HIGH'))
            THROW 50014, 'Invalid priority.', 1;
        SET @ServiceTypes = NULLIF(LTRIM(RTRIM(@ServiceTypes)), '');
        SET @ServiceTypeOther = NULLIF(LTRIM(RTRIM(@ServiceTypeOther)), '');
        IF (@ServiceTypes IS NULL)
            THROW 50016, 'Please select a service type.', 1;
        IF EXISTS (SELECT 1 FROM STRING_SPLIT(@ServiceTypes, ',')
                   WHERE LTRIM(RTRIM(value)) NOT IN ('WARRANTY','AMC','BILLABLE','TESTING','OTHER'))
            THROW 50017, 'Invalid service type.', 1;
        IF (',' + @ServiceTypes + ',' LIKE '%,OTHER,%' AND @ServiceTypeOther IS NULL)
            THROW 50018, 'Please enter the other service type.', 1;
        IF (',' + @ServiceTypes + ',' NOT LIKE '%,OTHER,%')
            SET @ServiceTypeOther = NULL;

        -- CRM source (lead organization): link the existing master customer when there is one.
        -- Lookup only, never inserts, so the selected customer is never saved twice.
        IF (@CustomerSource = 'CRM' AND @Ref_CustomerId IS NULL)
            SELECT TOP 1 @Ref_CustomerId = CustomerId
            FROM dbo.CUS_Customer
            WHERE Ref_LeadOrganizationId = @SourceCustomerId AND IsActive = 1
            ORDER BY CustomerId;

        DECLARE @ByManager BIT = 0;
        IF (@IsManager = 1)
        BEGIN
            -- Support Manager: assign to a mapped employee or to himself (no approval either way).
            IF (@AssignedTo IS NULL OR @AssignedTo = @EmployeeId)
                SET @AssignedTo = @EmployeeId;
            ELSE IF NOT EXISTS (SELECT 1 FROM dbo.ORG_Employee
                                WHERE EmployeeId = @AssignedTo AND Ref_ReportingManagerId = @EmployeeId AND IsActive = 1)
                THROW 50015, 'You can assign tickets only to your mapped employees.', 1;
            SET @ByManager = 1;
        END
        ELSE
            SET @AssignedTo = @EmployeeId;   -- employee raises his own ticket

        -- Employee-raised VISIT tickets need Support Manager approval.
        DECLARE @NewApproval VARCHAR(20) =
            CASE WHEN @ByManager = 0 AND @ServiceMode = 'VISIT' THEN 'PENDING' ELSE 'NOT_REQUIRED' END;
        -- Manager's own ticket: travel is auto approved.
        DECLARE @NewSelfManaged BIT = CASE WHEN @ByManager = 1 AND @AssignedTo = @EmployeeId THEN 1 ELSE 0 END;
        DECLARE @NewStatus VARCHAR(20) = CASE WHEN @NewApproval = 'PENDING' THEN 'APPROVAL_PENDING' ELSE 'OPEN' END;

        BEGIN TRAN;
            DECLARE @NextNo INT;
            SELECT @NextNo = ISNULL(MAX(TicketId), 0) + 1 FROM dbo.TKT_Ticket WITH (UPDLOCK, HOLDLOCK);

            INSERT INTO dbo.TKT_Ticket
            (
                TicketNo, CustomerSource, SourceCustomerId, Ref_CustomerId, CustomerName, Issue, Priority,
                ServiceMode, RaisedOn, ExpectedDate, RaisedBy, IsRaisedByManager, AssignedTo, Status,
                ApprovalStatus, CreatedOn, CreatedBy, ServiceTypes, ServiceTypeOther
            )
            VALUES
            (
                'TKT-' + RIGHT('0000' + CAST(@NextNo AS VARCHAR(10)), 4), @CustomerSource, @SourceCustomerId,
                @Ref_CustomerId, LTRIM(RTRIM(@CustomerName)), LTRIM(RTRIM(@Issue)), @Priority, @ServiceMode, @Now,
                @ExpectedDate, @EmployeeId, @ByManager, @AssignedTo, @NewStatus, @NewApproval, @Now, @EmployeeId,
                @ServiceTypes, @ServiceTypeOther
            );
            DECLARE @NewTicketId INT = SCOPE_IDENTITY();

            -- Requirement / Offering items with serial numbers.
            IF (@ItemsJson IS NOT NULL)
                INSERT INTO dbo.TKT_TicketItem
                    (Ref_TicketId, Ref_ProductCategoryId, CategoryName, Ref_OfferingId, OfferingName,
                     Ref_ProductId, ProductName, SerialNo, ItemRemark, CreatedOn, CreatedBy)
                SELECT @NewTicketId, J.categoryId, J.categoryName, J.offeringId, J.offeringName,
                       J.productId, J.productName, NULLIF(LTRIM(RTRIM(J.serialNo)), ''),
                       NULLIF(LTRIM(RTRIM(J.remark)), ''), @Now, @EmployeeId
                FROM OPENJSON(@ItemsJson) WITH (
                        categoryId INT, categoryName NVARCHAR(200), offeringId INT, offeringName NVARCHAR(200),
                        productId BIGINT, productName NVARCHAR(255), serialNo NVARCHAR(100), remark NVARCHAR(500)) J
                WHERE J.categoryId IS NOT NULL AND J.productId IS NOT NULL;

            -- Travel request sent together with an employee-raised Visit ticket.
            IF (@ExpensesJson IS NOT NULL)
                INSERT INTO dbo.TKT_TicketExpense
                    (Ref_TicketId, ExpenseSource, Category, TravelMode, FromLocation, ToLocation, Km, Amount, Remarks, ApprovalStatus, CreatedOn, CreatedBy)
                SELECT @NewTicketId, 'TRAVEL', ISNULL(J.category, CASE WHEN J.travelMode = 'STAY' THEN 'STAY' WHEN J.travelMode = 'OTHER' THEN 'OTHER' ELSE 'TRANSPORT' END),
                       J.travelMode, J.fromLocation, J.toLocation, J.km, ISNULL(J.amount, 0), J.remarks,
                       CASE WHEN A.TravelMode IS NULL OR @NewSelfManaged = 1 THEN 'AUTO_OK' ELSE 'PENDING' END, @Now, @EmployeeId
                FROM OPENJSON(@ExpensesJson) WITH (
                        category VARCHAR(20), travelMode VARCHAR(20), fromLocation NVARCHAR(200), toLocation NVARCHAR(200),
                        km DECIMAL(9,2), amount DECIMAL(12,2), remarks NVARCHAR(500)) J
                LEFT JOIN @ApprovalModes A ON A.TravelMode = J.travelMode;

            INSERT INTO dbo.TKT_TicketStatusLog (Ref_TicketId, FromStatus, ToStatus, Remarks, CreatedOn, CreatedBy)
            VALUES (@NewTicketId, NULL, @NewStatus, 'Ticket raised', @Now, @EmployeeId);
        COMMIT;

        SELECT 1 AS Status, 'Ticket raised successfully.' AS Message, T.TicketId, T.TicketNo, T.Status AS TicketStatus,
               T.AssignedTo, T.RaisedBy, T.ApprovalStatus,
               (SELECT Ref_ReportingManagerId FROM dbo.ORG_Employee WHERE EmployeeId = T.AssignedTo) AS ApproverId
        FROM dbo.TKT_Ticket T WHERE T.TicketId = @NewTicketId;
        RETURN;
    END;

    /* ===================== GET_TICKETS ===================== */
    IF (@Status = 'GET_TICKETS')
    BEGIN
        SELECT T.TicketId, T.TicketNo, T.CustomerSource, T.SourceCustomerId, T.CustomerName, T.Issue, T.Priority,
               T.ServiceMode, T.RaisedOn, T.ExpectedDate, T.Status, T.ApprovalStatus, T.IsRaisedByManager,
               T.RaisedBy, LTRIM(RTRIM(ISNULL(RB.FirstName, '') + ' ' + ISNULL(RB.LastName, ''))) AS RaisedByName,
               T.AssignedTo, LTRIM(RTRIM(ISNULL(AE.FirstName, '') + ' ' + ISNULL(AE.LastName, ''))) AS AssignedToName,
               T.CheckInOn, T.CompletedOn,
               (SELECT COUNT(*) FROM dbo.TKT_TicketExpense X WHERE X.Ref_TicketId = T.TicketId AND X.ApprovalStatus = 'PENDING') AS PendingExpenseCount
        FROM dbo.TKT_Ticket T
        JOIN dbo.ORG_Employee AE ON AE.EmployeeId = T.AssignedTo
        LEFT JOIN dbo.ORG_Employee RB ON RB.EmployeeId = T.RaisedBy
        WHERE (@ListType = 'ASSIGNED' AND T.AssignedTo = @EmployeeId)
           OR (@ListType = 'RAISED' AND T.RaisedBy = @EmployeeId)
           OR (@ListType = 'TEAM_OPEN' AND @IsManager = 1 AND AE.Ref_ReportingManagerId = @EmployeeId
               AND T.Status IN ('OPEN','APPROVAL_PENDING','IN_PROGRESS','CLOSE_REQUESTED'))
           OR (@ListType = 'TEAM_CLOSED' AND @IsManager = 1 AND AE.Ref_ReportingManagerId = @EmployeeId
               AND T.Status IN ('COMPLETED','REJECTED','CANCELLED'))
        ORDER BY CASE WHEN T.Status IN ('COMPLETED','REJECTED','CANCELLED') THEN 1 ELSE 0 END, T.ExpectedDate, T.TicketId DESC;
        RETURN;
    END;

    /* ===================== MY_COUNTS (My Tickets tiles) ===================== */
   IF (@Status = 'MY_COUNTS')
BEGIN
    SELECT
        ISNULL(SUM(CASE WHEN T.Status IN ('OPEN','IN_PROGRESS','CLOSE_REQUESTED') THEN 1 ELSE 0 END), 0) AS OpenCount,
        ISNULL(SUM(CASE WHEN T.Status = 'APPROVAL_PENDING' OR PX.HasPending = 1 THEN 1 ELSE 0 END), 0) AS AwaitingApprovalCount,
        ISNULL(SUM(CASE WHEN T.Status = 'COMPLETED' THEN 1 ELSE 0 END), 0) AS CompletedCount
    FROM dbo.TKT_Ticket T
    OUTER APPLY (SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.TKT_TicketExpense X
                                          WHERE X.Ref_TicketId = T.TicketId AND X.ApprovalStatus = 'PENDING')
                             THEN 1 ELSE 0 END AS HasPending) PX
    WHERE (@ListType = 'RAISED' AND T.RaisedBy = @EmployeeId)
       OR (ISNULL(@ListType, 'ASSIGNED') = 'ASSIGNED' AND T.AssignedTo = @EmployeeId);
    RETURN;
END;

    /* ===================== TICKET_DETAILS ===================== */
    IF (@Status = 'TICKET_DETAILS')
    BEGIN
        -- T0 ticket
        SELECT T.*, LTRIM(RTRIM(ISNULL(RB.FirstName, '') + ' ' + ISNULL(RB.LastName, ''))) AS RaisedByName,
               LTRIM(RTRIM(ISNULL(AE.FirstName, '') + ' ' + ISNULL(AE.LastName, ''))) AS AssignedToName,
               LTRIM(RTRIM(ISNULL(AP.FirstName, '') + ' ' + ISNULL(AP.LastName, ''))) AS ApprovedByName,
               @IsApprover AS CanApprove,
               CASE WHEN T.AssignedTo = @EmployeeId THEN 1 ELSE 0 END AS IsAssignee,
               CASE WHEN T.RaisedBy = @EmployeeId THEN 1 ELSE 0 END AS IsRaiser,
               (SELECT COUNT(*) FROM dbo.TKT_TicketExpense X WHERE X.Ref_TicketId = T.TicketId AND X.ApprovalStatus = 'PENDING') AS PendingExpenseCount,
               (SELECT ISNULL(SUM(X.Amount), 0) FROM dbo.TKT_TicketExpense X WHERE X.Ref_TicketId = T.TicketId AND X.ApprovalStatus <> 'REJECTED') AS TotalExpenseAmount
        FROM dbo.TKT_Ticket T
        JOIN dbo.ORG_Employee AE ON AE.EmployeeId = T.AssignedTo
        LEFT JOIN dbo.ORG_Employee RB ON RB.EmployeeId = T.RaisedBy
        LEFT JOIN dbo.ORG_Employee AP ON AP.EmployeeId = T.ApprovedBy
        WHERE T.TicketId = @TicketId;

        -- T1 activities
        SELECT A.*, (SELECT COUNT(*) FROM dbo.TKT_TicketExpense X WHERE X.Ref_TicketActivityId = A.TicketActivityId) AS ExpenseCount
        FROM dbo.TKT_TicketActivity A WHERE A.Ref_TicketId = @TicketId ORDER BY A.ActivityOn DESC, A.TicketActivityId DESC;

        -- T2 expenses
        SELECT X.*, A.Title AS ActivityTitle, A.ActivityOn,
               LTRIM(RTRIM(ISNULL(AP.FirstName, '') + ' ' + ISNULL(AP.LastName, ''))) AS ApprovedByName,
               LTRIM(RTRIM(ISNULL(CB.FirstName, '') + ' ' + ISNULL(CB.LastName, ''))) AS CreatedByName
        FROM dbo.TKT_TicketExpense X
        LEFT JOIN dbo.TKT_TicketActivity A ON A.TicketActivityId = X.Ref_TicketActivityId
        LEFT JOIN dbo.ORG_Employee AP ON AP.EmployeeId = X.ApprovedBy
        LEFT JOIN dbo.ORG_Employee CB ON CB.EmployeeId = X.CreatedBy
        WHERE X.Ref_TicketId = @TicketId ORDER BY X.TicketExpenseId;

        -- T3 attachments
        SELECT * FROM dbo.TKT_TicketAttachment WHERE Ref_TicketId = @TicketId ORDER BY TicketAttachmentId;

        -- T4 status log
        SELECT L.*, LTRIM(RTRIM(ISNULL(E.FirstName, '') + ' ' + ISNULL(E.LastName, ''))) AS CreatedByName
        FROM dbo.TKT_TicketStatusLog L LEFT JOIN dbo.ORG_Employee E ON E.EmployeeId = L.CreatedBy
        WHERE L.Ref_TicketId = @TicketId ORDER BY L.TicketStatusLogId;

        -- T5 requirement / offering items
        SELECT * FROM dbo.TKT_TicketItem WHERE Ref_TicketId = @TicketId ORDER BY TicketItemId;
        RETURN;
    END;

    /* ===================== SEND_FOR_APPROVAL (assignee, travel request) ===================== */
    IF (@Status = 'SEND_FOR_APPROVAL')
    BEGIN
        IF (@TAssignedTo <> @EmployeeId)
            THROW 50020, 'Only the assigned employee can send a travel request.', 1;
        IF (@TStatus NOT IN ('OPEN','IN_PROGRESS','APPROVAL_PENDING'))
            THROW 50021, 'Travel request is not allowed in the current ticket status.', 1;
        IF (@ExpensesJson IS NULL OR NOT EXISTS (SELECT 1 FROM OPENJSON(@ExpensesJson)))
            THROW 50022, 'Please select a travel mode.', 1;

        BEGIN TRAN;
            UPDATE dbo.TKT_Ticket SET ServiceMode = 'VISIT', UpdatedOn = @Now, UpdatedBy = @EmployeeId WHERE TicketId = @TicketId;

            INSERT INTO dbo.TKT_TicketExpense
                (Ref_TicketId, ExpenseSource, Category, TravelMode, FromLocation, ToLocation, Km, Amount, Remarks, ApprovalStatus, CreatedOn, CreatedBy)
            SELECT @TicketId, 'TRAVEL', ISNULL(J.category, CASE WHEN J.travelMode = 'STAY' THEN 'STAY' WHEN J.travelMode = 'OTHER' THEN 'OTHER' ELSE 'TRANSPORT' END),
                   J.travelMode, J.fromLocation, J.toLocation, J.km, ISNULL(J.amount, 0), J.remarks,
                   CASE WHEN A.TravelMode IS NULL OR @SelfManaged = 1 THEN 'AUTO_OK' ELSE 'PENDING' END, @Now, @EmployeeId
            FROM OPENJSON(@ExpensesJson) WITH (
                    category VARCHAR(20), travelMode VARCHAR(20), fromLocation NVARCHAR(200), toLocation NVARCHAR(200),
                    km DECIMAL(9,2), amount DECIMAL(12,2), remarks NVARCHAR(500)) J
            LEFT JOIN @ApprovalModes A ON A.TravelMode = J.travelMode;

            IF EXISTS (SELECT 1 FROM dbo.TKT_TicketExpense WHERE Ref_TicketId = @TicketId AND ApprovalStatus = 'PENDING')
               AND @TStatus <> 'APPROVAL_PENDING'
            BEGIN
                UPDATE dbo.TKT_Ticket SET Status = 'APPROVAL_PENDING' WHERE TicketId = @TicketId;
                INSERT INTO dbo.TKT_TicketStatusLog (Ref_TicketId, FromStatus, ToStatus, Remarks, CreatedOn, CreatedBy)
                VALUES (@TicketId, @TStatus, 'APPROVAL_PENDING', 'Travel request sent for approval', @Now, @EmployeeId);
            END;
        COMMIT;

        SELECT 1 AS Status,
               CASE WHEN EXISTS (SELECT 1 FROM dbo.TKT_TicketExpense WHERE Ref_TicketId = @TicketId AND ApprovalStatus = 'PENDING')
                    THEN 'Sent to Support Manager for approval.' ELSE 'Travel saved (no approval needed).' END AS Message,
               @TicketId AS TicketId, T.TicketNo, T.Status AS TicketStatus, T.CustomerName,
               CASE WHEN EXISTS (SELECT 1 FROM dbo.TKT_TicketExpense WHERE Ref_TicketId = @TicketId AND ApprovalStatus = 'PENDING') THEN 1 ELSE 0 END AS HasPending,
               (SELECT Ref_ReportingManagerId FROM dbo.ORG_Employee WHERE EmployeeId = T.AssignedTo) AS ApproverId
        FROM dbo.TKT_Ticket T WHERE T.TicketId = @TicketId;
        RETURN;
    END;

    /* ===================== APPROVE_TICKET (Support Manager) =====================
       Approves/rejects the ticket approval and every PENDING expense on it. */
    IF (@Status = 'APPROVE_TICKET')
    BEGIN
        IF (@IsApprover = 0)
            THROW 50030, 'Only the Support Manager of the assigned employee can approve.', 1;
        IF (@Decision NOT IN ('APPROVE','REJECT'))
            THROW 50031, 'Invalid decision.', 1;
        IF (@Decision = 'REJECT' AND NULLIF(LTRIM(RTRIM(@Remarks)), '') IS NULL)
            THROW 50032, 'Please enter a reject reason.', 1;
        IF (@TApproval <> 'PENDING' AND NOT EXISTS (SELECT 1 FROM dbo.TKT_TicketExpense WHERE Ref_TicketId = @TicketId AND ApprovalStatus = 'PENDING'))
            THROW 50033, 'Nothing is pending approval on this ticket.', 1;

        DECLARE @NewExpStatus VARCHAR(20) = CASE WHEN @Decision = 'APPROVE' THEN 'APPROVED' ELSE 'REJECTED' END;
        DECLARE @AfterStatus VARCHAR(20);

        BEGIN TRAN;
            UPDATE dbo.TKT_TicketExpense
            SET ApprovalStatus = @NewExpStatus, ApprovedBy = @EmployeeId, ApprovedOn = @Now, ApprovalRemark = @Remarks
            WHERE Ref_TicketId = @TicketId AND ApprovalStatus = 'PENDING';

            -- Rejecting an employee-raised Visit ticket rejects the ticket;
            -- rejecting only a travel request keeps the ticket open.
            SET @AfterStatus =
                CASE
                    WHEN @TApproval = 'PENDING' AND @Decision = 'REJECT' THEN 'REJECTED'
                    WHEN @TStatus = 'APPROVAL_PENDING' THEN CASE WHEN EXISTS (SELECT 1 FROM dbo.TKT_Ticket WHERE TicketId = @TicketId AND CheckInOn IS NOT NULL) THEN 'IN_PROGRESS' ELSE 'OPEN' END
                    ELSE @TStatus
                END;

            UPDATE dbo.TKT_Ticket
            SET ApprovalStatus = CASE WHEN ApprovalStatus = 'PENDING' THEN @NewExpStatus ELSE ApprovalStatus END,
                ApprovedBy = @EmployeeId, ApprovedOn = @Now, ApprovalRemark = @Remarks,
                Status = @AfterStatus, UpdatedOn = @Now, UpdatedBy = @EmployeeId
            WHERE TicketId = @TicketId;

            INSERT INTO dbo.TKT_TicketStatusLog (Ref_TicketId, FromStatus, ToStatus, Remarks, CreatedOn, CreatedBy)
            VALUES (@TicketId, @TStatus, @AfterStatus, CASE WHEN @Decision = 'APPROVE' THEN 'Approved' ELSE 'Rejected: ' + @Remarks END, @Now, @EmployeeId);
        COMMIT;

        SELECT 1 AS Status, CASE WHEN @Decision = 'APPROVE' THEN 'Approved successfully.' ELSE 'Rejected.' END AS Message,
               @TicketId AS TicketId, T.TicketNo, T.Status AS TicketStatus, T.AssignedTo
        FROM dbo.TKT_Ticket T WHERE T.TicketId = @TicketId;
        RETURN;
    END;

    /* ===================== APPROVE_EXPENSE (Support Manager, one expense) ===================== */
    IF (@Status = 'APPROVE_EXPENSE')
    BEGIN
        IF (@IsApprover = 0)
            THROW 50040, 'Only the Support Manager of the assigned employee can approve.', 1;
        IF (@Decision NOT IN ('APPROVE','REJECT'))
            THROW 50041, 'Invalid decision.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.TKT_TicketExpense WHERE TicketExpenseId = @TicketExpenseId AND Ref_TicketId = @TicketId AND ApprovalStatus = 'PENDING')
            THROW 50042, 'This expense is not pending approval.', 1;

        BEGIN TRAN;
            UPDATE dbo.TKT_TicketExpense
            SET ApprovalStatus = CASE WHEN @Decision = 'APPROVE' THEN 'APPROVED' ELSE 'REJECTED' END,
                ApprovedBy = @EmployeeId, ApprovedOn = @Now, ApprovalRemark = @Remarks
            WHERE TicketExpenseId = @TicketExpenseId;

            -- Last pending item decided: release the ticket.
            IF (@TStatus = 'APPROVAL_PENDING' AND @TApproval <> 'PENDING'
                AND NOT EXISTS (SELECT 1 FROM dbo.TKT_TicketExpense WHERE Ref_TicketId = @TicketId AND ApprovalStatus = 'PENDING'))
            BEGIN
                DECLARE @Released VARCHAR(20) = CASE WHEN EXISTS (SELECT 1 FROM dbo.TKT_Ticket WHERE TicketId = @TicketId AND CheckInOn IS NOT NULL) THEN 'IN_PROGRESS' ELSE 'OPEN' END;
                UPDATE dbo.TKT_Ticket SET Status = @Released, UpdatedOn = @Now, UpdatedBy = @EmployeeId WHERE TicketId = @TicketId;
                INSERT INTO dbo.TKT_TicketStatusLog (Ref_TicketId, FromStatus, ToStatus, Remarks, CreatedOn, CreatedBy)
                VALUES (@TicketId, @TStatus, @Released, 'All expenses decided', @Now, @EmployeeId);
            END;
        COMMIT;

        SELECT 1 AS Status, CASE WHEN @Decision = 'APPROVE' THEN 'Expense approved.' ELSE 'Expense rejected.' END AS Message,
               @TicketId AS TicketId, T.TicketNo, T.Status AS TicketStatus, T.AssignedTo
        FROM dbo.TKT_Ticket T WHERE T.TicketId = @TicketId;
        RETURN;
    END;

    /* ===================== CHECK_IN (assignee at client site) ===================== */
    IF (@Status = 'CHECK_IN')
    BEGIN
        IF (@TAssignedTo <> @EmployeeId)
            THROW 50050, 'Only the assigned employee can check in.', 1;
        IF (@TStatus NOT IN ('OPEN','IN_PROGRESS'))
            THROW 50051, 'Check-in is allowed only after approval.', 1;
        IF (@Latitude IS NULL OR @Longitude IS NULL)
            THROW 50052, 'Location is required for check-in.', 1;

        BEGIN TRAN;
            UPDATE dbo.TKT_Ticket
            SET CheckInOn = @Now, CheckInLatitude = @Latitude, CheckInLongitude = @Longitude, CheckInAddress = @Address,
                Status = 'IN_PROGRESS', UpdatedOn = @Now, UpdatedBy = @EmployeeId
            WHERE TicketId = @TicketId;

            IF (@TStatus <> 'IN_PROGRESS')
                INSERT INTO dbo.TKT_TicketStatusLog (Ref_TicketId, FromStatus, ToStatus, Remarks, CreatedOn, CreatedBy)
                VALUES (@TicketId, @TStatus, 'IN_PROGRESS', 'Checked in: ' + ISNULL(@Address, ''), @Now, @EmployeeId);
        COMMIT;

        SELECT 1 AS Status, 'Checked in.' AS Message, @TicketId AS TicketId, 'IN_PROGRESS' AS TicketStatus;
        RETURN;
    END;

    /* ===================== CHECK_OUT (after check-in) ===================== */
    IF (@Status = 'CHECK_OUT')
    BEGIN
        IF (@TAssignedTo <> @EmployeeId)
            THROW 50055, 'Only the assigned employee can check out.', 1;
        IF (@TStatus <> 'IN_PROGRESS'
            OR NOT EXISTS (SELECT 1 FROM dbo.TKT_Ticket WHERE TicketId = @TicketId AND CheckInOn IS NOT NULL))
            THROW 50056, 'Please check in first.', 1;
        IF EXISTS (SELECT 1 FROM dbo.TKT_Ticket WHERE TicketId = @TicketId AND CheckOutOn IS NOT NULL)
            THROW 50057, 'Already checked out.', 1;
        IF (@Latitude IS NULL OR @Longitude IS NULL)
            THROW 50058, 'Location is required for check-out.', 1;

        BEGIN TRAN;
            UPDATE dbo.TKT_Ticket
            SET CheckOutOn = @Now, CheckOutLatitude = @Latitude, CheckOutLongitude = @Longitude, CheckOutAddress = @Address,
                UpdatedOn = @Now, UpdatedBy = @EmployeeId
            WHERE TicketId = @TicketId;

            INSERT INTO dbo.TKT_TicketStatusLog (Ref_TicketId, FromStatus, ToStatus, Remarks, CreatedOn, CreatedBy)
            VALUES (@TicketId, @TStatus, @TStatus, 'Checked out: ' + ISNULL(@Address, ''), @Now, @EmployeeId);
        COMMIT;

        SELECT 1 AS Status, 'Checked out.' AS Message, @TicketId AS TicketId;
        RETURN;
    END;

    /* ===================== COMPLETE_ACTIVITY (planned -> completed) ===================== */
    IF (@Status = 'COMPLETE_ACTIVITY')
    BEGIN
        IF (@TAssignedTo <> @EmployeeId)
            THROW 50065, 'Only the assigned employee can update activities.', 1;
        IF (@TStatus IN ('COMPLETED','REJECTED','CANCELLED','CLOSE_REQUESTED'))
            THROW 50066, 'Ticket is closed.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.TKT_TicketActivity
                       WHERE TicketActivityId = @TicketActivityId AND Ref_TicketId = @TicketId AND ActivityStatus = 'PLANNED')
            THROW 50067, 'Planned activity not found.', 1;

        UPDATE dbo.TKT_TicketActivity
        SET ActivityStatus = 'COMPLETED', CompletedOn = @Now,
            Remarks = ISNULL(NULLIF(LTRIM(RTRIM(@Remarks)), ''), Remarks)
        WHERE TicketActivityId = @TicketActivityId;

        SELECT 1 AS Status, 'Activity completed.' AS Message, @TicketId AS TicketId, @TicketActivityId AS TicketActivityId;
        RETURN;
    END;

    /* ===================== DECIDE_CLOSE (raiser approves / rejects a close request) ===================== */
    IF (@Status = 'DECIDE_CLOSE')
    BEGIN
        IF (@TRaisedBy <> @EmployeeId)
            THROW 50085, 'Only the person who raised the ticket can close it.', 1;
        IF (@TStatus <> 'CLOSE_REQUESTED')
            THROW 50086, 'There is no close request on this ticket.', 1;
        IF (@Decision NOT IN ('APPROVE','REJECT'))
            THROW 50087, 'Decision must be Approve or Reject.', 1;
        IF (@Decision = 'REJECT' AND NULLIF(LTRIM(RTRIM(@Remarks)), '') IS NULL)
            THROW 50088, 'Please enter the reason.', 1;

        DECLARE @CloseTo VARCHAR(20) = CASE WHEN @Decision = 'APPROVE' THEN 'COMPLETED' ELSE 'IN_PROGRESS' END;
        BEGIN TRAN;
            UPDATE dbo.TKT_Ticket
            SET Status = @CloseTo,
                CompletedOn = CASE WHEN @Decision = 'APPROVE' THEN @Now ELSE NULL END,
                CloseDecisionRemark = NULLIF(LTRIM(RTRIM(@Remarks)), ''),
                UpdatedOn = @Now, UpdatedBy = @EmployeeId
            WHERE TicketId = @TicketId;

            INSERT INTO dbo.TKT_TicketStatusLog (Ref_TicketId, FromStatus, ToStatus, Remarks, CreatedOn, CreatedBy)
            VALUES (@TicketId, @TStatus, @CloseTo,
                    CASE WHEN @Decision = 'APPROVE' THEN 'Close request approved' ELSE 'Close request rejected: ' + LTRIM(RTRIM(@Remarks)) END,
                    @Now, @EmployeeId);
        COMMIT;

        SELECT 1 AS Status,
               CASE WHEN @Decision = 'APPROVE' THEN 'Ticket closed.' ELSE 'Close request rejected.' END AS Message,
               @TicketId AS TicketId, T.TicketNo, T.Status AS TicketStatus, T.AssignedTo
        FROM dbo.TKT_Ticket T WHERE T.TicketId = @TicketId;
        RETURN;
    END;

    /* ===================== REASSIGN_TICKET (raiser, on a close request) ===================== */
    IF (@Status = 'REASSIGN_TICKET')
    BEGIN
        IF (@TRaisedBy <> @EmployeeId OR @IsManager = 0)
            THROW 50095, 'Only the Support Manager who raised the ticket can reassign it.', 1;
        IF (@TStatus <> 'CLOSE_REQUESTED')
            THROW 50096, 'Ticket can be reassigned only on a close request.', 1;
        IF (@AssignedTo IS NULL OR @AssignedTo = @TAssignedTo)
            THROW 50097, 'Please select a different employee.', 1;
        IF (@AssignedTo <> @EmployeeId AND NOT EXISTS (SELECT 1 FROM dbo.ORG_Employee
                        WHERE EmployeeId = @AssignedTo AND Ref_ReportingManagerId = @EmployeeId AND IsActive = 1))
            THROW 50098, 'You can assign tickets only to your mapped employees.', 1;

        BEGIN TRAN;
            -- New assignee starts fresh: check-in / check-out / resolution are cleared.
            UPDATE dbo.TKT_Ticket
            SET AssignedTo = @AssignedTo, Status = 'OPEN',
                CheckInOn = NULL, CheckInLatitude = NULL, CheckInLongitude = NULL, CheckInAddress = NULL,
                CheckOutOn = NULL, CheckOutLatitude = NULL, CheckOutLongitude = NULL, CheckOutAddress = NULL,
                ResolutionRemarks = NULL, CloseRequestedOn = NULL,
                CloseDecisionRemark = NULLIF(LTRIM(RTRIM(@Remarks)), ''),
                UpdatedOn = @Now, UpdatedBy = @EmployeeId
            WHERE TicketId = @TicketId;

            INSERT INTO dbo.TKT_TicketStatusLog (Ref_TicketId, FromStatus, ToStatus, Remarks, CreatedOn, CreatedBy)
            SELECT @TicketId, @TStatus, 'OPEN',
                   'Reassigned to ' + LTRIM(RTRIM(ISNULL(E.FirstName, '') + ' ' + ISNULL(E.LastName, '')))
                   + ISNULL(': ' + NULLIF(LTRIM(RTRIM(@Remarks)), ''), ''),
                   @Now, @EmployeeId
            FROM dbo.ORG_Employee E WHERE E.EmployeeId = @AssignedTo;
        COMMIT;

        SELECT 1 AS Status, 'Ticket reassigned.' AS Message, @TicketId AS TicketId, T.TicketNo, T.CustomerName,
               T.AssignedTo, @TAssignedTo AS PreviousAssignedTo
        FROM dbo.TKT_Ticket T WHERE T.TicketId = @TicketId;
        RETURN;
    END;

    /* ===================== ADD_ACTIVITY ===================== */
    IF (@Status = 'ADD_ACTIVITY')
    BEGIN
        IF (@TAssignedTo <> @EmployeeId)
            THROW 50060, 'Only the assigned employee can add activities.', 1;
        IF (@TStatus IN ('COMPLETED','REJECTED','CANCELLED','CLOSE_REQUESTED'))
            THROW 50061, 'Ticket is closed.', 1;
        IF (@ActivityType NOT IN ('SITE_VISIT','CALL','ONLINE'))
            THROW 50062, 'Invalid activity type.', 1;
        IF (NULLIF(LTRIM(RTRIM(@Title)), '') IS NULL)
            THROW 50063, 'Title is required.', 1;
        SET @ActivityStatus = ISNULL(NULLIF(@ActivityStatus, ''), 'COMPLETED');

        INSERT INTO dbo.TKT_TicketActivity
            (Ref_TicketId, ActivityType, Title, ActivityOn, Location, LocationAddress, Latitude, Longitude, ContactPerson, Remarks, ActivityStatus, CreatedOn, CreatedBy)
        VALUES
            (@TicketId, @ActivityType, LTRIM(RTRIM(@Title)), ISNULL(@ActivityOn, @Now),
             -- Location = short place name (first part), LocationAddress = full address.
             LEFT(LTRIM(RTRIM(CASE WHEN CHARINDEX(',', @Address) > 0 THEN LEFT(@Address, CHARINDEX(',', @Address) - 1) ELSE @Address END)), 500),
             @Address, @Latitude, @Longitude,
             @ContactPerson, @Remarks, @ActivityStatus, @Now, @EmployeeId);

        SELECT 1 AS Status, 'Activity saved.' AS Message, @TicketId AS TicketId, SCOPE_IDENTITY() AS TicketActivityId;
        RETURN;
    END;

    /* ===================== ADD_EXPENSE (visit expense on an activity) ===================== */
    IF (@Status = 'ADD_EXPENSE')
    BEGIN
        IF (@TAssignedTo <> @EmployeeId)
            THROW 50070, 'Only the assigned employee can add expenses.', 1;
        IF (@TStatus IN ('COMPLETED','REJECTED','CANCELLED'))
            THROW 50071, 'Ticket is closed.', 1;
        IF (@ExpensesJson IS NULL OR NOT EXISTS (SELECT 1 FROM OPENJSON(@ExpensesJson)))
            THROW 50072, 'Expense details are required.', 1;
        IF EXISTS (SELECT 1 FROM OPENJSON(@ExpensesJson) WITH (ticketActivityId INT) J
                   WHERE J.ticketActivityId IS NOT NULL
                     AND NOT EXISTS (SELECT 1 FROM dbo.TKT_TicketActivity A WHERE A.TicketActivityId = J.ticketActivityId AND A.Ref_TicketId = @TicketId))
            THROW 50073, 'Activity does not belong to this ticket.', 1;

        BEGIN TRAN;
            INSERT INTO dbo.TKT_TicketExpense
                (Ref_TicketId, Ref_TicketActivityId, ExpenseSource, Category, TravelMode, FromLocation, ToLocation, Km, Amount,
                 Remarks, ReceiptFileName, ReceiptPath, ApprovalStatus, CreatedOn, CreatedBy)
            SELECT @TicketId, J.ticketActivityId, 'VISIT',
                   ISNULL(J.category, CASE WHEN J.travelMode = 'STAY' THEN 'STAY' WHEN J.travelMode = 'OTHER' THEN 'OTHER' ELSE 'TRANSPORT' END),
                   J.travelMode, J.fromLocation, J.toLocation, J.km, ISNULL(J.amount, 0), J.remarks, J.receiptFileName, J.receiptPath,
                   CASE WHEN A.TravelMode IS NULL OR @SelfManaged = 1 THEN 'AUTO_OK' ELSE 'PENDING' END, @Now, @EmployeeId
            FROM OPENJSON(@ExpensesJson) WITH (
                    ticketActivityId INT, category VARCHAR(20), travelMode VARCHAR(20), fromLocation NVARCHAR(200),
                    toLocation NVARCHAR(200), km DECIMAL(9,2), amount DECIMAL(12,2), remarks NVARCHAR(500),
                    receiptFileName NVARCHAR(255), receiptPath NVARCHAR(500)) J
            LEFT JOIN @ApprovalModes A ON A.TravelMode = J.travelMode;
        COMMIT;

        DECLARE @Pending BIT = CASE WHEN EXISTS (SELECT 1 FROM dbo.TKT_TicketExpense WHERE Ref_TicketId = @TicketId AND ApprovalStatus = 'PENDING') THEN 1 ELSE 0 END;
        SELECT 1 AS Status,
               CASE WHEN @Pending = 1 THEN 'Expense added. Waiting for Support Manager approval.' ELSE 'Expense added.' END AS Message,
               @TicketId AS TicketId, T.TicketNo, @Pending AS HasPending,
               (SELECT Ref_ReportingManagerId FROM dbo.ORG_Employee WHERE EmployeeId = T.AssignedTo) AS ApproverId
        FROM dbo.TKT_Ticket T WHERE T.TicketId = @TicketId;
        RETURN;
    END;

    /* ===================== COMPLETE_TICKET ===================== */
    IF (@Status = 'COMPLETE_TICKET')
    BEGIN
        IF (@TAssignedTo <> @EmployeeId)
            THROW 50080, 'Only the assigned employee can complete the ticket.', 1;
        IF (@TStatus IN ('COMPLETED','REJECTED','CANCELLED'))
            THROW 50081, 'Ticket is already closed.', 1;
        IF (@TStatus = 'CLOSE_REQUESTED')
            THROW 50089, 'Close request is already sent.', 1;
        IF (@TApproval = 'PENDING' OR @TStatus = 'APPROVAL_PENDING'
            OR EXISTS (SELECT 1 FROM dbo.TKT_TicketExpense WHERE Ref_TicketId = @TicketId AND ApprovalStatus = 'PENDING'))
            THROW 50082, 'Complete is locked until the Support Manager approves.', 1;
        IF (@TMode = 'VISIT' AND NOT EXISTS (SELECT 1 FROM dbo.TKT_Ticket WHERE TicketId = @TicketId AND CheckInOn IS NOT NULL))
            THROW 50083, 'Please check in at the client site first.', 1;
        IF EXISTS (SELECT 1 FROM dbo.TKT_Ticket WHERE TicketId = @TicketId AND CheckInOn IS NOT NULL AND CheckOutOn IS NULL)
            THROW 50092, 'Please check out first.', 1;
        IF EXISTS (SELECT 1 FROM dbo.TKT_TicketActivity WHERE Ref_TicketId = @TicketId AND ActivityStatus = 'PLANNED')
            THROW 50093, 'Please complete all planned activities first.', 1;
        IF (NULLIF(LTRIM(RTRIM(@Remarks)), '') IS NULL)
            THROW 50084, 'Resolution remarks are required.', 1;

        -- Raised by someone else (manager): the assignee only requests closing;
        -- the raiser approves it (DECIDE_CLOSE). Own ticket: closed directly.
        DECLARE @CompleteTo VARCHAR(20) = CASE WHEN @TRaisedBy = @EmployeeId THEN 'COMPLETED' ELSE 'CLOSE_REQUESTED' END;

        BEGIN TRAN;
            UPDATE dbo.TKT_Ticket
            SET Status = @CompleteTo, ResolutionRemarks = LTRIM(RTRIM(@Remarks)),
                CompletedOn = CASE WHEN @CompleteTo = 'COMPLETED' THEN @Now ELSE NULL END,
                CloseRequestedOn = CASE WHEN @CompleteTo = 'CLOSE_REQUESTED' THEN @Now ELSE NULL END,
                CloseDecisionRemark = NULL,
                UpdatedOn = @Now, UpdatedBy = @EmployeeId
            WHERE TicketId = @TicketId;

            IF (@AttachmentsJson IS NOT NULL)
                INSERT INTO dbo.TKT_TicketAttachment (Ref_TicketId, AttachmentType, FileName, FilePath, CreatedOn, CreatedBy)
                SELECT @TicketId, ISNULL(J.attachmentType, 'OTHER'), J.fileName, J.filePath, @Now, @EmployeeId
                FROM OPENJSON(@AttachmentsJson) WITH (attachmentType VARCHAR(20), fileName NVARCHAR(255), filePath NVARCHAR(500)) J
                WHERE J.filePath IS NOT NULL;

            INSERT INTO dbo.TKT_TicketStatusLog (Ref_TicketId, FromStatus, ToStatus, Remarks, CreatedOn, CreatedBy)
            VALUES (@TicketId, @TStatus, @CompleteTo,
                    CASE WHEN @CompleteTo = 'COMPLETED' THEN 'Ticket completed' ELSE 'Close request sent' END, @Now, @EmployeeId);
        COMMIT;

        SELECT 1 AS Status,
               CASE WHEN @CompleteTo = 'COMPLETED' THEN 'Ticket completed.' ELSE 'Close request sent.' END AS Message,
               @TicketId AS TicketId, T.TicketNo, T.CustomerName, @CompleteTo AS TicketStatus,
               T.RaisedBy, (SELECT Ref_ReportingManagerId FROM dbo.ORG_Employee WHERE EmployeeId = T.AssignedTo) AS ApproverId
        FROM dbo.TKT_Ticket T WHERE T.TicketId = @TicketId;
        RETURN;
    END;

    /* ===================== ADD_ATTACHMENTS ===================== */
    IF (@Status = 'ADD_ATTACHMENTS')
    BEGIN
        IF (@TStatus IN ('REJECTED','CANCELLED'))
            THROW 50090, 'Ticket is closed.', 1;
        INSERT INTO dbo.TKT_TicketAttachment (Ref_TicketId, AttachmentType, FileName, FilePath, CreatedOn, CreatedBy)
        SELECT @TicketId, ISNULL(J.attachmentType, 'OTHER'), J.fileName, J.filePath, @Now, @EmployeeId
        FROM OPENJSON(@AttachmentsJson) WITH (attachmentType VARCHAR(20), fileName NVARCHAR(255), filePath NVARCHAR(500)) J
        WHERE J.filePath IS NOT NULL;

        SELECT 1 AS Status, 'Files uploaded.' AS Message, @TicketId AS TicketId;
        RETURN;
    END;

    /* ===================== CANCEL_TICKET (raiser, before work starts) ===================== */
    IF (@Status = 'CANCEL_TICKET')
    BEGIN
        IF (@TRaisedBy <> @EmployeeId)
            THROW 50100, 'Only the person who raised the ticket can cancel it.', 1;
        IF (@TStatus NOT IN ('OPEN','APPROVAL_PENDING')
            OR EXISTS (SELECT 1 FROM dbo.TKT_TicketActivity WHERE Ref_TicketId = @TicketId))
            THROW 50101, 'Ticket can be cancelled only before work starts.', 1;

        BEGIN TRAN;
            UPDATE dbo.TKT_Ticket SET Status = 'CANCELLED', UpdatedOn = @Now, UpdatedBy = @EmployeeId WHERE TicketId = @TicketId;
            UPDATE dbo.TKT_TicketExpense SET ApprovalStatus = 'REJECTED', ApprovalRemark = 'Ticket cancelled'
            WHERE Ref_TicketId = @TicketId AND ApprovalStatus = 'PENDING';
            INSERT INTO dbo.TKT_TicketStatusLog (Ref_TicketId, FromStatus, ToStatus, Remarks, CreatedOn, CreatedBy)
            VALUES (@TicketId, @TStatus, 'CANCELLED', @Remarks, @Now, @EmployeeId);
        COMMIT;

        SELECT 1 AS Status, 'Ticket cancelled.' AS Message, @TicketId AS TicketId;
        RETURN;
    END;

    /* ===================== MANAGER_DASHBOARD ===================== */
    IF (@Status = 'MANAGER_DASHBOARD')
    BEGIN
        IF (@IsManager = 0)
            THROW 50110, 'Only a Support Manager can open the ticket dashboard.', 1;

        -- Field status per mapped employee:
        -- ON_FIELD  = has a VISIT ticket checked in and not completed
        -- VISITED   = completed a VISIT ticket today
        -- AVAILABLE = otherwise
        ;WITH Team AS
        (
            SELECT E.EmployeeId, LTRIM(RTRIM(ISNULL(E.FirstName, '') + ' ' + ISNULL(E.LastName, ''))) AS EmployeeName,
                   D.DesignationName,
                   OnField.TicketNo AS OnFieldTicketNo, OnField.CustomerName AS OnFieldCustomer, OnField.ServiceMode AS OnFieldMode,
                   Visited.TicketNo AS VisitedTicketNo, Visited.CustomerName AS VisitedCustomer
            FROM dbo.ORG_Employee E
            LEFT JOIN dbo.ORG_Designation D ON D.DesignationId = E.Ref_DesignationId
            OUTER APPLY (SELECT TOP 1 T.TicketNo, T.CustomerName, T.ServiceMode FROM dbo.TKT_Ticket T
                         WHERE T.AssignedTo = E.EmployeeId AND T.Status = 'IN_PROGRESS' AND T.CheckInOn IS NOT NULL
                         ORDER BY T.CheckInOn DESC) OnField
            OUTER APPLY (SELECT TOP 1 T.TicketNo, T.CustomerName FROM dbo.TKT_Ticket T
                         WHERE T.AssignedTo = E.EmployeeId AND T.Status = 'COMPLETED' AND T.ServiceMode = 'VISIT'
                           AND CAST(T.CompletedOn AS DATE) = @Today
                         ORDER BY T.CompletedOn DESC) Visited
            WHERE E.Ref_ReportingManagerId = @EmployeeId AND E.IsActive = 1
        )
        SELECT *, CASE WHEN OnFieldTicketNo IS NOT NULL THEN 'ON_FIELD'
                       WHEN VisitedTicketNo IS NOT NULL THEN 'VISITED'
                       ELSE 'AVAILABLE' END AS FieldStatus
        INTO #Team FROM Team;

        -- T0 counts
        SELECT
            (SELECT COUNT(*) FROM #Team WHERE FieldStatus = 'AVAILABLE') AS AvailableCount,
            (SELECT COUNT(*) FROM #Team WHERE FieldStatus = 'ON_FIELD')  AS OnFieldCount,
            (SELECT COUNT(*) FROM #Team WHERE FieldStatus = 'VISITED')   AS VisitedCount,
            (SELECT COUNT(*) FROM dbo.TKT_Ticket T JOIN #Team M ON M.EmployeeId = T.AssignedTo
              WHERE T.Status IN ('OPEN','APPROVAL_PENDING','IN_PROGRESS','CLOSE_REQUESTED')) AS OpenTicketCount,
            (SELECT COUNT(*) FROM dbo.TKT_Ticket T JOIN #Team M ON M.EmployeeId = T.AssignedTo
              WHERE T.Status NOT IN ('REJECTED','CANCELLED')
                AND (T.ApprovalStatus = 'PENDING'
                     OR EXISTS (SELECT 1 FROM dbo.TKT_TicketExpense X WHERE X.Ref_TicketId = T.TicketId AND X.ApprovalStatus = 'PENDING'))) AS PendingApprovalCount;

        -- T1 team
        SELECT * FROM #Team
        ORDER BY CASE FieldStatus WHEN 'AVAILABLE' THEN 0 WHEN 'ON_FIELD' THEN 1 ELSE 2 END, EmployeeName;
        RETURN;
    END;

    /* ===================== TEAM_LIST (assign dropdown) ===================== */
    IF (@Status = 'TEAM_LIST')
    BEGIN
        IF (@IsManager = 0)
            THROW 50120, 'Only a Support Manager can assign tickets.', 1;

        -- The manager himself first (IsSelf = 1), then his mapped employees.
        SELECT E.EmployeeId, LTRIM(RTRIM(ISNULL(E.FirstName, '') + ' ' + ISNULL(E.LastName, ''))) AS EmployeeName,
               CASE WHEN EXISTS (SELECT 1 FROM dbo.TKT_Ticket T WHERE T.AssignedTo = E.EmployeeId AND T.Status = 'IN_PROGRESS' AND T.CheckInOn IS NOT NULL)
                    THEN 'ON_FIELD' ELSE 'AVAILABLE' END AS FieldStatus,
               CASE WHEN E.EmployeeId = @EmployeeId THEN 1 ELSE 0 END AS IsSelf
        FROM dbo.ORG_Employee E
        WHERE (E.EmployeeId = @EmployeeId)
           OR (E.Ref_ReportingManagerId = @EmployeeId AND E.IsActive = 1)
        ORDER BY IsSelf DESC, EmployeeName;
        RETURN;
    END;

    /* ===================== SET_SERVICE_MODE (assignee chooses Online / Visit) ===================== */
    IF (@Status = 'SET_SERVICE_MODE')
    BEGIN
        IF (@TAssignedTo <> @EmployeeId)
            THROW 50140, 'Only the assigned employee can change the service mode.', 1;
        IF (@ServiceMode NOT IN ('ONLINE','CARRY_IN','VISIT'))
            THROW 50141, 'Service mode must be Online, Carry In or Visit.', 1;
        IF (@TStatus NOT IN ('OPEN','APPROVAL_PENDING')
            OR EXISTS (SELECT 1 FROM dbo.TKT_Ticket WHERE TicketId = @TicketId AND CheckInOn IS NOT NULL))
            THROW 50142, 'Service mode cannot be changed after work has started.', 1;
        IF (@ServiceMode IN ('ONLINE','CARRY_IN') AND EXISTS (SELECT 1 FROM dbo.TKT_TicketExpense
                                                WHERE Ref_TicketId = @TicketId AND ExpenseSource = 'TRAVEL' AND ApprovalStatus <> 'REJECTED'))
            THROW 50143, 'Travel is already requested for this ticket.', 1;

        UPDATE dbo.TKT_Ticket SET ServiceMode = @ServiceMode, UpdatedOn = @Now, UpdatedBy = @EmployeeId
        WHERE TicketId = @TicketId;

        SELECT 1 AS Status, 'Service mode updated.' AS Message, @TicketId AS TicketId, @ServiceMode AS ServiceMode;
        RETURN;
    END;

    /* ===================== PENDING_APPROVALS (Support Manager) ===================== */
    IF (@Status = 'PENDING_APPROVALS')
    BEGIN
        IF (@IsManager = 0)
            THROW 50130, 'Only a Support Manager can view approvals.', 1;

        -- T0 tickets with something pending
        SELECT T.TicketId, T.TicketNo, T.CustomerName, T.ServiceMode, T.Status, T.ApprovalStatus, T.IsRaisedByManager,
               T.AssignedTo, LTRIM(RTRIM(ISNULL(AE.FirstName, '') + ' ' + ISNULL(AE.LastName, ''))) AS AssignedToName,
               T.Issue, T.ExpectedDate,
               CASE WHEN T.RaisedBy = T.AssignedTo THEN 1 ELSE 0 END AS IsSelfRaised
        FROM dbo.TKT_Ticket T
        JOIN dbo.ORG_Employee AE ON AE.EmployeeId = T.AssignedTo
        WHERE AE.Ref_ReportingManagerId = @EmployeeId
          AND T.Status NOT IN ('REJECTED','CANCELLED','COMPLETED')
          AND (T.ApprovalStatus = 'PENDING'
               OR EXISTS (SELECT 1 FROM dbo.TKT_TicketExpense X WHERE X.Ref_TicketId = T.TicketId AND X.ApprovalStatus = 'PENDING'))
        ORDER BY T.TicketId;

        -- T1 their pending expenses
        SELECT X.TicketExpenseId, X.Ref_TicketId, X.ExpenseSource, X.Category, X.TravelMode, X.FromLocation, X.ToLocation,
               X.Km, X.Amount, X.Remarks, X.ReceiptPath
        FROM dbo.TKT_TicketExpense X
        JOIN dbo.TKT_Ticket T ON T.TicketId = X.Ref_TicketId
        JOIN dbo.ORG_Employee AE ON AE.EmployeeId = T.AssignedTo
        WHERE AE.Ref_ReportingManagerId = @EmployeeId AND X.ApprovalStatus = 'PENDING'
          AND T.Status NOT IN ('REJECTED','CANCELLED','COMPLETED')
        ORDER BY X.Ref_TicketId, X.TicketExpenseId;
        RETURN;
    END;

    THROW 50999, 'Invalid @Status.', 1;
END;
GO

/* ===== Optional: give Support Engineer (4) and Support Manager (9) the module.
   Prefer the Module Access screen; uncomment to do it here. =====
INSERT INTO dbo.ORG_RoleModuleAccess (Ref_RoleId, Ref_ModuleId, IsEnabled, CreatedOn, CreatedBy)
SELECT R.RoleId, 4013, 1, GETDATE(), 1
FROM (VALUES (4), (9)) R(RoleId)
WHERE NOT EXISTS (SELECT 1 FROM dbo.ORG_RoleModuleAccess A WHERE A.Ref_RoleId = R.RoleId AND A.Ref_ModuleId = 4013);
*/

/* ===== Backfill (idempotent): CRM-source tickets created before the CREATE_TICKET
   lookup above get their master customer. Only fills NULLs. ===== */
UPDATE T
SET Ref_CustomerId = C.CustomerId
FROM dbo.TKT_Ticket T
CROSS APPLY (SELECT TOP 1 CU.CustomerId FROM dbo.CUS_Customer CU
             WHERE CU.Ref_LeadOrganizationId = T.SourceCustomerId AND CU.IsActive = 1
             ORDER BY CU.CustomerId) C
WHERE T.CustomerSource = 'CRM' AND T.Ref_CustomerId IS NULL;
SELECT @@ROWCOUNT AS TicketsLinked;
GO
