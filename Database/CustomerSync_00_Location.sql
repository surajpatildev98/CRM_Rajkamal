/* =====================================================================
   CRM <-> WMS customer sync  —  Step 0: State / City masters
   ---------------------------------------------------------------------
   Run on the CRM database BEFORE CustomerSync_01_Import.sql.
     UAT  : Rkl_EnterpriseSuiteDB       -> @WmsDb = 'Rkl_WMS_Dev'
     LIVE : Rkl_EnterpriseSuiteDB_Live  -> @WmsDb = 'Rkl_WMS_Live'
   1. UAT only: aligns MST_State / MST_City with Live (same IDs). Live is read only.
   2. Synonym dbo.WMS_PincodeMaster -> <WmsDb>.dbo.GATE_Pincode_Master.
   3. dbo.fn_CRM_WmsLocation: resolves a WMS City/State/PIN to a clean
      CityName + CRM StateId (PIN master first, then text, then aliases).
      Shared by the import, ENSURE_WMS_CUSTOMER and the WMS trigger.
   4. Adds every missing WMS city to MST_City and fills State/City/Country
      on WMS-linked CUS_Customer rows. Re-runnable.
   ===================================================================== */
SET XACT_ABORT ON;
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @WmsDb SYSNAME = N'Rkl_WMS_Dev';
DECLARE @MasterDb SYSNAME = N'Rkl_EnterpriseSuiteDB_Live';
DECLARE @Sql NVARCHAR(MAX);

/* ---------- 1. Align masters with Live (skipped when running on Live) ---------- */
IF DB_NAME() <> @MasterDb
BEGIN
    SET @Sql = N'
    UPDATE S SET StateName = L.StateName, StateCode = L.StateCode, Ref_CountryId = L.Ref_CountryId
    FROM dbo.MST_State S JOIN ' + QUOTENAME(@MasterDb) + N'.dbo.MST_State L ON L.StateId = S.StateId
    WHERE S.StateName <> L.StateName OR ISNULL(S.StateCode, '''') <> ISNULL(L.StateCode, '''');

    SET IDENTITY_INSERT dbo.MST_State ON;
    INSERT INTO dbo.MST_State (StateId, Ref_CountryId, StateCode, StateName, IsActive)
    SELECT L.StateId, L.Ref_CountryId, L.StateCode, L.StateName, L.IsActive
    FROM ' + QUOTENAME(@MasterDb) + N'.dbo.MST_State L
    WHERE NOT EXISTS (SELECT 1 FROM dbo.MST_State S WHERE S.StateId = L.StateId);
    SET IDENTITY_INSERT dbo.MST_State OFF;

    SET IDENTITY_INSERT dbo.MST_City ON;
    INSERT INTO dbo.MST_City (CityId, Ref_StateId, CityCode, CityName, IsActive)
    SELECT L.CityId, L.Ref_StateId, L.CityCode, L.CityName, L.IsActive
    FROM ' + QUOTENAME(@MasterDb) + N'.dbo.MST_City L
    WHERE NOT EXISTS (SELECT 1 FROM dbo.MST_City C WHERE C.CityId = L.CityId);
    SET IDENTITY_INSERT dbo.MST_City OFF;';
    EXEC (@Sql);
END;

/* ---------- 1b. Foreign locations used by WMS customers (by name, idempotent) ---------- */
INSERT INTO dbo.MST_Country (CountryCode, CountryName, IsActive)
SELECT V.Code, V.Name, 1 FROM (VALUES ('SG', 'SINGAPORE'), ('BD', 'BANGLADESH')) V(Code, Name)
WHERE NOT EXISTS (SELECT 1 FROM dbo.MST_Country C WHERE C.CountryName = V.Name);

INSERT INTO dbo.MST_State (Ref_CountryId, StateCode, StateName, IsActive)
SELECT C.CountryId, V.Code, V.StateName, 1
FROM (VALUES ('SINGAPORE', 'SG', 'Singapore'), ('BANGLADESH', 'DHK', 'Dhaka')) V(Country, Code, StateName)
JOIN dbo.MST_Country C ON C.CountryName = V.Country
WHERE NOT EXISTS (SELECT 1 FROM dbo.MST_State S WHERE S.StateName = V.StateName AND S.Ref_CountryId = C.CountryId);

INSERT INTO dbo.MST_City (Ref_StateId, CityCode, CityName, IsActive)
SELECT S.StateId, NULL, V.CityName, 1
FROM (VALUES ('Singapore', 'Singapore'), ('Dhaka', 'Narayanganj')) V(StateName, CityName)
JOIN dbo.MST_State S ON S.StateName = V.StateName
WHERE NOT EXISTS (SELECT 1 FROM dbo.MST_City X WHERE X.CityName = V.CityName AND X.Ref_StateId = S.StateId);

/* ---------- 2. Synonym to the environment's WMS PIN master ---------- */
IF OBJECT_ID('dbo.WMS_PincodeMaster', 'SN') IS NOT NULL
    DROP SYNONYM dbo.WMS_PincodeMaster;
SET @Sql = N'CREATE SYNONYM dbo.WMS_PincodeMaster FOR ' + QUOTENAME(@WmsDb) + N'.dbo.GATE_Pincode_Master';
EXEC (@Sql);

IF OBJECT_ID('dbo.WMS_CustomerMaster', 'SN') IS NOT NULL
    DROP SYNONYM dbo.WMS_CustomerMaster;
SET @Sql = N'CREATE SYNONYM dbo.WMS_CustomerMaster FOR ' + QUOTENAME(@WmsDb) + N'.dbo.CustomerMaster';
EXEC (@Sql);
GO

/* ---------- 3. Shared resolver ---------- */
CREATE OR ALTER FUNCTION dbo.fn_CRM_WmsLocation
(
    @City  NVARCHAR(200),
    @State NVARCHAR(200),
    @Pin   NVARCHAR(20)
)
RETURNS TABLE
AS
RETURN
    WITH K AS
    (
        SELECT
            CityName = COALESCE(NULLIF(LTRIM(RTRIM(P.PCity)), ''), NULLIF(LTRIM(RTRIM(@City)), '')),
            -- key = upper, '&' -> AND, no spaces; aliases for known WMS spellings
            PinKey   = A1.K,
            TextKey  = A2.K
        FROM (SELECT 1 AS One) O
        OUTER APPLY (SELECT TOP 1 G.PCity, G.PState FROM dbo.WMS_PincodeMaster G
                     WHERE G.Pincodeno = LTRIM(RTRIM(@Pin)) ORDER BY G.PID) P
        CROSS APPLY (SELECT REPLACE(REPLACE(UPPER(LTRIM(RTRIM(P.PState))), '&', 'AND'), ' ', '')) N1(K)
        CROSS APPLY (SELECT REPLACE(REPLACE(UPPER(LTRIM(RTRIM(@State))),  '&', 'AND'), ' ', '')) N2(K)
        CROSS APPLY (SELECT CASE N1.K WHEN 'CHATTISGARH' THEN 'CHHATTISGARH' WHEN 'PONDICHERRY' THEN 'PUDUCHERRY'
                                      WHEN 'UTTATRAKHAND' THEN 'UTTARAKHAND' WHEN 'NEWDELHI' THEN 'DELHI'
                                      WHEN 'DADRAANDNAGARHAVELI' THEN 'DADRAANDNAGARHAVELIANDDAMANANDDIU'
                                      WHEN 'DAMANANDDIU' THEN 'DADRAANDNAGARHAVELIANDDAMANANDDIU' ELSE N1.K END) A1(K)
        CROSS APPLY (SELECT CASE N2.K WHEN 'CHATTISGARH' THEN 'CHHATTISGARH' WHEN 'PONDICHERRY' THEN 'PUDUCHERRY'
                                      WHEN 'UTTATRAKHAND' THEN 'UTTARAKHAND' WHEN 'NEWDELHI' THEN 'DELHI'
                                      WHEN 'DADRAANDNAGARHAVELI' THEN 'DADRAANDNAGARHAVELIANDDAMANANDDIU'
                                      WHEN 'DAMANANDDIU' THEN 'DADRAANDNAGARHAVELIANDDAMANANDDIU' ELSE N2.K END) A2(K)
    )
    SELECT
        -- City blank: use a city named like the state when one exists (city-states, e.g. Singapore).
        CityName = COALESCE(K.CityName, SS.CityName),
        StateId  = COALESCE(SP.StateId, ST.StateId, SC.Ref_StateId)
    FROM K
    OUTER APPLY (SELECT TOP 1 C.CityName FROM dbo.MST_City C
                 WHERE K.CityName IS NULL AND C.CityName = LTRIM(RTRIM(@State))) SS
    OUTER APPLY (SELECT TOP 1 S.StateId FROM dbo.MST_State S
                 WHERE REPLACE(REPLACE(UPPER(S.StateName), '&', 'AND'), ' ', '') = K.PinKey) SP
    OUTER APPLY (SELECT TOP 1 S.StateId FROM dbo.MST_State S
                 WHERE REPLACE(REPLACE(UPPER(S.StateName), '&', 'AND'), ' ', '') = K.TextKey) ST
    -- State text is junk (e.g. a city name): take the state of a known city with that name.
    OUTER APPLY (SELECT TOP 1 C.Ref_StateId FROM dbo.MST_City C
                 WHERE C.CityName = K.CityName ORDER BY C.CityId) SC;
GO

/* ---------- 4. Missing cities + fill linked customers ---------- */
SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRAN;

INSERT INTO dbo.MST_City (Ref_StateId, CityCode, CityName, IsActive)
SELECT DISTINCT L.StateId, NULL, LEFT(L.CityName, 100), 1
FROM dbo.WMS_CustomerMaster W
CROSS APPLY dbo.fn_CRM_WmsLocation(W.City, W.State, W.PINCode) L
WHERE L.CityName IS NOT NULL AND L.StateId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.MST_City C WHERE C.CityName = LEFT(L.CityName, 100) AND C.Ref_StateId = L.StateId);

UPDATE C
SET Ref_StateId   = L.StateId,
    Ref_CityId    = CI.CityId,
    Ref_CountryId = S.Ref_CountryId
FROM dbo.CUS_Customer C
JOIN dbo.WMS_CustomerMaster W ON W.CustomerMasterID = C.Ref_WmsCustomerMasterId
CROSS APPLY dbo.fn_CRM_WmsLocation(W.City, W.State, W.PINCode) L
LEFT JOIN dbo.MST_State S ON S.StateId = L.StateId
OUTER APPLY (SELECT TOP 1 X.CityId FROM dbo.MST_City X
             WHERE X.CityName = LEFT(L.CityName, 100) AND X.Ref_StateId = L.StateId ORDER BY X.CityId) CI
WHERE L.StateId IS NOT NULL;

COMMIT TRAN;

/* ---------- 5. Result: anything still unresolved is listed for manual fix ---------- */
SELECT COUNT(*) AS LinkedCustomers,
       SUM(IIF(Ref_StateId IS NULL, 1, 0)) AS StateNull,
       SUM(IIF(Ref_CityId IS NULL, 1, 0))  AS CityNull
FROM dbo.CUS_Customer WHERE Ref_WmsCustomerMasterId IS NOT NULL;

SELECT C.CustomerId, C.CustomerName, W.City, W.State, W.PINCode
FROM dbo.CUS_Customer C
JOIN dbo.WMS_CustomerMaster W ON W.CustomerMasterID = C.Ref_WmsCustomerMasterId
WHERE C.Ref_StateId IS NULL OR C.Ref_CityId IS NULL;
