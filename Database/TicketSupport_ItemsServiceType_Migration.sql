/* =====================================================================
   Ticket Support: Requirement / Offering items + Service Type
   Run on Rkl_EnterpriseSuiteDB_Live (safe to run more than once).

   Step 1 (this file): table changes.
   Step 2: re-run the PROCEDURE part of Database/TicketSupport.sql
           (CREATE OR ALTER PROCEDURE dbo.USP_TKT_TicketManagement), which adds
           @ServiceTypes, @ServiceTypeOther, @ItemsJson, saves them in
           CREATE_TICKET, and returns the items as result set T5 of TICKET_DETAILS.
   ===================================================================== */

/* ---- Service Mode: add CARRY_IN (works like ONLINE) ---- */
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
/* ---- Close request (assignee -> raiser). Ticket Status gets a new value CLOSE_REQUESTED ---- */
IF COL_LENGTH('dbo.TKT_Ticket', 'CloseRequestedOn') IS NULL
    ALTER TABLE dbo.TKT_Ticket ADD CloseRequestedOn DATETIME NULL,
                                   CloseDecisionRemark NVARCHAR(500) NULL;
/* ---- Planned activity marked completed later ---- */
IF COL_LENGTH('dbo.TKT_TicketActivity', 'CompletedOn') IS NULL
    ALTER TABLE dbo.TKT_TicketActivity ADD CompletedOn DATETIME NULL;
GO

/* ---- Service Type columns on the ticket ---- */
IF COL_LENGTH('dbo.TKT_Ticket', 'ServiceTypes') IS NULL
    ALTER TABLE dbo.TKT_Ticket ADD ServiceTypes VARCHAR(100) NULL;          -- WARRANTY,AMC,BILLABLE,TESTING,OTHER
IF COL_LENGTH('dbo.TKT_Ticket', 'ServiceTypeOther') IS NULL
    ALTER TABLE dbo.TKT_Ticket ADD ServiceTypeOther NVARCHAR(200) NULL;     -- text when OTHER is ticked
GO

/* ---- Requirement / Offering items ---- */
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
