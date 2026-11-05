/* =====================================================================
   CRM <-> WMS customer sync  —  Step 4: WMS -> CRM trigger
   ---------------------------------------------------------------------
   Run on the WMS database AFTER the CRM steps 0-3.
     DEV  : Rkl_WMS_Dev   -> @CrmDb = 'Rkl_EnterpriseSuiteDB'       and CreatedBy 11   (in the trigger)
     LIVE : Rkl_WMS_Live  -> @CrmDb = 'Rkl_EnterpriseSuiteDB_Live'  and CreatedBy 3037 (in the trigger)
   * Every new CustomerMaster row is copied into the CRM master through
     CRM's USP_CRM_CustomerManagement 'ENSURE_WMS_CUSTOMER' (idempotent).
   * Rows inserted by CRM itself (PUSH_TO_WMS sets SESSION_CONTEXT
     'SyncSource' = 'CRM') are skipped, so there is no loop.
   * Runs inside the WMS insert's transaction: if CRM fails, the WMS insert
     rolls back too, so the two never disagree.
   * USP_CustomerDetails and the WMS application are not changed.
   ===================================================================== */
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

DECLARE @CrmDb SYSNAME = N'Rkl_EnterpriseSuiteDB';
DECLARE @Sql NVARCHAR(400);

IF OBJECT_ID('dbo.CRM_USP_CustomerManagement', 'SN') IS NOT NULL
    DROP SYNONYM dbo.CRM_USP_CustomerManagement;
SET @Sql = N'CREATE SYNONYM dbo.CRM_USP_CustomerManagement FOR ' + QUOTENAME(@CrmDb) + N'.dbo.USP_CRM_CustomerManagement';
EXEC (@Sql);
GO

CREATE OR ALTER TRIGGER dbo.trg_CustomerMaster_SyncToCRM
ON dbo.CustomerMaster
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM inserted) RETURN;
    IF CAST(SESSION_CONTEXT(N'SyncSource') AS NVARCHAR(10)) = N'CRM' RETURN;   -- inserted by CRM

    DECLARE @SyncUserId INT = 11;   -- CRM CreatedBy: DEV 11, LIVE 3037

    -- ENSURE returns one row; capture it so the trigger returns no result set to the WMS app.
    DECLARE @Out TABLE (CustomerId INT, CustomerCode NVARCHAR(50), CustomerName NVARCHAR(400),
                        Ref_WmsCustomerMasterId INT, Ref_LeadOrganizationId INT, IsNew BIT);
    DECLARE @Id INT;

    DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT CustomerMasterID FROM inserted;
    OPEN c;
    FETCH NEXT FROM c INTO @Id;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        INSERT INTO @Out
        EXEC dbo.CRM_USP_CustomerManagement
            @Status = 'ENSURE_WMS_CUSTOMER',
            @WmsCustomerMasterId = @Id,
            @CreatedBy = @SyncUserId;

        FETCH NEXT FROM c INTO @Id;
    END;
    CLOSE c;
    DEALLOCATE c;
END;
GO
