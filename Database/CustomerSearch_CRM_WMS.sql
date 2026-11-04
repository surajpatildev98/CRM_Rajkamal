/* =====================================================================
   CRM + WMS combined customer search
   ---------------------------------------------------------------------
   * Customer dropdown/search shows CRM customers (CRM_LeadOrganization +
     CRM_LeadCompany, same rows as USP_CRM_LeadManagement 'SEARCH_CUSTOMER')
     and WMS customers (Rkl_WMS_Live.dbo.CustomerMaster) together.
   * WMS customers are copied into the CRM customer master (CUS_Customer)
     only on first use (a lead saved for that customer), linked by the new
     column CUS_Customer.Ref_WmsCustomerMasterId.
   * Rkl_WMS_Live is read only. USP_CRM_LeadManagement is NOT changed.
   * Run on Rkl_EnterpriseSuiteDB_Live (same SQL Server as Rkl_WMS_Live).
   ===================================================================== */

/* ---------- 1. Mapping column on the CRM customer master ---------- */
IF COL_LENGTH('dbo.CUS_Customer', 'Ref_WmsCustomerMasterId') IS NULL
BEGIN
    ALTER TABLE dbo.CUS_Customer ADD Ref_WmsCustomerMasterId INT NULL;
END;
GO

-- One WMS customer maps to at most one CRM customer.
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UX_CUS_Customer_Ref_WmsCustomerMasterId'
                 AND object_id = OBJECT_ID('dbo.CUS_Customer'))
BEGIN
    CREATE UNIQUE INDEX UX_CUS_Customer_Ref_WmsCustomerMasterId
        ON dbo.CUS_Customer (Ref_WmsCustomerMasterId)
        WHERE Ref_WmsCustomerMasterId IS NOT NULL;
END;
GO

/* ---------- 2. Procedure ---------- */
CREATE OR ALTER PROCEDURE dbo.USP_CRM_CustomerManagement
    @Status               NVARCHAR(50),
    @SearchText           NVARCHAR(200) = NULL,
    @WmsCustomerMasterId  INT           = NULL,
    @LeadId               INT           = NULL,
    @CreatedBy            INT           = NULL
AS
BEGIN
    SET NOCOUNT ON;

    /* -------------------------------------------------------------
       SEARCH_CUSTOMER_ALL
       Result (one shape for both sources, identify by
       SourceSystem + CustomerId, never CustomerId alone):
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
            -- CRM: same rows/filter as USP_CRM_LeadManagement 'SEARCH_CUSTOMER'
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
            -- WMS: all CustomerMaster rows (StatusActive is NULL for all rows,
            -- so no active filter). Skip customers already linked to a CRM lead
            -- organization - they appear in the CRM block above.
            SELECT TOP (20)
                CAST('WMS' AS VARCHAR(3))            AS SourceSystem,
                W.CustomerMasterID                   AS CustomerId,
                CAST(NULL AS INT)                    AS LeadOrganizationId,
                CAST(NULL AS INT)                    AS Ref_LeadCompanyId,
                W.CustomerCode,
                W.CompanyName                        AS CustomerName,
                ISNULL(MC.CityName, W.City)          AS CityName,
                ISNULL(MS.StateName, W.State)        AS StateName,
                CAST(W.CompanyName + ' - ' + ISNULL(W.City, '') AS NVARCHAR(500)) AS DisplayName,
                CAST(W.CustomerPlace AS NVARCHAR(500)) AS Address,
                W.PINCode                            AS PinCode,
                W.gst                                AS Gst,
                W.pan                                AS Pan,
                MS.Ref_CountryId,
                MS.StateId                           AS Ref_StateId,
                MC.CityId                            AS Ref_CityId,
                W.ContactPerson,
                W.ContactNumber,
                W.Email
            FROM Rkl_WMS_Live.dbo.CustomerMaster W
            -- City/State are free text in WMS: match CRM masters by name when possible.
            OUTER APPLY (SELECT TOP 1 S.StateId, S.StateName, S.Ref_CountryId
                         FROM dbo.MST_State S
                         WHERE S.StateName = LTRIM(RTRIM(W.State))) WS
            OUTER APPLY (SELECT TOP 1 CI.CityId, CI.CityName, CI.Ref_StateId
                         FROM dbo.MST_City CI
                         WHERE CI.CityName = LTRIM(RTRIM(W.City))
                           AND (WS.StateId IS NULL OR CI.Ref_StateId = WS.StateId)
                         ORDER BY CI.CityId) MC
            OUTER APPLY (SELECT TOP 1 S.StateId, S.StateName, S.Ref_CountryId
                         FROM dbo.MST_State S
                         WHERE S.StateId = ISNULL(MC.Ref_StateId, WS.StateId)) MS
            WHERE (W.CompanyName LIKE @SearchText + '%'
                   OR W.CustomerCode LIKE @SearchText + '%'
                   OR W.gst LIKE @SearchText + '%'
                   OR W.pan LIKE @SearchText + '%')
              AND NOT EXISTS (SELECT 1 FROM dbo.CUS_Customer CU
                              WHERE CU.Ref_WmsCustomerMasterId = W.CustomerMasterID
                                AND CU.Ref_LeadOrganizationId IS NOT NULL)
            ORDER BY W.CompanyName
        ) WMS;

        RETURN;
    END;

    /* -------------------------------------------------------------
       ENSURE_WMS_CUSTOMER
       Returns the CUS_Customer row for a WMS customer, inserting it on
       first use. Optional @LeadId links the customer to that lead's
       organization so Lead -> Opportunity conversion
       (USP_CRM_OpportunityManagement, matches Ref_LeadOrganizationId)
       reuses this customer instead of creating a duplicate.
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

        IF NOT EXISTS (SELECT 1 FROM Rkl_WMS_Live.dbo.CustomerMaster
                       WHERE CustomerMasterID = @WmsCustomerMasterId)
        BEGIN
            RAISERROR('WMS customer not found.', 16, 1);
            RETURN;
        END;

        DECLARE @LeadOrgId INT = NULL;
        IF (@LeadId IS NOT NULL)
            SELECT @LeadOrgId = Ref_LeadOrganizationId
            FROM dbo.CRM_Lead
            WHERE LeadId = @LeadId;

        DECLARE @CustomerId INT = NULL, @IsNew BIT = 0;

        BEGIN TRY
            BEGIN TRAN;

            SELECT @CustomerId = CustomerId
            FROM dbo.CUS_Customer WITH (UPDLOCK, HOLDLOCK)
            WHERE Ref_WmsCustomerMasterId = @WmsCustomerMasterId;

            IF (@CustomerId IS NULL)
            BEGIN
                DECLARE @Code VARCHAR(20);
                -- Same code format as USP_CRM_OpportunityManagement conversion.
                SELECT @Code = 'CUST' + RIGHT('0000' + CAST(ISNULL(MAX(CustomerId), 0) + 1 AS VARCHAR(10)), 5)
                FROM dbo.CUS_Customer WITH (UPDLOCK, HOLDLOCK);

                INSERT INTO dbo.CUS_Customer
                (
                    CustomerCode, CustomerName, Address, Ref_CountryId, Ref_StateId, Ref_CityId,
                    PinCode, Gst, Pan, Ref_LeadOrganizationId, Ref_WmsCustomerMasterId,
                    IsActive, CreatedOn, CreatedBy
                )
                SELECT
                    @Code,
                    LEFT(W.CompanyName, 200),
                    LEFT(W.CustomerPlace, 500),
                    MS.Ref_CountryId,
                    MS.StateId,
                    MC.CityId,
                    LEFT(NULLIF(LTRIM(RTRIM(W.PINCode)), ''), 10),
                    LEFT(NULLIF(LTRIM(RTRIM(W.gst)), ''), 20),
                    LEFT(NULLIF(LTRIM(RTRIM(W.pan)), ''), 20),
                    @LeadOrgId,
                    W.CustomerMasterID,
                    1, GETDATE(), ISNULL(@CreatedBy, 0)
                FROM Rkl_WMS_Live.dbo.CustomerMaster W
                OUTER APPLY (SELECT TOP 1 S.StateId FROM dbo.MST_State S
                             WHERE S.StateName = LTRIM(RTRIM(W.State))) WS
                OUTER APPLY (SELECT TOP 1 CI.CityId, CI.Ref_StateId FROM dbo.MST_City CI
                             WHERE CI.CityName = LTRIM(RTRIM(W.City))
                               AND (WS.StateId IS NULL OR CI.Ref_StateId = WS.StateId)
                             ORDER BY CI.CityId) MC
                OUTER APPLY (SELECT TOP 1 S.StateId, S.Ref_CountryId FROM dbo.MST_State S
                             WHERE S.StateId = ISNULL(MC.Ref_StateId, WS.StateId)) MS
                WHERE W.CustomerMasterID = @WmsCustomerMasterId;

                SET @CustomerId = SCOPE_IDENTITY();
                SET @IsNew = 1;

                -- Primary contact from WMS, when present.
                INSERT INTO dbo.CUS_CustomerContact
                (
                    Ref_CustomerId, ContactPerson, Mobile, Email, Ref_DesignationId,
                    IsPrimary, IsActive, CreatedOn, CreatedBy
                )
                SELECT
                    @CustomerId,
                    LEFT(W.ContactPerson, 150),
                    LEFT(NULLIF(LTRIM(RTRIM(W.ContactNumber)), ''), 20),
                    LEFT(NULLIF(LTRIM(RTRIM(W.Email)), ''), 150),
                    NULL, 1, 1, GETDATE(), ISNULL(@CreatedBy, 0)
                FROM Rkl_WMS_Live.dbo.CustomerMaster W
                WHERE W.CustomerMasterID = @WmsCustomerMasterId
                  AND NULLIF(LTRIM(RTRIM(W.ContactPerson)), '') IS NOT NULL;
            END
            ELSE IF (@LeadOrgId IS NOT NULL)
            BEGIN
                -- Already mapped (e.g. used in a ticket first): link the lead org once.
                UPDATE dbo.CUS_Customer
                SET Ref_LeadOrganizationId = @LeadOrgId,
                    UpdatedOn = GETDATE(),
                    UpdatedBy = @CreatedBy
                WHERE CustomerId = @CustomerId
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
        WHERE CustomerId = @CustomerId;

        RETURN;
    END;

    RAISERROR('Invalid @Status.', 16, 1);
END;
GO
