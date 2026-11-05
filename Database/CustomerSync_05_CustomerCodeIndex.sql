/* =====================================================================
   CRM <-> WMS customer sync  —  Step 5: unique CUS_Customer.CustomerCode
   ---------------------------------------------------------------------
   Run on the CRM database (UAT and Live). Idempotent.
   CLOSE_OPPORTUNITY generates 'CUST' + MAX(CustomerId)+1 without a lock;
   this index turns a simultaneous-close collision into an error (the close
   rolls back and can be retried) instead of a silent duplicate code.
   ===================================================================== */
SET NOCOUNT ON;

IF EXISTS (SELECT 1 FROM dbo.CUS_Customer GROUP BY CustomerCode HAVING COUNT(*) > 1)
    THROW 50100, 'Duplicate CustomerCode values exist; fix them before creating the unique index.', 1;

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UX_CUS_Customer_CustomerCode' AND object_id = OBJECT_ID('dbo.CUS_Customer'))
    CREATE UNIQUE INDEX UX_CUS_Customer_CustomerCode ON dbo.CUS_Customer (CustomerCode);

SELECT name, is_unique FROM sys.indexes
WHERE object_id = OBJECT_ID('dbo.CUS_Customer') AND name = 'UX_CUS_Customer_CustomerCode';
