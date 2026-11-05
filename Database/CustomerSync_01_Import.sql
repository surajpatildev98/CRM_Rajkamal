/* =====================================================================
   CRM <-> WMS customer sync  —  Step 1: synonym + one-time import
   ---------------------------------------------------------------------
   Run on the CRM database AFTER CustomerSync_00_Location.sql. Set the two values below per environment:
     UAT  : Rkl_EnterpriseSuiteDB       -> @WmsDb = 'Rkl_WMS_Dev',  @SyncUserId = 11
     LIVE : Rkl_EnterpriseSuiteDB_Live  -> @WmsDb = 'Rkl_WMS_Live', @SyncUserId = 3037
   * Copies every WMS CustomerMaster row into CUS_Customer (+ primary
     contact into CUS_CustomerContact), linked by Ref_WmsCustomerMasterId.
   * Re-runnable: rows already linked are skipped; a CRM customer with the
     same name and no link is linked instead of duplicated.
   * WMS is only read.
   ===================================================================== */
SET XACT_ABORT ON;
SET NOCOUNT ON;

DECLARE @WmsDb SYSNAME = N'Rkl_WMS_Dev';
DECLARE @SyncUserId INT = 11;

/* ---------- 1. Synonym to the environment's WMS customer table ---------- */
IF OBJECT_ID('dbo.WMS_CustomerMaster', 'SN') IS NOT NULL
    DROP SYNONYM dbo.WMS_CustomerMaster;
DECLARE @Sql NVARCHAR(400) = N'CREATE SYNONYM dbo.WMS_CustomerMaster FOR ' + QUOTENAME(@WmsDb) + N'.dbo.CustomerMaster';
EXEC (@Sql);

BEGIN TRAN;

/* ---------- 2. Link existing unlinked CRM customers by exact name ---------- */
UPDATE C
SET Ref_WmsCustomerMasterId = W.CustomerMasterID,
    UpdatedOn = GETDATE(),
    UpdatedBy = @SyncUserId
FROM dbo.CUS_Customer C WITH (UPDLOCK, HOLDLOCK)
JOIN dbo.WMS_CustomerMaster W ON LTRIM(RTRIM(W.CompanyName)) = LTRIM(RTRIM(C.CustomerName))
WHERE C.Ref_WmsCustomerMasterId IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.CUS_Customer X WHERE X.Ref_WmsCustomerMasterId = W.CustomerMasterID);

/* ---------- 3. Insert the rest (same mapping as ENSURE_WMS_CUSTOMER) ---------- */
DECLARE @BaseId INT = (SELECT ISNULL(MAX(CustomerId), 0) FROM dbo.CUS_Customer WITH (UPDLOCK, HOLDLOCK));
DECLARE @New TABLE (CustomerId INT PRIMARY KEY, WmsId INT NOT NULL);

INSERT INTO dbo.CUS_Customer
(
    CustomerCode, CustomerName, Address, Ref_CountryId, Ref_StateId, Ref_CityId,
    PinCode, Gst, Pan, Ref_LeadOrganizationId, Ref_WmsCustomerMasterId,
    IsActive, CreatedOn, CreatedBy
)
OUTPUT inserted.CustomerId, inserted.Ref_WmsCustomerMasterId INTO @New (CustomerId, WmsId)
SELECT
    'CUST' + RIGHT('0000' + CAST(@BaseId + ROW_NUMBER() OVER (ORDER BY W.CustomerMasterID) AS VARCHAR(10)), 5),
    LEFT(LTRIM(RTRIM(W.CompanyName)), 200),
    LEFT(W.CustomerPlace, 500),
    S.Ref_CountryId,
    L.StateId,
    CI.CityId,
    LEFT(NULLIF(LTRIM(RTRIM(W.PINCode)), ''), 10),
    LEFT(NULLIF(LTRIM(RTRIM(W.gst)), ''), 20),
    LEFT(NULLIF(LTRIM(RTRIM(W.pan)), ''), 20),
    NULL,
    W.CustomerMasterID,
    1, GETDATE(), @SyncUserId
FROM dbo.WMS_CustomerMaster W
-- Location via the shared resolver; cities were added by CustomerSync_00_Location.sql.
CROSS APPLY dbo.fn_CRM_WmsLocation(W.City, W.State, W.PINCode) L
LEFT JOIN dbo.MST_State S ON S.StateId = L.StateId
OUTER APPLY (SELECT TOP 1 X.CityId FROM dbo.MST_City X
             WHERE X.CityName = LEFT(L.CityName, 100) AND X.Ref_StateId = L.StateId ORDER BY X.CityId) CI
WHERE NULLIF(LTRIM(RTRIM(W.CompanyName)), '') IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.CUS_Customer C WHERE C.Ref_WmsCustomerMasterId = W.CustomerMasterID);

/* ---------- 4. Primary contact for the customers inserted above ---------- */
INSERT INTO dbo.CUS_CustomerContact
(
    Ref_CustomerId, ContactPerson, Mobile, Email, Ref_DesignationId,
    IsPrimary, IsActive, CreatedOn, CreatedBy
)
SELECT
    N.CustomerId,
    LEFT(LTRIM(RTRIM(W.ContactPerson)), 150),
    LEFT(NULLIF(LTRIM(RTRIM(W.ContactNumber)), ''), 20),
    LEFT(NULLIF(LTRIM(RTRIM(W.Email)), ''), 150),
    NULL, 1, 1, GETDATE(), @SyncUserId
FROM @New N
JOIN dbo.WMS_CustomerMaster W ON W.CustomerMasterID = N.WmsId
WHERE NULLIF(LTRIM(RTRIM(W.ContactPerson)), '') IS NOT NULL;

COMMIT TRAN;

/* ---------- 5. Result ---------- */
SELECT
    (SELECT COUNT(*) FROM dbo.WMS_CustomerMaster)                                        AS WmsCustomers,
    (SELECT COUNT(*) FROM dbo.CUS_Customer WHERE Ref_WmsCustomerMasterId IS NOT NULL)    AS CrmLinkedCustomers,
    (SELECT COUNT(*) FROM @New)                                                          AS InsertedNow,
    (SELECT COUNT(*) FROM dbo.CUS_CustomerContact CC JOIN @New N ON N.CustomerId = CC.Ref_CustomerId) AS ContactsInsertedNow;
