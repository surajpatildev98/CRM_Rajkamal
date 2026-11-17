/* =====================================================================
   AMC module - tables (UAT: Rkl_EnterpriseSuiteDB)
   Rollback: AMC_Tables_99_Rollback.sql
   ===================================================================== */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRAN;

/* ---------- Masters ---------- */
CREATE TABLE dbo.MST_AmcDuration
(
    DurationMonths  INT           NOT NULL CONSTRAINT PK_MST_AmcDuration PRIMARY KEY,
    DisplayName     NVARCHAR(50)  NOT NULL,          -- shown in Android dropdown
    SortOrder       INT           NOT NULL,
    IsActive        BIT           NOT NULL CONSTRAINT DF_MST_AmcDuration_IsActive DEFAULT (1)
);
INSERT INTO dbo.MST_AmcDuration (DurationMonths, DisplayName, SortOrder) VALUES
(3, N'3 Months', 1), (6, N'6 Months', 2), (12, N'12 Months (1 Year)', 3), (24, N'24 Months (2 Years)', 4), (36, N'36 Months (3 Years)', 5);

CREATE TABLE dbo.MST_AmcCoverageService
(
    CoverageServiceId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MST_AmcCoverageService PRIMARY KEY,
    ServiceName       NVARCHAR(100) NOT NULL,
    AmcType           VARCHAR(10)   NOT NULL,        -- HARDWARE | SOFTWARE | BOTH
    SortOrder         INT           NOT NULL CONSTRAINT DF_MST_AmcCoverageService_SortOrder DEFAULT (0),
    IsActive          BIT           NOT NULL CONSTRAINT DF_MST_AmcCoverageService_IsActive DEFAULT (1),
    CreatedOn         DATETIME      NOT NULL CONSTRAINT DF_MST_AmcCoverageService_CreatedOn DEFAULT (GETDATE()),
    CreatedBy         INT           NULL,
    UpdatedOn         DATETIME      NULL,
    UpdatedBy         INT           NULL,
    CONSTRAINT UQ_MST_AmcCoverageService_Name UNIQUE (ServiceName, AmcType),
    CONSTRAINT CK_MST_AmcCoverageService_Type CHECK (AmcType IN ('HARDWARE','SOFTWARE','BOTH'))
);
INSERT INTO dbo.MST_AmcCoverageService (ServiceName, AmcType, SortOrder) VALUES
(N'Bug Fixes', 'SOFTWARE', 1), (N'Updates', 'SOFTWARE', 2), (N'Technical Support', 'BOTH', 3),
(N'Remote Support', 'BOTH', 4), (N'Onsite Visit', 'HARDWARE', 5), (N'Parts Replacement', 'HARDWARE', 6);

CREATE TABLE dbo.AMC_NumberCounter
(
    AmcYear     INT NOT NULL CONSTRAINT PK_AMC_NumberCounter PRIMARY KEY,
    LastNumber  INT NOT NULL
);

/* ---------- Contract header ---------- */
CREATE TABLE dbo.AMC_Contract
(
    AmcContractId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AMC_Contract PRIMARY KEY,
    AmcNo                VARCHAR(20)    NOT NULL,          -- AMC-2026-0001
    AmcType              VARCHAR(10)    NOT NULL,          -- HARDWARE | SOFTWARE
    CoverageType         VARCHAR(20)    NOT NULL,          -- COMPREHENSIVE | NON_COMPREHENSIVE
    -- Customer: same mechanism as TKT_Ticket (ids overlap between CRM and WMS)
    CustomerSource       VARCHAR(3)     NOT NULL,          -- CRM | WMS
    SourceCustomerId     INT            NOT NULL,
    Ref_CustomerId       INT            NULL,
    CustomerName         NVARCHAR(255)  NOT NULL,
    ContactMobile        VARCHAR(20)    NULL,
    ContactEmail         NVARCHAR(150)  NULL,              -- renewal reminders go here
    -- Period
    DurationMonths       INT            NOT NULL CONSTRAINT FK_AMC_Contract_Duration REFERENCES dbo.MST_AmcDuration (DurationMonths),
    StartDate            DATE           NOT NULL,
    EndDate              DATE           NOT NULL,          -- StartDate + months - 1 day (set by SP)
    TotalVisits          INT            NULL,              -- required HW, optional SW; used = count of completed VISIT tickets
    -- Invoice (one optional PDF)
    InvoiceNo            NVARCHAR(50)   NULL,
    InvoiceDate          DATE           NULL,
    InvoiceFilePath      NVARCHAR(500)  NULL,
    InvoiceFileName      NVARCHAR(255)  NULL,
    -- Money
    Amount               DECIMAL(18,2)  NOT NULL,
    GstPercent           DECIMAL(5,2)   NOT NULL CONSTRAINT DF_AMC_Contract_GstPercent DEFAULT (0),
    GstAmount            DECIMAL(18,2)  NOT NULL CONSTRAINT DF_AMC_Contract_GstAmount DEFAULT (0),
    TotalAmount          DECIMAL(18,2)  NOT NULL,
    -- Payment (simple)
    PaymentStatus        VARCHAR(10)    NOT NULL CONSTRAINT DF_AMC_Contract_PaymentStatus DEFAULT ('PENDING'),
    PaymentMode          VARCHAR(30)    NULL,
    PaymentDate          DATE           NULL,
    PaymentRemark        NVARCHAR(500)  NULL,
    -- Status (Expiring / Expired are derived from EndDate)
    Status               VARCHAR(10)    NOT NULL CONSTRAINT DF_AMC_Contract_Status DEFAULT ('ACTIVE'),
    Ref_RenewedFromAmcId INT            NULL CONSTRAINT FK_AMC_Contract_RenewedFrom REFERENCES dbo.AMC_Contract (AmcContractId),
    RenewedToAmcId       INT            NULL CONSTRAINT FK_AMC_Contract_RenewedTo REFERENCES dbo.AMC_Contract (AmcContractId),
    CancelReason         NVARCHAR(500)  NULL,
    CancelledBy          INT            NULL,
    CancelledOn          DATETIME       NULL,
    Remarks              NVARCHAR(1000) NULL,
    IsActive             BIT            NOT NULL CONSTRAINT DF_AMC_Contract_IsActive DEFAULT (1),
    CreatedOn            DATETIME       NOT NULL CONSTRAINT DF_AMC_Contract_CreatedOn DEFAULT (GETDATE()),
    CreatedBy            INT            NOT NULL,
    UpdatedOn            DATETIME       NULL,
    UpdatedBy            INT            NULL,
    CONSTRAINT UQ_AMC_Contract_AmcNo UNIQUE (AmcNo),
    CONSTRAINT CK_AMC_Contract_AmcType CHECK (AmcType IN ('HARDWARE','SOFTWARE')),
    CONSTRAINT CK_AMC_Contract_CoverageType CHECK (CoverageType IN ('COMPREHENSIVE','NON_COMPREHENSIVE')),
    CONSTRAINT CK_AMC_Contract_CustomerSource CHECK (CustomerSource IN ('CRM','WMS')),
    CONSTRAINT CK_AMC_Contract_PaymentStatus CHECK (PaymentStatus IN ('PENDING','PARTIAL','PAID')),
    CONSTRAINT CK_AMC_Contract_Status CHECK (Status IN ('ACTIVE','RENEWED','CANCELLED')),
    CONSTRAINT CK_AMC_Contract_Dates CHECK (EndDate >= StartDate)
);
CREATE INDEX IX_AMC_Contract_Customer ON dbo.AMC_Contract (CustomerSource, SourceCustomerId);
CREATE INDEX IX_AMC_Contract_EndDate  ON dbo.AMC_Contract (Status, EndDate);

/* ---------- Items (many serials per contract) ---------- */
CREATE TABLE dbo.AMC_ContractItem
(
    AmcContractItemId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AMC_ContractItem PRIMARY KEY,
    Ref_AmcContractId     INT            NOT NULL CONSTRAINT FK_AMC_ContractItem_Contract REFERENCES dbo.AMC_Contract (AmcContractId),
    Ref_ProductCategoryId INT            NULL,
    CategoryName          NVARCHAR(200)  NULL,
    Ref_OfferingId        INT            NULL,
    OfferingName          NVARCHAR(200)  NULL,
    Ref_ProductId         BIGINT         NULL,
    ProductName           NVARCHAR(255)  NULL,
    SerialNo              NVARCHAR(100)  NULL,             -- required for HARDWARE (SP check)
    LicenseKey            NVARCHAR(200)  NULL,             -- SOFTWARE optional
    SoftwareVersion       NVARCHAR(50)   NULL,
    NoOfUsers             INT            NULL,
    ItemRemark            NVARCHAR(500)  NULL,
    IsActive              BIT            NOT NULL CONSTRAINT DF_AMC_ContractItem_IsActive DEFAULT (1),
    CreatedOn             DATETIME       NOT NULL CONSTRAINT DF_AMC_ContractItem_CreatedOn DEFAULT (GETDATE()),
    CreatedBy             INT            NOT NULL,
    UpdatedOn             DATETIME       NULL,
    UpdatedBy             INT            NULL
);
CREATE INDEX IX_AMC_ContractItem_Contract ON dbo.AMC_ContractItem (Ref_AmcContractId);
CREATE INDEX IX_AMC_ContractItem_SerialNo ON dbo.AMC_ContractItem (SerialNo) WHERE SerialNo IS NOT NULL;

/* ---------- Coverage chips (contract level now; item level later via Ref_AmcContractItemId) ---------- */
CREATE TABLE dbo.AMC_ContractCoverage
(
    AmcContractCoverageId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AMC_ContractCoverage PRIMARY KEY,
    Ref_AmcContractId     INT      NOT NULL CONSTRAINT FK_AMC_ContractCoverage_Contract REFERENCES dbo.AMC_Contract (AmcContractId),
    Ref_AmcContractItemId INT      NULL     CONSTRAINT FK_AMC_ContractCoverage_Item REFERENCES dbo.AMC_ContractItem (AmcContractItemId), -- NULL = whole contract
    Ref_CoverageServiceId INT      NOT NULL CONSTRAINT FK_AMC_ContractCoverage_Service REFERENCES dbo.MST_AmcCoverageService (CoverageServiceId),
    CreatedOn             DATETIME NOT NULL CONSTRAINT DF_AMC_ContractCoverage_CreatedOn DEFAULT (GETDATE()),
    CreatedBy             INT      NOT NULL
);
CREATE INDEX IX_AMC_ContractCoverage_Contract ON dbo.AMC_ContractCoverage (Ref_AmcContractId);

/* ---------- History ---------- */
CREATE TABLE dbo.AMC_ContractLog
(
    AmcContractLogId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AMC_ContractLog PRIMARY KEY,
    Ref_AmcContractId INT            NOT NULL CONSTRAINT FK_AMC_ContractLog_Contract REFERENCES dbo.AMC_Contract (AmcContractId),
    Action            VARCHAR(20)    NOT NULL,   -- CREATED | EDITED | RENEWED | CANCELLED | INVOICE_ATTACHED | REMINDER_SENT
    ReminderDays      INT            NULL,       -- 30/15/10/5/1 for REMINDER_SENT (stops duplicate mails)
    Remark            NVARCHAR(1000) NULL,
    ActionBy          INT            NULL,       -- NULL = system/scheduler
    ActionOn          DATETIME       NOT NULL CONSTRAINT DF_AMC_ContractLog_ActionOn DEFAULT (GETDATE()),
    CONSTRAINT CK_AMC_ContractLog_Action CHECK (Action IN ('CREATED','EDITED','RENEWED','CANCELLED','INVOICE_ATTACHED','REMINDER_SENT'))
);
CREATE INDEX IX_AMC_ContractLog_Contract ON dbo.AMC_ContractLog (Ref_AmcContractId);

/* ---------- Ticket link (no blocking) ---------- */
ALTER TABLE dbo.TKT_Ticket ADD
    Ref_AmcContractId INT         NULL CONSTRAINT FK_TKT_Ticket_AmcContract REFERENCES dbo.AMC_Contract (AmcContractId),
    AmcCoverageStatus VARCHAR(25) NULL;   -- COVERED | NOT_COVERED | VISIT_LIMIT_EXCEEDED
EXEC ('ALTER TABLE dbo.TKT_Ticket ADD CONSTRAINT CK_TKT_Ticket_AmcCoverageStatus
       CHECK (AmcCoverageStatus IN (''COVERED'',''NOT_COVERED'',''VISIT_LIMIT_EXCEEDED''))');
EXEC ('CREATE INDEX IX_TKT_Ticket_AmcContract ON dbo.TKT_Ticket (Ref_AmcContractId) WHERE Ref_AmcContractId IS NOT NULL');

COMMIT;
