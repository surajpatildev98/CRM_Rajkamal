/* =====================================================================
   CRM <-> WMS customer sync  —  Step 2: USP_CRM_CustomerManagement
   ---------------------------------------------------------------------
   Run on the CRM database AFTER Step 0 and Step 1.
     UAT  : Rkl_EnterpriseSuiteDB       -> @WmsDb = 'Rkl_WMS_Dev'
     LIVE : Rkl_EnterpriseSuiteDB_Live  -> @WmsDb = 'Rkl_WMS_Live'
   Replaces the version from CustomerSearch_CRM_WMS.sql:
   * SEARCH_CUSTOMER_ALL  same result shape; WMS block now reads the
                          imported CUS_Customer rows (no cross-DB search).
                          SourceSystem='WMS', CustomerId = WMS id (unchanged contract).
   * ENSURE_WMS_CUSTOMER  same contract; uses synonyms + fn_CRM_WmsLocation.
                          Called by lead save, ticket create and the WMS trigger.
   * PUSH_TO_WMS (new)    @CustomerId: creates the CRM customer in WMS through
                          WMS's own USP_CustomerDetails 'InsertCustomer' (or links
                          to the WMS customer with the same CompanyName) and stores
                          Ref_WmsCustomerMasterId. No result set (safe to call inside
                          another procedure's transaction); raises an error on failure.
   ===================================================================== */
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

DECLARE @WmsDb SYSNAME = N'Rkl_WMS_Dev';
DECLARE @Sql NVARCHAR(400);

IF OBJECT_ID('dbo.WMS_USP_CustomerDetails', 'SN') IS NOT NULL
    DROP SYNONYM dbo.WMS_USP_CustomerDetails;
SET @Sql = N'CREATE SYNONYM dbo.WMS_USP_CustomerDetails FOR ' + QUOTENAME(@WmsDb) + N'.dbo.USP_CustomerDetails';
EXEC (@Sql);
GO

/* Duplicate key for a company name: 'Macfos Ltd.', 'MACFOS LIMITED-' -> 'MACFOSLTD'. */
CREATE OR ALTER FUNCTION dbo.fn_CRM_CompanyKey (@Name NVARCHAR(400))
RETURNS NVARCHAR(400)
WITH SCHEMABINDING
AS
BEGIN
    DECLARE @s NVARCHAR(400) = N' ' + UPPER(LTRIM(RTRIM(ISNULL(@Name, N'')))) + N' ';
    SET @s = REPLACE(REPLACE(REPLACE(@s, N'&', N' AND '), N'.', N' '), N'-', N' ');
    SET @s = REPLACE(REPLACE(@s, N' PRIVATE ', N' PVT '), N' LIMITED ', N' LTD ');
    DECLARE @k NVARCHAR(400) = N'', @i INT = 1, @c NCHAR(1);
    WHILE @i <= LEN(@s)
    BEGIN
        SET @c = SUBSTRING(@s, @i, 1);
        IF @c LIKE N'[A-Z0-9]' SET @k += @c;
        SET @i += 1;
    END;
    RETURN NULLIF(@k, N'');
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_CRM_CustomerManagement
    @Status               NVARCHAR(50),
    @SearchText           NVARCHAR(200) = NULL,
    @WmsCustomerMasterId  INT           = NULL,
    @LeadId               INT           = NULL,
    @CreatedBy            INT           = NULL,
    @CustomerId           INT           = NULL
AS
BEGIN
    SET NOCOUNT ON;

    /* -------------------------------------------------------------
       SEARCH_CUSTOMER_ALL
       Result (identify by SourceSystem + CustomerId, never CustomerId alone):
         SourceSystem, CustomerId, LeadOrganizationId, Ref_LeadCompanyId,
         CustomerCode, CustomerName, CityName, StateName, DisplayName,
         Address, PinCode, Gst, Pan, Ref_CountryId, Ref_StateId,
         Ref_CityId, ContactPerson, ContactNumber, Email
       ------------------------------------------------------------- */
    IF (@Status = 'SEARCH_CUSTOMER_ALL')
    BEGIN
        SET @SearchText = LTRIM(RTRIM(ISNULL(@SearchText, '')));
        IF (@SearchText = '')
        BEGIN
            RAISERROR('Search text is required.', 16, 1);
            RETURN;
        END;

        SELECT * FROM
        (
            -- CRM lead organizations: same rows/filter as USP_CRM_LeadManagement 'SEARCH_CUSTOMER'
            SELECT TOP (20)
                CAST('CRM' AS VARCHAR(3))            AS SourceSystem,
                LO.LeadOrganizationId                AS CustomerId,
                LO.LeadOrganizationId,
                LO.Ref_LeadCompanyId,
                CAST(NULL AS NVARCHAR(255))          AS CustomerCode,
                CAST(C.CustomerName AS NVARCHAR(255)) AS CustomerName,
                CT.CityName,
                ST.StateName,
                CAST(C.CustomerName + ' - ' + ISNULL(CT.CityName, '') AS NVARCHAR(500)) AS DisplayName,
                CAST(LO.Address AS NVARCHAR(500))    AS Address,
                CAST(LO.PinCode AS NVARCHAR(255))    AS PinCode,
                CAST(LO.Gst AS NVARCHAR(50))         AS Gst,
                CAST(C.Pan AS NVARCHAR(50))          AS Pan,
                LO.Ref_CountryId,
                LO.Ref_StateId,
                LO.Ref_CityId,
                CAST(NULL AS NVARCHAR(255))          AS ContactPerson,
                CAST(NULL AS NVARCHAR(255))          AS ContactNumber,
                CAST(NULL AS NVARCHAR(255))          AS Email
            FROM dbo.CRM_LeadOrganization LO
            JOIN dbo.CRM_LeadCompany C ON C.CompanyId = LO.Ref_LeadCompanyId
            LEFT JOIN dbo.MST_City  CT ON CT.CityId  = LO.Ref_CityId
            LEFT JOIN dbo.MST_State ST ON ST.StateId = LO.Ref_StateId
            WHERE C.CustomerName LIKE @SearchText + '%'
               OR LO.Gst LIKE @SearchText + '%'
               OR C.Pan LIKE @SearchText + '%'
            ORDER BY C.CustomerName, CT.CityName
        ) CRM

        UNION ALL

        SELECT * FROM
        (
            -- WMS customers, from the CRM master (imported / synced). Customers already
            -- linked to a lead organization appear in the CRM block above.
            SELECT TOP (20)
                CAST('WMS' AS VARCHAR(3))            AS SourceSystem,
                CU.Ref_WmsCustomerMasterId           AS CustomerId,
                CAST(NULL AS INT)                    AS LeadOrganizationId,
                CAST(NULL AS INT)                    AS Ref_LeadCompanyId,
                CAST(CU.CustomerCode AS NVARCHAR(255)) AS CustomerCode,
                CAST(CU.CustomerName AS NVARCHAR(255)) AS CustomerName,
                CT.CityName,
                ST.StateName,
                CAST(CU.CustomerName + ' - ' + ISNULL(CT.CityName, '') AS NVARCHAR(500)) AS DisplayName,
                CAST(CU.Address AS NVARCHAR(500))    AS Address,
                CAST(CU.PinCode AS NVARCHAR(255))    AS PinCode,
                CAST(CU.Gst AS NVARCHAR(50))         AS Gst,
                CAST(CU.Pan AS NVARCHAR(50))         AS Pan,
                CU.Ref_CountryId,
                CU.Ref_StateId,
                CU.Ref_CityId,
                CAST(CC.ContactPerson AS NVARCHAR(255)) AS ContactPerson,
                CAST(CC.Mobile AS NVARCHAR(255))     AS ContactNumber,
                CAST(CC.Email AS NVARCHAR(255))      AS Email
            FROM dbo.CUS_Customer CU
            LEFT JOIN dbo.MST_City  CT ON CT.CityId  = CU.Ref_CityId
            LEFT JOIN dbo.MST_State ST ON ST.StateId = CU.Ref_StateId
            OUTER APPLY (SELECT TOP 1 X.ContactPerson, X.Mobile, X.Email FROM dbo.CUS_CustomerContact X
                         WHERE X.Ref_CustomerId = CU.CustomerId AND X.IsActive = 1
                         ORDER BY X.IsPrimary DESC, X.CustomerContactId) CC
            WHERE CU.Ref_WmsCustomerMasterId IS NOT NULL
              AND CU.Ref_LeadOrganizationId IS NULL
              AND CU.IsActive = 1
              AND (CU.CustomerName LIKE @SearchText + '%'
                   OR CU.CustomerCode LIKE @SearchText + '%'
                   OR CU.Gst LIKE @SearchText + '%'
                   OR CU.Pan LIKE @SearchText + '%')
            ORDER BY CU.CustomerName
        ) WMS;

        RETURN;
    END;

    /* -------------------------------------------------------------
       ENSURE_WMS_CUSTOMER
       Returns the CUS_Customer row for a WMS customer, inserting it on
       first use (normally already there after the import). Links the
       lead organization when @LeadId is given.
       Result: CustomerId, CustomerCode, CustomerName, Ref_WmsCustomerMasterId,
               Ref_LeadOrganizationId, IsNew
       ------------------------------------------------------------- */
    IF (@Status = 'ENSURE_WMS_CUSTOMER')
    BEGIN
        IF (@WmsCustomerMasterId IS NULL)
        BEGIN
            RAISERROR('WmsCustomerMasterId is required.', 16, 1);
            RETURN;
        END;

        IF NOT EXISTS (SELECT 1 FROM dbo.WMS_CustomerMaster WHERE CustomerMasterID = @WmsCustomerMasterId)
        BEGIN
            RAISERROR('WMS customer not found.', 16, 1);
            RETURN;
        END;

        DECLARE @LeadOrgId INT = NULL;
        IF (@LeadId IS NOT NULL)
            SELECT @LeadOrgId = Ref_LeadOrganizationId
            FROM dbo.CRM_Lead
            WHERE LeadId = @LeadId;

        DECLARE @EnsuredId INT = NULL, @IsNew BIT = 0;

        BEGIN TRY
            BEGIN TRAN;

            SELECT @EnsuredId = CustomerId
            FROM dbo.CUS_Customer WITH (UPDLOCK, HOLDLOCK)
            WHERE Ref_WmsCustomerMasterId = @WmsCustomerMasterId;

            IF (@EnsuredId IS NULL)
            BEGIN
                DECLARE @Code VARCHAR(20);
                -- Same code format as USP_CRM_OpportunityManagement conversion.
                SELECT @Code = 'CUST' + RIGHT('0000' + CAST(ISNULL(MAX(CustomerId), 0) + 1 AS VARCHAR(10)), 5)
                FROM dbo.CUS_Customer WITH (UPDLOCK, HOLDLOCK);

                -- Location: add the city to the master when it is new.
                DECLARE @E_CityName NVARCHAR(200), @E_StateId INT;
                SELECT @E_CityName = LEFT(L.CityName, 100), @E_StateId = L.StateId
                FROM dbo.WMS_CustomerMaster W
                CROSS APPLY dbo.fn_CRM_WmsLocation(W.City, W.State, W.PINCode) L
                WHERE W.CustomerMasterID = @WmsCustomerMasterId;

                IF (@E_CityName IS NOT NULL AND @E_StateId IS NOT NULL
                    AND NOT EXISTS (SELECT 1 FROM dbo.MST_City WHERE CityName = @E_CityName AND Ref_StateId = @E_StateId))
                    INSERT INTO dbo.MST_City (Ref_StateId, CityCode, CityName, IsActive)
                    VALUES (@E_StateId, NULL, @E_CityName, 1);

                INSERT INTO dbo.CUS_Customer
                (
                    CustomerCode, CustomerName, Address, Ref_CountryId, Ref_StateId, Ref_CityId,
                    PinCode, Gst, Pan, Ref_LeadOrganizationId, Ref_WmsCustomerMasterId,
                    IsActive, CreatedOn, CreatedBy
                )
                SELECT
                    @Code,
                    LEFT(LTRIM(RTRIM(W.CompanyName)), 200),
                    LEFT(W.CustomerPlace, 500),
                    S.Ref_CountryId,
                    @E_StateId,
                    (SELECT TOP 1 X.CityId FROM dbo.MST_City X
                     WHERE X.CityName = @E_CityName AND X.Ref_StateId = @E_StateId ORDER BY X.CityId),
                    LEFT(NULLIF(LTRIM(RTRIM(W.PINCode)), ''), 10),
                    LEFT(NULLIF(LTRIM(RTRIM(W.gst)), ''), 20),
                    LEFT(NULLIF(LTRIM(RTRIM(W.pan)), ''), 20),
                    @LeadOrgId,
                    W.CustomerMasterID,
                    1, GETDATE(), ISNULL(@CreatedBy, 0)
                FROM dbo.WMS_CustomerMaster W
                LEFT JOIN dbo.MST_State S ON S.StateId = @E_StateId
                WHERE W.CustomerMasterID = @WmsCustomerMasterId;

                SET @EnsuredId = SCOPE_IDENTITY();
                SET @IsNew = 1;

                -- Primary contact from WMS, when present.
                INSERT INTO dbo.CUS_CustomerContact
                (
                    Ref_CustomerId, ContactPerson, Mobile, Email, Ref_DesignationId,
                    IsPrimary, IsActive, CreatedOn, CreatedBy
                )
                SELECT
                    @EnsuredId,
                    LEFT(LTRIM(RTRIM(W.ContactPerson)), 150),
                    LEFT(NULLIF(LTRIM(RTRIM(W.ContactNumber)), ''), 20),
                    LEFT(NULLIF(LTRIM(RTRIM(W.Email)), ''), 150),
                    NULL, 1, 1, GETDATE(), ISNULL(@CreatedBy, 0)
                FROM dbo.WMS_CustomerMaster W
                WHERE W.CustomerMasterID = @WmsCustomerMasterId
                  AND NULLIF(LTRIM(RTRIM(W.ContactPerson)), '') IS NOT NULL;
            END
            ELSE IF (@LeadOrgId IS NOT NULL)
            BEGIN
                -- Already mapped (imported, or used in a ticket first): link the lead org once.
                UPDATE dbo.CUS_Customer
                SET Ref_LeadOrganizationId = @LeadOrgId,
                    UpdatedOn = GETDATE(),
                    UpdatedBy = @CreatedBy
                WHERE CustomerId = @EnsuredId
                  AND Ref_LeadOrganizationId IS NULL;
            END;

            COMMIT;
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0 ROLLBACK;
            THROW;
        END CATCH;

        SELECT CustomerId, CustomerCode, CustomerName, Ref_WmsCustomerMasterId,
               Ref_LeadOrganizationId, @IsNew AS IsNew
        FROM dbo.CUS_Customer
        WHERE CustomerId = @EnsuredId;

        RETURN;
    END;

    /* -------------------------------------------------------------
       PUSH_TO_WMS  (@CustomerId, @CreatedBy unused: WMS rows use MUSERID = 1)
       No result set. Runs inside the caller's transaction.
       ------------------------------------------------------------- */
    IF (@Status = 'PUSH_TO_WMS')
    BEGIN
        DECLARE @P_Name NVARCHAR(400), @P_WmsId INT, @P_Address NVARCHAR(1000), @P_Pin NVARCHAR(20),
                @P_Gst NVARCHAR(50), @P_Pan NVARCHAR(50), @P_City NVARCHAR(200), @P_State NVARCHAR(200),
                @P_Contact NVARCHAR(255), @P_Mobile NVARCHAR(255), @P_Email NVARCHAR(255);

        SELECT @P_Name = LTRIM(RTRIM(C.CustomerName)), @P_WmsId = C.Ref_WmsCustomerMasterId,
               @P_Address = C.Address, @P_Pin = C.PinCode, @P_Gst = C.Gst, @P_Pan = C.Pan,
               @P_City = CT.CityName, @P_State = ST.StateName,
               @P_Contact = CC.ContactPerson, @P_Mobile = CC.Mobile, @P_Email = CC.Email
        FROM dbo.CUS_Customer C WITH (UPDLOCK)
        LEFT JOIN dbo.MST_City  CT ON CT.CityId  = C.Ref_CityId
        LEFT JOIN dbo.MST_State ST ON ST.StateId = C.Ref_StateId
        OUTER APPLY (SELECT TOP 1 X.ContactPerson, X.Mobile, X.Email FROM dbo.CUS_CustomerContact X
                     WHERE X.Ref_CustomerId = C.CustomerId AND X.IsActive = 1
                     ORDER BY X.IsPrimary DESC, X.CustomerContactId) CC
        WHERE C.CustomerId = @CustomerId;

        IF (@P_Name IS NULL)
        BEGIN
            RAISERROR('Customer not found for WMS sync.', 16, 1);
            RETURN;
        END;

        IF (@P_WmsId IS NOT NULL) RETURN;   -- already linked

        -- Same company already in WMS: exact name, same company key, or same GST -> link to it.
        DECLARE @P_Key NVARCHAR(400) = dbo.fn_CRM_CompanyKey(@P_Name);
        SET @P_Gst = NULLIF(LTRIM(RTRIM(@P_Gst)), '');
        SELECT TOP 1 @P_WmsId = CustomerMasterID
        FROM dbo.WMS_CustomerMaster WITH (UPDLOCK, HOLDLOCK)
        WHERE CompanyName = @P_Name
           OR dbo.fn_CRM_CompanyKey(CompanyName) = @P_Key
           OR (@P_Gst IS NOT NULL AND LTRIM(RTRIM(gst)) = @P_Gst)
        ORDER BY CASE WHEN CompanyName = @P_Name THEN 0 ELSE 1 END, CustomerMasterID;

        IF (@P_WmsId IS NULL)
        BEGIN
            -- CustomerCode: numeric text in WMS, next = max + 1.
            DECLARE @P_Code NVARCHAR(50) =
                CAST((SELECT ISNULL(MAX(TRY_CAST(CustomerCode AS INT)), 0) + 1
                      FROM dbo.WMS_CustomerMaster WITH (UPDLOCK, HOLDLOCK)) AS NVARCHAR(50));

            -- Shortcode: initials of the name (single word: first 3 letters);
            -- if taken, add letters A..Z, AA.. until unique in WMS.
            DECLARE @P_Words TABLE (Pos INT IDENTITY(1, 1), Word NVARCHAR(200));
            DECLARE @i INT = 1, @ch NCHAR(1), @w NVARCHAR(200) = N'', @src NVARCHAR(400) = UPPER(@P_Name) + N' ';
            WHILE @i <= LEN(@src + N'x') - 1
            BEGIN
                SET @ch = SUBSTRING(@src, @i, 1);
                IF @ch LIKE N'[A-Z0-9]' SET @w += @ch;
                ELSE IF @w <> N''
                BEGIN
                    IF @w NOT IN (N'PVT', N'PRIVATE', N'LTD', N'LIMITED', N'LLP', N'CO', N'AND', N'THE', N'DR', N'INC')
                        INSERT INTO @P_Words (Word) VALUES (@w);
                    SET @w = N'';
                END;
                SET @i += 1;
            END;

            DECLARE @P_Base NVARCHAR(50) =
                CASE WHEN (SELECT COUNT(*) FROM @P_Words) = 1 THEN (SELECT LEFT(Word, 3) FROM @P_Words)
                     ELSE (SELECT STRING_AGG(LEFT(Word, 1), N'') WITHIN GROUP (ORDER BY Pos) FROM @P_Words) END;
            SET @P_Base = ISNULL(NULLIF(@P_Base, N''), N'CUS');

            DECLARE @P_Short NVARCHAR(50) = @P_Base, @n INT = 0, @k INT, @suffix NVARCHAR(10);
            WHILE EXISTS (SELECT 1 FROM dbo.WMS_CustomerMaster WITH (UPDLOCK, HOLDLOCK) WHERE shortcode = @P_Short)
            BEGIN
                SET @n += 1; SET @k = @n; SET @suffix = N'';
                WHILE @k > 0
                BEGIN
                    SET @suffix = NCHAR(65 + (@k - 1) % 26) + @suffix;
                    SET @k = (@k - 1) / 26;
                END;
                SET @P_Short = @P_Base + @suffix;
            END;

            -- Tell the WMS trigger this insert comes from CRM (prevents the loop).
            EXEC sys.sp_set_session_context @key = N'SyncSource', @value = N'CRM';

            DECLARE @P_Msg VARCHAR(255) = '';
            EXEC dbo.WMS_USP_CustomerDetails
                @Status        = N'InsertCustomer',
                @CustomerCode  = @P_Code,
                @CompanyName   = @P_Name,
                @Place         = @P_Address,
                @PINCode       = @P_Pin,
                @ContactPerson = @P_Contact,
                @ContactNumber = @P_Mobile,
                @CITY          = @P_City,
                @STATE         = @P_State,
                @gst           = @P_Gst,
                @pan           = @P_Pan,
                @designation   = N'',
                @email         = @P_Email,
                @MUSERID       = 1,
                @shortcode     = @P_Short,
                @Message       = @P_Msg OUTPUT;

            EXEC sys.sp_set_session_context @key = N'SyncSource', @value = NULL;

            SELECT @P_WmsId = CustomerMasterID FROM dbo.WMS_CustomerMaster WHERE CompanyName = @P_Name;

            IF (@P_WmsId IS NULL)
            BEGIN
                RAISERROR('Customer could not be saved in WMS: %s', 16, 1, @P_Msg);
                RETURN;
            END;
        END;

        IF EXISTS (SELECT 1 FROM dbo.CUS_Customer WHERE Ref_WmsCustomerMasterId = @P_WmsId AND CustomerId <> @CustomerId)
        BEGIN
            RAISERROR('This company is already linked to another CRM customer.', 16, 1);
            RETURN;
        END;

        UPDATE dbo.CUS_Customer
        SET Ref_WmsCustomerMasterId = @P_WmsId
        WHERE CustomerId = @CustomerId;

        RETURN;
    END;

    RAISERROR('Invalid @Status.', 16, 1);
END;
GO
