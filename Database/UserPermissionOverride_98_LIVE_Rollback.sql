SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;  -- required (login SP uses FOR JSON). Run with: sqlcmd -I
GO
/* LIVE ROLLBACK for User Override. Restores LIVE SP definitions exported 2026-11-12 17:43 from Rkl_EnterpriseSuiteDB_live, then drops the function and table. */

-- ============================================================================
-- Author:      Suraj
-- Create date: 13-10-2026
-- Description: This stored procedure will handle Permissions Related Operations
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[USP_CRM_PermissionManagement]
(
    @Status                     VARCHAR(50),
    @EmployeeId                 INT = NULL,
    @PermissionCode             VARCHAR(100) = NULL,
    @ResourceType               VARCHAR(50) = NULL,
    @ResourceId                 INT = NULL,
    @Action                     VARCHAR(50) = NULL,
    -- NEW: Module management
    @ModuleId                   INT = NULL,
    @Ref_SuperModuleId          INT = NULL,
    @ModuleCode                 VARCHAR(50) = NULL,
    @ModuleName                 VARCHAR(100) = NULL,
    @Description                VARCHAR(255) = NULL,
    @IconName                   VARCHAR(50) = NULL,
    @DisplayOrder               INT = NULL,
    -- NEW: Permission catalog management
    @PermissionId               INT = NULL,
    @PermissionName             VARCHAR(200) = NULL,
    -- NEW: Role assignment
    @Ref_RoleId                 INT = NULL,
    @RolePermissionsJson        NVARCHAR(MAX) = NULL,   -- [{"PermissionId":11,"IsAllowed":true}, ...]
    @RoleModuleAccessJson       NVARCHAR(MAX) = NULL,  -- [{"ModuleId":1,"IsEnabled":false}, ...]
    @AssignedTo                 INT =NULL,
    @SuperModuleMappingJson     NVARCHAR(MAX) = NULL,   -- [{"ModuleId":1017,"IsMapped":true}, ...]
    @RoleSuperModuleAccessJson  NVARCHAR(MAX) = NULL,   -- [{"SuperModuleId":2,"IsEnabled":true}, ...]
    @CreatedBy                  INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;


IF(@Status = 'GET_USER_PERMISSIONS')
    BEGIN
        SELECT DISTINCT P.PermissionCode,P.PermissionName,M.ModuleCode,M.ModuleName,P.Description, CAST(1 AS BIT) AS IsGranted,P.PermissionId
        FROM ORG_EmployeeRole ER
        INNER JOIN ORG_RolePermission RP ON RP.Ref_RoleId = ER.Ref_RoleId AND RP.IsAllowed = 1
        INNER JOIN ORG_Permission P ON P.PermissionId = RP.Ref_PermissionId AND P.IsActive = 1
        INNER JOIN ORG_RoleModuleAccess RMA ON RMA.Ref_RoleId = ER.Ref_RoleId AND RMA.Ref_ModuleId = P.Ref_ModuleId AND RMA.IsEnabled = 1
        INNER JOIN ORG_Module M ON M.ModuleId = P.Ref_ModuleId AND M.IsActive = 1
        WHERE ER.Ref_EmployeeId = 11  AND ER.IsActive = 1 AND P.IsActive=1
        ORDER BY M.ModuleName,P.PermissionName;
    END

ELSE IF(@Status = 'GET_SUPER_MODULES')
    BEGIN
        SELECT SuperModuleId, SuperModuleCode, SuperModuleName, Description, IconName, DisplayOrder
        FROM ORG_SuperModule
        WHERE IsActive = 1
        ORDER BY ISNULL(DisplayOrder, 999), SuperModuleName;
    END

ELSE IF(@Status = 'GET_SUPERMODULE_MODULES')
BEGIN
    SELECT M.ModuleId, M.ModuleCode, M.ModuleName, M.IconName, M.DisplayOrder,
        CASE WHEN SMM.ModuleId IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsMapped
    FROM ORG_Module M
    LEFT JOIN ORG_SuperModuleMapping SMM ON SMM.ModuleId = M.ModuleId AND SMM.SuperModuleId = @Ref_SuperModuleId
    WHERE M.IsActive = 1
    ORDER BY ISNULL(M.DisplayOrder, 999), M.ModuleName;
END

ELSE IF(@Status = 'SAVE_SUPERMODULE_MAPPING')
    BEGIN
        BEGIN TRY
            BEGIN TRAN;

            MERGE ORG_SuperModuleMapping AS target
            USING (
                SELECT @Ref_SuperModuleId AS SuperModuleId, J.ModuleId, J.IsMapped
                FROM OPENJSON(@SuperModuleMappingJson)
                WITH (
                    ModuleId INT '$.ModuleId',
                    IsMapped BIT '$.IsMapped'
                ) J
            ) AS source
            ON target.SuperModuleId = source.SuperModuleId
               AND target.ModuleId = source.ModuleId

            WHEN MATCHED AND source.IsMapped = 0 THEN
                DELETE

            WHEN NOT MATCHED AND source.IsMapped = 1 THEN
                INSERT (SuperModuleId, ModuleId, CreatedBy)
                VALUES (source.SuperModuleId, source.ModuleId, @CreatedBy);

            COMMIT TRAN;
            SELECT CAST(1 AS BIT) AS Success, 'Mapping saved.' AS Message;
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0 ROLLBACK TRAN;
            SELECT CAST(0 AS BIT) AS Success, ERROR_MESSAGE() AS Message;
        END CATCH
    END

-- NEW: role <-> supermodule access (grants CRM/HRMS/ADMIN tab per role)

ELSE IF(@Status = 'GET_ROLE_SUPERMODULE_ACCESS')
    BEGIN
        SELECT SM.SuperModuleId,SM.SuperModuleCode,SM.SuperModuleName,SM.IconName,ISNULL(RSA.IsEnabled, 0) AS IsEnabled
        FROM ORG_SuperModule SM
        LEFT JOIN ORG_RoleSuperModuleAccess RSA ON RSA.Ref_SuperModuleId = SM.SuperModuleId
           AND RSA.Ref_RoleId = @Ref_RoleId
        WHERE SM.IsActive = 1
        ORDER BY ISNULL(SM.DisplayOrder, 999), SM.SuperModuleName;
    END

ELSE IF(@Status = 'SAVE_ROLE_SUPERMODULE_ACCESS')
    BEGIN
        BEGIN TRY
            BEGIN TRAN;

            MERGE ORG_RoleSuperModuleAccess AS target
            USING (
                SELECT @Ref_RoleId AS Ref_RoleId, J.SuperModuleId, J.IsEnabled
                FROM OPENJSON(@RoleSuperModuleAccessJson)
                WITH (
                    SuperModuleId INT '$.SuperModuleId',
                    IsEnabled     BIT '$.IsEnabled'
                ) J
            ) AS source
            ON target.Ref_RoleId = source.Ref_RoleId
               AND target.Ref_SuperModuleId = source.SuperModuleId

            WHEN MATCHED THEN
                UPDATE SET IsEnabled = source.IsEnabled,
                           UpdatedOn = GETDATE(),
                           UpdatedBy = @CreatedBy

            WHEN NOT MATCHED THEN
                INSERT (Ref_RoleId, Ref_SuperModuleId, IsEnabled, CreatedOn, CreatedBy)
                VALUES (source.Ref_RoleId, source.SuperModuleId, source.IsEnabled, GETDATE(), @CreatedBy);

            COMMIT TRAN;
            SELECT CAST(1 AS BIT) AS Success, 'Super module access saved.' AS Message;
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0 ROLLBACK TRAN;
            SELECT CAST(0 AS BIT) AS Success, ERROR_MESSAGE() AS Message;
        END CATCH
    END

ELSE IF(@Status = 'CHECK_PERMISSION')
    BEGIN

        SELECT CAST
        (
            CASE
                WHEN EXISTS
                (
                    SELECT 1
                    FROM ORG_EmployeeRole ER
                    INNER JOIN ORG_RolePermission RP ON RP.Ref_RoleId = ER.Ref_RoleId AND RP.IsAllowed = 1
                    INNER JOIN ORG_Permission P ON P.PermissionId = RP.Ref_PermissionId AND P.PermissionCode = @PermissionCode AND P.IsActive = 1
                    INNER JOIN ORG_RoleModuleAccess RMA ON RMA.Ref_RoleId = ER.Ref_RoleId AND RMA.Ref_ModuleId = P.Ref_ModuleId AND RMA.IsEnabled = 1
                    INNER JOIN ORG_Module M ON M.ModuleId = P.Ref_ModuleId AND M.IsActive = 1
                    WHERE ER.Ref_EmployeeId = @EmployeeId AND ER.IsActive = 1
                )
                THEN 1
                ELSE 0
            END
            AS BIT
        ) AS HasPermission;

    END
    -------------------------------------------------------------
    -- 3. CHECK MULTIPLE PERMISSIONS
    --    Returns multiple permissions check in one call
    --    Used when: Checking multiple permissions at once for performance
   ------------------------------------------------------------

ELSE IF(@Status = 'CHECK_MULTIPLE_PERMISSIONS')
    BEGIN

        SELECT C.PermissionCode,
            CAST
            (
                CASE
                    WHEN EXISTS
                    (
                        SELECT 1
                        FROM ORG_EmployeeRole ER
                        INNER JOIN ORG_RolePermission RP ON RP.Ref_RoleId = ER.Ref_RoleId AND RP.IsAllowed = 1
                        INNER JOIN ORG_Permission P ON P.PermissionId = RP.Ref_PermissionId AND P.PermissionCode = C.PermissionCode AND P.IsActive = 1
                        INNER JOIN ORG_RoleModuleAccess RMA ON RMA.Ref_RoleId = ER.Ref_RoleId AND RMA.Ref_ModuleId = P.Ref_ModuleId AND RMA.IsEnabled = 1
                        WHERE ER.Ref_EmployeeId = @EmployeeId
                          AND ER.IsActive = 1
                    )
                    THEN 1
                    ELSE 0
                END
                AS BIT
            ) AS HasPermission

        FROM
        (
            SELECT TRIM(value) AS PermissionCode
            FROM STRING_SPLIT(@PermissionCode, ',')
        ) C;

    END

    -------------------------------------------------------------
    -- 4. GET RESOURCE OWNER
    --    Returns who created and who is assigned to a resource
    --    Used when: Checking ownership for resource-level permissions
    -------------------------------------------------------------  

ELSE IF(@Status = 'GET_RESOURCE_OWNER')
    BEGIN
        IF @ResourceType = 'Lead'
        BEGIN
            SELECT 
                LeadId AS ResourceId,CreatedBy,Ref_CurrentAssignedEmployeeId AS AssignedEmployeeId,'Lead' AS ResourceType
            FROM CRM_Lead
            WHERE LeadId = @ResourceId;
        END
        ELSE IF @ResourceType = 'Opportunity' OR @ResourceType = 'Opp'
        BEGIN
            SELECT 
                OpportunityId AS ResourceId,CreatedBy,Ref_OwnerEmployeeId AS AssignedEmployeeId,'Opportunity' AS ResourceType
            FROM CRM_Opportunity
            WHERE OpportunityId = @ResourceId
                AND IsActive = 1;
        END
        ELSE
        BEGIN
            -- Return empty result for unknown resource type
            SELECT NULL AS ResourceId, NULL AS CreatedBy, NULL AS AssignedEmployeeId, NULL AS ResourceType
            WHERE 1 = 0;
        END
    END

    -------------------------------------------------------------
    -- 5. CHECK RESOURCE ACCESS WITH OWNERSHIP
    --    Comprehensive check: permission + ownership validation
    --    Used when: Checking if user can access specific resource
    -------------------------------------------------------------

ELSE IF(@Status = 'CHECK_RESOURCE_ACCESS')
    BEGIN
        DECLARE @HasAccess BIT = 0;
        DECLARE @CheckPermissionCode VARCHAR(100);
        DECLARE @ResourceCreatedBy INT, @ResourceAssignedTo INT;   -- renamed, local-only
        
        -- Step 1: Does user have ViewAll permission? (Super access)
        SET @CheckPermissionCode = @ResourceType + '_ViewAll';
        
        IF EXISTS (SELECT 1 FROM ORG_EmployeeRole er
            INNER JOIN ORG_RolePermission rp ON er.Ref_RoleId = rp.Ref_RoleId
            INNER JOIN ORG_Permission p ON rp.Ref_PermissionId = p.PermissionId
            WHERE er.Ref_EmployeeId = @EmployeeId
              AND er.IsActive = 1
              AND rp.IsAllowed = 1
              AND p.PermissionCode = @CheckPermissionCode
              AND p.IsActive = 1
        )
        BEGIN
            SET @HasAccess = 1;
        END
        ELSE
        BEGIN
            -- Step 2: Does user have specific action permission?
            IF @Action IS NOT NULL
            BEGIN
                SET @CheckPermissionCode = @ResourceType + '_' + @Action;
                
                IF EXISTS (
                    SELECT 1 
                    FROM ORG_EmployeeRole er
                    INNER JOIN ORG_RolePermission rp ON er.Ref_RoleId = rp.Ref_RoleId
                    INNER JOIN ORG_Permission p ON rp.Ref_PermissionId = p.PermissionId
                    WHERE er.Ref_EmployeeId = @EmployeeId
                      AND er.IsActive = 1
                      AND rp.IsAllowed = 1
                      AND p.PermissionCode = @CheckPermissionCode
                      AND p.IsActive = 1
                )
                BEGIN
                    -- Step 3: Check ownership (except for Create action)
                    IF @Action = 'Create'
                    BEGIN
                        SET @HasAccess = 1;
                    END
                    ELSE
                    BEGIN                                             
                        -- Get resource owner info based on resource type
                        IF @ResourceType = 'Lead'
                        BEGIN
                            SELECT 
                                @CreatedBy = CreatedBy,
                                @AssignedTo = Ref_CurrentAssignedEmployeeId
                            FROM CRM_Lead
                            WHERE LeadId = @ResourceId
                              --AND IsActive = 1;
                        END
                        ELSE IF @ResourceType = 'Opportunity'
                        BEGIN
                            SELECT 
                                @CreatedBy = CreatedBy,
                                @AssignedTo = Ref_OwnerEmployeeId
                            FROM CRM_Opportunity
                            WHERE OpportunityId = @ResourceId
                              AND IsActive = 1;
                        END
                        
                        -- User can access if they are creator OR assigned person
                        IF @CreatedBy = @EmployeeId OR @AssignedTo = @EmployeeId
                        BEGIN
                            SET @HasAccess = 1;
                        END
                    END
                END
            END
        END
        
        SELECT @HasAccess AS HasAccess;
    END

    -------------------------------------------------------------
    -- 6. GET USER ROLES WITH PERMISSIONS (For auditing/debugging)
    --    Returns all roles and their permissions for a user
    --    Used when: Debugging or displaying user permissions in UI
    -------------------------------------------------------------
ELSE IF(@Status = 'GET_USER_ROLES_PERMISSIONS')
    BEGIN
        SELECT 
            r.RoleId,r.RoleCode,r.RoleName,r.Description AS RoleDescription,er.IsPrimaryRole,p.PermissionCode,p.PermissionName,
            p.ModuleName,p.Description AS PermissionDescription,rp.IsAllowed,
            CASE WHEN rp.IsAllowed = 1 THEN 'Granted' ELSE 'Denied' END AS PermissionStatus
        FROM ORG_EmployeeRole er
        INNER JOIN ORG_Role r ON er.Ref_RoleId = r.RoleId
        LEFT JOIN ORG_RolePermission rp ON r.RoleId = rp.Ref_RoleId
        LEFT JOIN ORG_Permission p ON rp.Ref_PermissionId = p.PermissionId
        WHERE er.Ref_EmployeeId = 11
          AND er.IsActive = 1
          AND r.IsActive = 1
          AND (p.IsActive = 1 OR p.PermissionId IS NULL)
        ORDER BY r.RoleCode, p.ModuleName, p.PermissionName;
    END

    -------------------------------------------------------------
    -- 7. GET USER MODULES (For building menu dynamically)
    --    Returns all modules user has access to
    --    Used when: Building dynamic menus based on user permissions
    -------------------------------------------------------------
ELSE IF(@Status = 'GET_USER_MODULES_Old')
    BEGIN
        SELECT DISTINCT p.ModuleName,MIN(p.ModuleName) AS ModuleDisplayName,COUNT(p.PermissionCode) AS TotalPermissions
        FROM ORG_EmployeeRole er
        INNER JOIN ORG_RolePermission rp ON er.Ref_RoleId = rp.Ref_RoleId
        INNER JOIN ORG_Permission p ON rp.Ref_PermissionId = p.PermissionId
        WHERE er.Ref_EmployeeId = @EmployeeId
          AND er.IsActive = 1
          AND rp.IsAllowed = 1
          AND p.IsActive = 1
        GROUP BY p.ModuleName
        ORDER BY p.ModuleName;
    END

ELSE IF(@Status = 'GET_USER_MODULES')
    BEGIN

        SELECT DISTINCT M.ModuleId,M.ModuleCode,M.ModuleName,M.Description,M.IconName,M.DisplayOrder,
            CAST(1 AS BIT) AS IsEnabled,
            (
                SELECT COUNT(1) FROM ORG_Permission P2 WHERE P2.Ref_ModuleId = M.ModuleId
                  AND P2.IsActive = 1
            ) AS TotalPermissions

        FROM ORG_EmployeeRole ER
        INNER JOIN ORG_RoleModuleAccess RMA ON RMA.Ref_RoleId = ER.Ref_RoleId AND RMA.IsEnabled = 1
        INNER JOIN ORG_Module M ON M.ModuleId = RMA.Ref_ModuleId AND M.IsActive = 1
        WHERE ER.Ref_EmployeeId = @EmployeeId AND ER.IsActive = 1
        ORDER BY M.DisplayOrder, M.ModuleName;

    END


    -------------------------------------------------------------
    -- 8. BATCH PERMISSION CHECK (For performance)
    --    Returns all permissions with flag if user has them
    --    Used when: Loading all permissions at once for caching
    -------------------------------------------------------------
ELSE IF(@Status = 'BATCH_PERMISSION_CHECK_Old')
    BEGIN
        SELECT 
            p.PermissionCode,p.PermissionName,p.ModuleName,
            CAST(CASE 
                WHEN EXISTS ( SELECT 1 FROM ORG_EmployeeRole er
                    INNER JOIN ORG_RolePermission rp ON er.Ref_RoleId = rp.Ref_RoleId
                    WHERE er.Ref_EmployeeId = @EmployeeId
                      AND er.IsActive = 1 AND rp.IsAllowed = 1 AND rp.Ref_PermissionId = p.PermissionId
                ) THEN 1 ELSE 0 
            END AS BIT) AS HasPermission
        FROM ORG_Permission p
        WHERE p.IsActive = 1
        ORDER BY p.ModuleName, p.PermissionName;
    END

ELSE IF(@Status = 'BATCH_PERMISSION_CHECK')
    BEGIN

        SELECT P.PermissionCode,P.PermissionName,P.ModuleName,
            CAST
            (
                CASE
                    WHEN EXISTS
                    (
                        SELECT 1 FROM ORG_EmployeeRole ER
                        INNER JOIN ORG_RolePermission RP ON RP.Ref_RoleId = ER.Ref_RoleId AND RP.Ref_PermissionId = P.PermissionId AND RP.IsAllowed = 1
                        INNER JOIN ORG_RoleModuleAccess RMA ON RMA.Ref_RoleId = ER.Ref_RoleId AND RMA.Ref_ModuleId = P.Ref_ModuleId AND RMA.IsEnabled = 1
                        WHERE ER.Ref_EmployeeId = @EmployeeId
                          AND ER.IsActive = 1
                    )
                    THEN 1
                    ELSE 0
                END
                AS BIT
            ) AS HasPermission

        FROM ORG_Permission P 
        INNER JOIN ORG_Module M ON M.ModuleId = P.Ref_ModuleId AND M.IsActive = 1
        WHERE P.IsActive = 1
        ORDER BY M.DisplayOrder,M.ModuleName,P.PermissionName;
    END
    -------------------------------------------------------------
    -- 9. GET ALL PERMISSIONS (For admin/configuration)
    --    Returns all available permissions in system
    --    Used when: Admin wants to see all permissions
    -------------------------------------------------------------
ELSE IF(@Status = 'GET_ALL_PERMISSIONS')
    BEGIN
        SELECT 
            PermissionId,ModuleName,PermissionCode,PermissionName, Description,IsActive,CreatedOn,CreatedBy
        FROM ORG_Permission
        WHERE IsActive = 1
        ORDER BY ModuleName, PermissionName;
    END

-------------------------------------------------------------
    -- 10. CREATE_MODULE
    -------------------------------------------------------------
ELSE IF(@Status = 'CREATE_MODULE')
    BEGIN
        IF EXISTS (SELECT 1 FROM ORG_Module WHERE ModuleCode = @ModuleCode)
        BEGIN
            SELECT CAST(0 AS BIT) AS Success, 'Module code already exists.' AS Message;
            RETURN;
        END

        INSERT INTO ORG_Module (ModuleCode, ModuleName, Description, IconName, DisplayOrder, IsActive, CreatedOn, CreatedBy)
        VALUES (@ModuleCode, @ModuleName, @Description, @IconName, @DisplayOrder, 1, GETDATE(), @CreatedBy);

        SELECT CAST(1 AS BIT) AS Success, 'Module created.' AS Message, SCOPE_IDENTITY() AS ModuleId;
    END

    -------------------------------------------------------------
    -- 11. UPDATE_MODULE
    -------------------------------------------------------------
ELSE IF(@Status = 'UPDATE_MODULE')
    BEGIN
        -- ModuleCode is immutable by design (referenced elsewhere) � only these are editable
        UPDATE ORG_Module
        SET ModuleName    = ISNULL(@ModuleName, ModuleName),
            Description   = @Description,
            IconName      = @IconName,
            DisplayOrder  = @DisplayOrder,
            UpdatedOn     = GETDATE(),
            UpdatedBy     = @CreatedBy
        WHERE ModuleId    = @ModuleId;

        SELECT CAST(1 AS BIT) AS Success, 'Module updated.' AS Message;
    END

    -------------------------------------------------------------
    -- 12. DEACTIVATE_MODULE (soft delete only � see prior discussion)
    -------------------------------------------------------------
ELSE IF(@Status = 'DEACTIVATE_MODULE')
    BEGIN
        UPDATE ORG_Module
        SET IsActive = 0, UpdatedOn = GETDATE(), UpdatedBy = @CreatedBy
        WHERE ModuleId = @ModuleId;

        SELECT CAST(1 AS BIT) AS Success, 'Module deactivated.' AS Message;
    END

    -------------------------------------------------------------
    -- 13. GET_MODULES (Screen 1 & 3 � module list with permission counts)
    -------------------------------------------------------------
ELSE IF(@Status = 'GET_MODULES')
    BEGIN
        SELECT
            M.ModuleId, M.ModuleCode, M.ModuleName, M.Description, M.IconName,
            M.DisplayOrder, M.IsActive,
            COUNT(P.PermissionId) AS TotalPermissions
        FROM ORG_Module M
        LEFT JOIN ORG_Permission P ON P.Ref_ModuleId = M.ModuleId AND P.IsActive = 1
        WHERE M.IsActive = 1
        GROUP BY M.ModuleId, M.ModuleCode, M.ModuleName, M.Description, M.IconName, M.DisplayOrder, M.IsActive
        ORDER BY ISNULL(M.DisplayOrder, 999), M.ModuleName;
    END

    -------------------------------------------------------------
    -- 14. CREATE_PERMISSION
    -------------------------------------------------------------
ELSE IF(@Status = 'CREATE_PERMISSION')
    BEGIN
        IF EXISTS (SELECT 1 FROM ORG_Permission WHERE PermissionCode = @PermissionCode)
        BEGIN
            SELECT CAST(0 AS BIT) AS Success, 'Permission code already exists.' AS Message;
            RETURN;
        END

        DECLARE @ResolvedModuleName VARCHAR(100) = (SELECT ModuleName FROM ORG_Module WHERE ModuleId = @ModuleId);

        IF @ResolvedModuleName IS NULL
        BEGIN
            SELECT CAST(0 AS BIT) AS Success, 'Invalid ModuleId.' AS Message;
            RETURN;
        END

        INSERT INTO ORG_Permission (ModuleName, Ref_ModuleId, PermissionCode, PermissionName, Description, IsActive, CreatedOn, CreatedBy)
        VALUES (@ResolvedModuleName, @ModuleId, @PermissionCode, @PermissionName, @Description, 1, GETDATE(), @CreatedBy);

        SELECT CAST(1 AS BIT) AS Success, 'Permission created.' AS Message, SCOPE_IDENTITY() AS PermissionId;
    END

    -------------------------------------------------------------
    -- 15. UPDATE_PERMISSION (PermissionCode is immutable � see prior discussion)
    -------------------------------------------------------------
ELSE IF(@Status = 'UPDATE_PERMISSION')
    BEGIN
        UPDATE ORG_Permission
        SET PermissionName = ISNULL(@PermissionName, PermissionName),
            Description    = @Description,
            UpdatedOn      = GETDATE(),
            UpdatedBy      = @CreatedBy
        WHERE PermissionId = @PermissionId;

        SELECT CAST(1 AS BIT) AS Success, 'Permission updated.' AS Message;
    END

    -------------------------------------------------------------
    -- 16. DEACTIVATE_PERMISSION (soft delete only)
    -------------------------------------------------------------
ELSE IF(@Status = 'DEACTIVATE_PERMISSION')
    BEGIN
        UPDATE ORG_Permission
        SET IsActive = 0, UpdatedOn = GETDATE(), UpdatedBy = @CreatedBy
        WHERE PermissionId = @PermissionId;

        SELECT CAST(1 AS BIT) AS Success, 'Permission deactivated.' AS Message;
    END

    -------------------------------------------------------------
    -- 17. GET_MODULE_PERMISSIONS (Screen 3 � catalog list for one module)
    -------------------------------------------------------------
ELSE IF(@Status = 'GET_MODULE_PERMISSIONS')
    BEGIN
        SELECT
            P.PermissionId, P.PermissionCode, P.PermissionName, P.Description, P.IsActive
        FROM ORG_Permission P
        WHERE P.Ref_ModuleId = @ModuleId
          AND P.IsActive = 1
        ORDER BY P.PermissionName;
    END

    -------------------------------------------------------------
    -- 18. GET_ROLE_PERMISSIONS (Screen 2 � checklist state for a role, grouped by module)
    -------------------------------------------------------------
ELSE IF(@Status = 'GET_ROLE_PERMISSIONS_old')
    BEGIN
        SELECT
            M.ModuleId, M.ModuleName,
            P.PermissionId, P.PermissionCode, P.PermissionName, P.Description,
            ISNULL(RP.IsAllowed, 0) AS IsAllowed
        FROM ORG_Permission P
        INNER JOIN ORG_Module M ON M.ModuleId = P.Ref_ModuleId
        LEFT JOIN ORG_RolePermission RP ON RP.Ref_PermissionId = P.PermissionId
           AND RP.Ref_RoleId = @Ref_RoleId
        WHERE P.IsActive = 1
          AND M.IsActive = 1
          AND (@ModuleId IS NULL OR M.ModuleId = @ModuleId)
        ORDER BY M.DisplayOrder, M.ModuleName, P.PermissionName;
    END

ELSE IF(@Status = 'GET_ROLE_PERMISSIONS')
    BEGIN

        SELECT M.ModuleId,M.ModuleName,P.PermissionId,P.PermissionCode,P.PermissionName,P.Description,
            ISNULL(RMA.IsEnabled, 0) AS ModuleIsAllowed,ISNULL(RP.IsAllowed, 0) AS IsAllowed
        FROM ORG_Module M
        INNER JOIN ORG_SuperModuleMapping SMM ON SMM.ModuleId = M.ModuleId AND (@Ref_SuperModuleId IS NULL OR SMM.SuperModuleId = @Ref_SuperModuleId) 
        LEFT JOIN ORG_RoleModuleAccess RMA ON RMA.Ref_ModuleId = M.ModuleId AND RMA.Ref_RoleId = @Ref_RoleId
        LEFT JOIN ORG_Permission P ON P.Ref_ModuleId = M.ModuleId AND P.IsActive = 1 
        LEFT JOIN ORG_RolePermission RP ON RP.Ref_PermissionId = P.PermissionId AND RP.Ref_RoleId = @Ref_RoleId
        WHERE M.IsActive = 1 AND (@ModuleId IS NULL OR M.ModuleId = @ModuleId)
        ORDER BY M.DisplayOrder, M.ModuleName, P.PermissionName;

    END


ELSE IF(@Status = 'GET_ROLE_PERMISSIONS_New(080926)')
    BEGIN

        SELECT M.ModuleId,M.ModuleName,P.PermissionId,P.PermissionCode,P.PermissionName,P.Description,      
            -- Module-level access
            ISNULL(RMA.IsEnabled, 0) AS ModuleIsAllowed,    
            -- Permission-level access       
            ISNULL(RP.IsAllowed, 0) AS IsAllowed
        FROM ORG_Module M
        INNER JOIN ORG_SuperModuleMapping SMM ON SMM.ModuleId = M.ModuleId
           AND (@Ref_SuperModuleId IS NULL OR SMM.SuperModuleId = @Ref_SuperModuleId)  

        -- Module access for selected role
        LEFT JOIN ORG_RoleModuleAccess RMA ON RMA.Ref_ModuleId = M.ModuleId
           AND RMA.Ref_RoleId = @Ref_RoleId
   
        -- Permissions belonging to module   
        LEFT JOIN ORG_Permission P ON P.Ref_ModuleId = M.ModuleId
           AND P.IsActive = 1
  
        -- Role permission    
        LEFT JOIN ORG_RolePermission RP ON RP.Ref_PermissionId = P.PermissionId
           AND RP.Ref_RoleId = @Ref_RoleId

        WHERE M.IsActive = 1
          AND
          (
              @ModuleId IS NULL
              OR M.ModuleId = @ModuleId
          )
        ORDER BY M.DisplayOrder,M.ModuleName,P.PermissionName;

    END
    -------------------------------------------------------------
    -- 19. SAVE_ROLE_PERMISSIONS (Screen 2 � delta-only save via MERGE)
    -- @RolePermissionsJson: [{"PermissionId":11,"IsAllowed":true}, ...]
    -------------------------------------------------------------
ELSE IF(@Status = 'SAVE_ROLE_PERMISSIONS')
    BEGIN
        BEGIN TRY
            BEGIN TRAN;

            MERGE ORG_RolePermission AS target
            USING (
                SELECT
                    @Ref_RoleId AS Ref_RoleId,
                    J.PermissionId,
                    J.IsAllowed
                FROM OPENJSON(@RolePermissionsJson)
                WITH (
                    PermissionId INT      '$.PermissionId',
                    IsAllowed    BIT      '$.IsAllowed'
                ) J
            ) AS source
            ON target.Ref_RoleId = source.Ref_RoleId
               AND target.Ref_PermissionId = source.PermissionId

            WHEN MATCHED THEN
                UPDATE SET IsAllowed = source.IsAllowed,
                           UpdatedOn = GETDATE(),
                           UpdatedBy = @CreatedBy

            WHEN NOT MATCHED THEN
                INSERT (Ref_RoleId, Ref_PermissionId, IsAllowed, CreatedOn, CreatedBy)
                VALUES (source.Ref_RoleId, source.PermissionId, source.IsAllowed, GETDATE(), @CreatedBy);

            COMMIT TRAN;
            SELECT CAST(1 AS BIT) AS Success, 'Permissions saved.' AS Message;
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0 ROLLBACK TRAN;
            SELECT CAST(0 AS BIT) AS Success, ERROR_MESSAGE() AS Message;
        END CATCH
    END

    -------------------------------------------------------------
    -- 20. GET_ROLE_MODULE_ACCESS (Screen 1 � module toggle state for a role)
    -- No row in ORG_RoleModuleAccess = enabled (per our fail-open design)
    -------------------------------------------------------------
ELSE IF(@Status = 'GET_ROLE_MODULE_ACCESS_Old')
    BEGIN
        SELECT
            M.ModuleId, M.ModuleName, M.Description, M.IconName,
            ISNULL(RMA.IsEnabled, 1) AS IsEnabled
        FROM ORG_Module M
        LEFT JOIN ORG_RoleModuleAccess RMA ON RMA.Ref_ModuleId = M.ModuleId
           AND RMA.Ref_RoleId = @Ref_RoleId
        WHERE M.IsActive = 1
        ORDER BY ISNULL(M.DisplayOrder, 999), M.ModuleName;
    END

ELSE IF(@Status = 'GET_ROLE_MODULE_ACCESS')
    BEGIN

        SELECT M.ModuleId,M.ModuleCode,M.ModuleName,M.Description,M.IconName,M.DisplayOrder,ISNULL(RMA.IsEnabled, 0) AS IsEnabled
        FROM ORG_Module M
        LEFT JOIN ORG_RoleModuleAccess RMA ON RMA.Ref_ModuleId = M.ModuleId
           AND RMA.Ref_RoleId = @Ref_RoleId
        WHERE M.IsActive = 1
        ORDER BY ISNULL(M.DisplayOrder, 999),M.ModuleName;
    END
    -------------------------------------------------------------
    -- 21. SAVE_ROLE_MODULE_ACCESS (Screen 1 � delta-only save via MERGE)
    -- @RoleModuleAccessJson: [{"ModuleId":1,"IsEnabled":false}, ...]
    -------------------------------------------------------------
ELSE IF(@Status = 'SAVE_ROLE_MODULE_ACCESS')
    BEGIN
        BEGIN TRY
            BEGIN TRAN;

            MERGE ORG_RoleModuleAccess AS target
            USING (
                SELECT
                    @Ref_RoleId AS Ref_RoleId,
                    J.ModuleId,
                    J.IsEnabled
                FROM OPENJSON(@RoleModuleAccessJson)
                WITH (
                    ModuleId  INT '$.ModuleId',
                    IsEnabled BIT '$.IsEnabled'
                ) J
            ) AS source
            ON target.Ref_RoleId = source.Ref_RoleId
               AND target.Ref_ModuleId = source.ModuleId

            WHEN MATCHED THEN
                UPDATE SET IsEnabled = source.IsEnabled,
                           UpdatedOn = GETDATE(),
                           UpdatedBy = @CreatedBy

            WHEN NOT MATCHED THEN
                INSERT (Ref_RoleId, Ref_ModuleId, IsEnabled, CreatedOn, CreatedBy)
                VALUES (source.Ref_RoleId, source.ModuleId, source.IsEnabled, GETDATE(), @CreatedBy);

            COMMIT TRAN;
            SELECT CAST(1 AS BIT) AS Success, 'Module access saved.' AS Message;
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0 ROLLBACK TRAN;
            SELECT CAST(0 AS BIT) AS Success, ERROR_MESSAGE() AS Message;
        END CATCH
    END

ELSE IF(@Status = 'SAVE_ROLE_ACCESS')
BEGIN
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;
     
        -- 1. Validate Role       

        IF NOT EXISTS
        (
            SELECT 1 FROM ORG_Role
            WHERE RoleId = @Ref_RoleId
              AND IsActive = 1
        )
        BEGIN
            THROW 50001, 'Invalid or inactive RoleId.', 1;
        END;
        
        -- 2. Parse Module Access     

        DECLARE @Modules TABLE
        (
            ModuleId INT NOT NULL PRIMARY KEY,IsEnabled BIT NOT NULL
        );

        INSERT INTO @Modules
        (
            ModuleId,IsEnabled
        )
        SELECT
            J.ModuleId,
            ISNULL(J.IsEnabled, 0)
        FROM OPENJSON(ISNULL(@RoleModuleAccessJson, '[]'))
        WITH
        (
            ModuleId INT '$.ModuleId',
            IsEnabled BIT '$.IsEnabled'
        ) J
        WHERE J.ModuleId IS NOT NULL;
               
        -- 2b. Parse SuperModule Access (NEW)    

        DECLARE @SuperModules TABLE
        (
            SuperModuleId INT NOT NULL PRIMARY KEY,
            IsEnabled BIT NOT NULL
        );

        INSERT INTO @SuperModules
        (
            SuperModuleId,
            IsEnabled
        )
        SELECT
            J.SuperModuleId,
            ISNULL(J.IsEnabled, 0)
        FROM OPENJSON(ISNULL(@RoleSuperModuleAccessJson, '[]'))
        WITH
        (
            SuperModuleId INT '$.SuperModuleId',
            IsEnabled BIT '$.IsEnabled'
        ) J
        WHERE J.SuperModuleId IS NOT NULL;
        
        -- 3b. Validate SuperModule IDs (NEW)     

        IF EXISTS
        (
            SELECT 1 FROM @SuperModules X
            LEFT JOIN ORG_SuperModule SM ON SM.SuperModuleId = X.SuperModuleId
               AND SM.IsActive = 1
            WHERE SM.SuperModuleId IS NULL
        )
        BEGIN
            DECLARE @InvalidSuperModuleIds VARCHAR(MAX);
            DECLARE @SuperModuleErrorMessage NVARCHAR(2048);

            SELECT
                @InvalidSuperModuleIds = STRING_AGG(CAST(X.SuperModuleId AS VARCHAR(20)),', ')
            FROM @SuperModules X
            LEFT JOIN ORG_SuperModule SM ON SM.SuperModuleId = X.SuperModuleId AND SM.IsActive = 1
            WHERE SM.SuperModuleId IS NULL;

            SET @SuperModuleErrorMessage =N'Invalid or inactive SuperModuleId(s): '+ ISNULL(@InvalidSuperModuleIds, '');
            THROW 50004, @SuperModuleErrorMessage, 1;
        END;
      
        -- 3. Validate Module IDs
        IF EXISTS
        (
            SELECT 1 FROM @Modules X 
            LEFT JOIN ORG_Module M ON M.ModuleId = X.ModuleId
               AND M.IsActive = 1
            WHERE M.ModuleId IS NULL
        )
        BEGIN
            DECLARE @InvalidModuleIds VARCHAR(MAX);
            DECLARE @ModuleErrorMessage NVARCHAR(2048);

            SELECT
                @InvalidModuleIds = STRING_AGG(CAST(X.ModuleId AS VARCHAR(20)),', ')
            FROM @Modules X
            LEFT JOIN ORG_Module M ON M.ModuleId = X.ModuleId AND M.IsActive = 1
            WHERE M.ModuleId IS NULL;

            SET @ModuleErrorMessage =N'Invalid or inactive ModuleId(s): '+ ISNULL(@InvalidModuleIds, '');
            THROW 50002, @ModuleErrorMessage, 1;
        END;
     
        -- 4. Parse Permission Access     
        DECLARE @Permissions TABLE
        (
            PermissionId INT NOT NULL PRIMARY KEY,
            IsAllowed BIT NOT NULL
        );

        INSERT INTO @Permissions
        (
            PermissionId,
            IsAllowed
        )
        SELECT
            J.PermissionId,
            ISNULL(J.IsAllowed, 0)
        FROM OPENJSON(ISNULL(@RolePermissionsJson, '[]'))
        WITH
        (
            PermissionId INT '$.PermissionId',
            IsAllowed BIT '$.IsAllowed'
        ) J
        WHERE J.PermissionId IS NOT NULL
          AND J.PermissionId > 0;

        -- 5. Validate Permission IDs
        IF EXISTS
        (
            SELECT 1 FROM @Permissions X
            LEFT JOIN ORG_Permission P ON P.PermissionId = X.PermissionId
               AND P.IsActive = 1
            WHERE P.PermissionId IS NULL
        )
        BEGIN
            DECLARE @InvalidPermissionIds VARCHAR(MAX);
            DECLARE @PermissionErrorMessage NVARCHAR(2048);

            SELECT
                @InvalidPermissionIds =STRING_AGG(CAST(X.PermissionId AS VARCHAR(20)),', ')
            FROM @Permissions X
            LEFT JOIN ORG_Permission P ON P.PermissionId = X.PermissionId
               AND P.IsActive = 1
            WHERE P.PermissionId IS NULL;

            SET @PermissionErrorMessage =N'Invalid or inactive PermissionId(s): '+ ISNULL(@InvalidPermissionIds, '');
            THROW 50003, @PermissionErrorMessage, 1;
        END;

        -- 6. Update Existing Module Access       
        UPDATE RMA
        SET
            RMA.IsEnabled = X.IsEnabled,
            RMA.UpdatedOn = GETDATE(),
            RMA.UpdatedBy = @CreatedBy
        FROM ORG_RoleModuleAccess RMA
        INNER JOIN @Modules X ON X.ModuleId = RMA.Ref_ModuleId
        WHERE RMA.Ref_RoleId = @Ref_RoleId;

        -- 7. Insert New Module Access
        INSERT INTO ORG_RoleModuleAccess
        (
            Ref_RoleId,Ref_ModuleId,IsEnabled,CreatedOn,CreatedBy
        )
        SELECT @Ref_RoleId,X.ModuleId,X.IsEnabled,GETDATE(),@CreatedBy
        FROM @Modules X
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM ORG_RoleModuleAccess RMA WITH (UPDLOCK, HOLDLOCK)
            WHERE RMA.Ref_RoleId = @Ref_RoleId
              AND RMA.Ref_ModuleId = X.ModuleId
        );
           
        -- 7b. Update Existing SuperModule Access (NEW)     

        UPDATE RSA
        SET
            RSA.IsEnabled = X.IsEnabled,
            RSA.UpdatedOn = GETDATE(),
            RSA.UpdatedBy = @CreatedBy
        FROM ORG_RoleSuperModuleAccess RSA
        INNER JOIN @SuperModules X ON X.SuperModuleId = RSA.Ref_SuperModuleId
        WHERE RSA.Ref_RoleId = @Ref_RoleId;
   
        -- 7c. Insert New SuperModule Access (NEW)
        INSERT INTO ORG_RoleSuperModuleAccess
        (
            Ref_RoleId,Ref_SuperModuleId,IsEnabled,CreatedOn,CreatedBy
        )
        SELECT @Ref_RoleId,X.SuperModuleId,X.IsEnabled,GETDATE(),@CreatedBy
        FROM @SuperModules X
        WHERE NOT EXISTS
        (
            SELECT 1 FROM ORG_RoleSuperModuleAccess RSA WITH (UPDLOCK, HOLDLOCK)
            WHERE RSA.Ref_RoleId = @Ref_RoleId
              AND RSA.Ref_SuperModuleId = X.SuperModuleId
        );
        
        -- 8. Update Existing Permission Access
        UPDATE RP
        SET
            RP.IsAllowed = X.IsAllowed,
            RP.UpdatedOn = GETDATE(),
            RP.UpdatedBy = @CreatedBy
        FROM ORG_RolePermission RP
        INNER JOIN @Permissions X ON X.PermissionId = RP.Ref_PermissionId
        WHERE RP.Ref_RoleId = @Ref_RoleId;
      
        -- 9. Insert New Permission Access
        INSERT INTO ORG_RolePermission
        (
            Ref_RoleId,Ref_PermissionId,IsAllowed,CreatedOn,CreatedBy
        )
        SELECT @Ref_RoleId,X.PermissionId,X.IsAllowed,GETDATE(),@CreatedBy
        FROM @Permissions X
        WHERE NOT EXISTS
        (
            SELECT 1 FROM ORG_RolePermission RP WITH (UPDLOCK, HOLDLOCK)
            WHERE RP.Ref_RoleId = @Ref_RoleId
              AND RP.Ref_PermissionId = X.PermissionId
        );
      
        -- 10. SUCCESS
        COMMIT TRANSACTION;

        SELECT CAST(1 AS BIT) AS Success,'Module and permission access saved successfully.' AS Message;

    END TRY

    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT CAST(0 AS BIT) AS Success,ERROR_MESSAGE() AS Message;
    END CATCH
END


END
GO

-- ============================================================================
-- Author:      Suraj
-- Create date: 13-10-2026
-- Description: This stored procedure will handle Authentication Related Operations
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[USP_ORG_Authentication]
(
    @Status             VARCHAR(30),
    @LoginId            VARCHAR(100) = NULL,
    @EmployeeId         INT = NULL,
    @DeviceId           VARCHAR(200) = NULL,
    @DeviceToken        VARCHAR(1000) = NULL,
    @DeviceType         VARCHAR(50) = NULL,
    @AppVersion         VARCHAR(50) = NULL,
    @OTPHash            VARCHAR(500) = NULL,
    @OTP                VARCHAR(10) = NULL,
    @NewPassword        VARCHAR(500) = NULL,
    @ResetToken         UNIQUEIDENTIFIER = NULL
)
AS
BEGIN
SET NOCOUNT ON;
IF(@Status='LOGIN')
    BEGIN
        SELECT E.EmployeeId,E.EmployeeCode,E.FirstName,E.LastName,E.LoginId,E.OfficialEmail,E.Mobile,E.PasswordHash,E.IsActive,E.IsLocked,E.FailedLoginAttempts,
            (SELECT STRING_AGG(CAST(R.RoleId AS VARCHAR),',') FROM ORG_EmployeeRole ER INNER JOIN ORG_Role R ON R.RoleId=ER.Ref_RoleId WHERE ER.Ref_EmployeeId=E.EmployeeId AND ER.IsActive=1) AS RoleIds,
            (SELECT STRING_AGG(R.RoleName,',') FROM ORG_EmployeeRole ER INNER JOIN ORG_Role R ON R.RoleId=ER.Ref_RoleId WHERE ER.Ref_EmployeeId=E.EmployeeId AND ER.IsActive=1) AS RoleNames,
            (SELECT TOP 1 D.DepartmentName FROM ORG_EmployeeDepartment ED INNER JOIN ORG_Department D ON D.DepartmentId=ED.Ref_DepartmentId WHERE ED.Ref_EmployeeId=E.EmployeeId AND ED.IsPrimaryDepartment=1) AS DepartmentName,
            (SELECT TOP 1 O.OfficeName FROM ORG_EmployeeOffice EO INNER JOIN ORG_Office O ON O.OfficeId=EO.Ref_OfficeId WHERE EO.Ref_EmployeeId=E.EmployeeId AND EO.IsPrimaryOffice=1) AS OfficeName
        FROM ORG_Employee E
        WHERE E.LoginId=@LoginId
           OR E.Mobile=@LoginId
           OR E.OfficialEmail=@LoginId;

        ;WITH EmployeeRoles AS
            ( SELECT ER.Ref_RoleId FROM ORG_EmployeeRole ER
                INNER JOIN ORG_Employee E ON E.EmployeeId=ER.Ref_EmployeeId
                WHERE (E.LoginId=@LoginId OR E.Mobile=@LoginId OR E.OfficialEmail=@LoginId) AND ER.IsActive=1
            )
            SELECT 
                M.ModuleId,M.ModuleCode,M.ModuleName,M.IconName,
                CAST(CASE
                    WHEN NOT EXISTS(SELECT 1 FROM ORG_Permission P WHERE P.Ref_ModuleId=M.ModuleId AND P.IsActive=1)
                    AND EXISTS(SELECT 1 FROM EmployeeRoles ER INNER JOIN ORG_RoleModuleAccess RMA ON RMA.Ref_RoleId=ER.Ref_RoleId AND RMA.Ref_ModuleId=M.ModuleId AND RMA.IsEnabled=1)
                    THEN 1
                    WHEN EXISTS
                    (
                        SELECT 1
                        FROM ORG_Permission P2
                        INNER JOIN ORG_RolePermission RP2 ON RP2.Ref_PermissionId=P2.PermissionId AND RP2.IsAllowed=1
                        INNER JOIN EmployeeRoles ER2 ON ER2.Ref_RoleId=RP2.Ref_RoleId
                        WHERE P2.Ref_ModuleId=M.ModuleId
                        AND P2.IsActive=1
                        AND EXISTS
                        (
                            SELECT 1
                            FROM ORG_RoleModuleAccess RMA2
                            WHERE RMA2.Ref_RoleId=ER2.Ref_RoleId
                            AND RMA2.Ref_ModuleId=M.ModuleId
                            AND RMA2.IsEnabled=1
                        )
                    )
                    THEN 1
                    ELSE 0
                END AS BIT) AS IsAllowed,
                (
                    SELECT
                        P.PermissionId,P.PermissionCode,P.PermissionName,
                        CAST(CASE
                            WHEN EXISTS
                            (
                                SELECT 1
                                FROM ORG_RolePermission RP
                                INNER JOIN EmployeeRoles ER ON ER.Ref_RoleId=RP.Ref_RoleId
                                WHERE RP.Ref_PermissionId=P.PermissionId
                                AND RP.IsAllowed=1
                                AND EXISTS
                                (
                                    SELECT 1
                                    FROM ORG_RoleModuleAccess RMA
                                    WHERE RMA.Ref_RoleId=ER.Ref_RoleId
                                    AND RMA.Ref_ModuleId=M.ModuleId
                                    AND RMA.IsEnabled=1
                                )
                            )
                            THEN 1 ELSE 0
                        END AS BIT) AS IsAllowed
                    FROM ORG_Permission P
                    WHERE P.Ref_ModuleId=M.ModuleId AND P.IsActive=1
                    ORDER BY P.PermissionName
                    FOR JSON PATH
                ) AS PermissionsJson
            FROM ORG_Module M
            WHERE M.IsActive=1
            ORDER BY ISNULL(M.DisplayOrder,999),M.ModuleName;
        
        ---- NEW: 3rd result set � SuperModule tab visibility (CRM/HRMS/ADMIN)   
        ;WITH EmployeeRoles AS
            (
                SELECT ER.Ref_RoleId
                FROM ORG_EmployeeRole ER
                INNER JOIN ORG_Employee E ON E.EmployeeId=ER.Ref_EmployeeId
                WHERE (E.LoginId=@LoginId OR E.Mobile=@LoginId OR E.OfficialEmail=@LoginId) AND ER.IsActive=1
            )
            SELECT SM.SuperModuleId,SM.SuperModuleCode,SM.SuperModuleName,SM.IconName,
                CAST(CASE
                    WHEN EXISTS
                    (
                        SELECT 1
                        FROM ORG_RoleSuperModuleAccess RSA
                        INNER JOIN EmployeeRoles ER ON ER.Ref_RoleId = RSA.Ref_RoleId
                        WHERE RSA.Ref_SuperModuleId = SM.SuperModuleId
                          AND RSA.IsEnabled = 1
                    )
                    THEN 1
                    ELSE 0
                END AS BIT) AS IsAllowed
            FROM ORG_SuperModule SM
            WHERE SM.IsActive = 1
            ORDER BY ISNULL(SM.DisplayOrder, 999), SM.SuperModuleName;

        SELECT SMM.SuperModuleId,SMM.ModuleId
            FROM ORG_SuperModuleMapping SMM
            INNER JOIN ORG_Module M ON M.ModuleId = SMM.ModuleId AND M.IsActive = 1;
    RETURN;
    END

ELSE IF (@Status = 'REGISTER_EMPLOYEE_DEVICE')
BEGIN

    IF @EmployeeId IS NULL THROW 50040, 'EmployeeId is required.', 1;
    IF NULLIF(LTRIM(RTRIM(@DeviceId)), '') IS NULL THROW 50041, 'DeviceId is required.', 1;
    IF NULLIF(LTRIM(RTRIM(@DeviceToken)), '') IS NULL THROW 50042, 'DeviceToken is required.', 1;
  
    -- Same employee + same device  

    IF EXISTS
    (
        SELECT 1
        FROM dbo.ORG_EmployeeDevice
        WHERE Ref_EmployeeId = @EmployeeId
          AND DeviceId = @DeviceId
    )
    BEGIN

        UPDATE dbo.ORG_EmployeeDevice
        SET
            DeviceToken     = @DeviceToken,
            DeviceType      = @DeviceType,
            AppVersion      = @AppVersion,
            IsActive        = 1,
            IsLoggedIn      = 1,
            LastSeenOn      = GETDATE(),
            UpdatedOn       = GETDATE(),
            UpdatedBy       = @EmployeeId
        WHERE Ref_EmployeeId = @EmployeeId
          AND DeviceId = @DeviceId;

        SELECT CAST(1 AS BIT) AS Status,'Device registered successfully.' AS Message;
        RETURN;
    END;
   
    -- STEP 2
    -- DeviceId belongs to another employee   
    IF EXISTS
    (
        SELECT 1
        FROM dbo.ORG_EmployeeDevice
        WHERE DeviceId = @DeviceId
          AND Ref_EmployeeId <> @EmployeeId
    )
    BEGIN

        IF EXISTS
        (
            SELECT 1
            FROM dbo.ORG_EmployeeDevice
            WHERE DeviceId = @DeviceId
              AND Ref_EmployeeId <> @EmployeeId
              AND IsActive = 1
              AND IsLoggedIn = 1
        )
        BEGIN
            SELECT CAST(0 AS BIT) AS Status,'This device is currently logged in with another user.' AS Message;
            RETURN;
        END;

        UPDATE dbo.ORG_EmployeeDevice
        SET
            Ref_EmployeeId  = @EmployeeId,
            DeviceToken     = @DeviceToken,
            DeviceType      = @DeviceType,
            AppVersion      = @AppVersion,
            IsActive        = 1,
            IsLoggedIn      = 1,
            LastSeenOn      = GETDATE(),
            UpdatedOn       = GETDATE(),
            UpdatedBy       = @EmployeeId
        WHERE DeviceId      = @DeviceId
        AND Ref_EmployeeId <> @EmployeeId;
        SELECT CAST(1 AS BIT) AS Status,'Device transferred successfully.' AS Message;
        RETURN;
    END;
  
    -- STEP 3
    -- FCM token belongs to another device 

    IF EXISTS
    (
        SELECT 1
        FROM dbo.ORG_EmployeeDevice
        WHERE DeviceToken = @DeviceToken
          AND DeviceId <> @DeviceId
    )
    BEGIN

        UPDATE dbo.ORG_EmployeeDevice
        SET
            DeviceToken     = @DeviceToken,
            DeviceType      = @DeviceType,
            AppVersion      = @AppVersion,
            IsActive        = 1,
            IsLoggedIn      = 1,
            LastSeenOn      = GETDATE(),
            UpdatedOn       = GETDATE(),
            UpdatedBy       = @EmployeeId
        WHERE DeviceToken   = @DeviceToken
          AND DeviceId <> @DeviceId;

        SELECT
            CAST(1 AS BIT) AS Status,
            'Device token updated successfully.' AS Message;

        RETURN;
    END;
   
    -- STEP 4
    -- New device 

    INSERT INTO dbo.ORG_EmployeeDevice (Ref_EmployeeId,DeviceId,DeviceToken,DeviceType,AppVersion,IsActive,IsLoggedIn,LastSeenOn,CreatedOn,CreatedBy)
    VALUES (@EmployeeId,@DeviceId,@DeviceToken,@DeviceType,@AppVersion,1,1,GETDATE(),GETDATE(),@EmployeeId);

    SELECT CAST(1 AS BIT) AS Status,'Device registered successfully.' AS Message;
    RETURN;

END

ELSE IF (@Status = 'CHECK_ACTIVE_DEVICE')
BEGIN

    IF @EmployeeId IS NULL THROW 50050, 'EmployeeId is required.', 1;
    IF NULLIF(LTRIM(RTRIM(@DeviceId)), '') IS NULL THROW 50051, 'DeviceId is required.', 1;
    -- CHECK CURRENTLY LOGGED-IN DEVICE
    SELECT TOP 1 EmployeeDeviceId,Ref_EmployeeId,DeviceId AS ActiveDeviceId,DeviceToken AS ActiveDeviceToken,DeviceType,AppVersion,IsActive,IsLoggedIn,LastSeenOn,
        -- IS THIS THE SAME DEVICE?
        CAST
        (
            CASE
                WHEN DeviceId = @DeviceId
                     AND IsActive = 1
                     AND IsLoggedIn = 1
                THEN 1
                ELSE 0
            END
            AS BIT
        ) AS IsSameDevice
    FROM dbo.ORG_EmployeeDevice

    -- ONLY CURRENTLY LOGGED-IN DEVICE
    WHERE Ref_EmployeeId = @EmployeeId
      AND IsActive = 1
      AND IsLoggedIn = 1
    ORDER BY LastSeenOn DESC,EmployeeDeviceId DESC;
    RETURN;

END

ELSE IF (@Status = 'FORCE_LOGOUT_DEVICE')
BEGIN

    IF @EmployeeId IS NULL THROW 50052, 'EmployeeId is required.', 1;
    IF NULLIF(LTRIM(RTRIM(@DeviceId)), '') IS NULL THROW 50053, 'DeviceId is required.', 1;

    -- STEP 1
    -- Get OLD DEVICE information
    DECLARE @OldDeviceToken VARCHAR(1000);


    SELECT TOP 1 @OldDeviceToken = DeviceToken
    FROM dbo.ORG_EmployeeDevice
    WHERE Ref_EmployeeId = @EmployeeId
      AND DeviceId = @DeviceId
      AND IsActive = 1
      AND IsLoggedIn = 1;

    -- STEP 2
    -- Check old logged-in device exists

    IF @OldDeviceToken IS NULL
    BEGIN
        SELECT
            CAST(0 AS BIT) AS Status, 'Active logged-in old device not found.' AS Message,NULL AS DeviceToken;
        RETURN;

    END;
    -- STEP 3
    -- FORCE LOGOUT OLD DEVICE
    --
    -- IsActive   = 0
    -- IsLoggedIn = 0
    --
    -- DeviceToken is intentionally kept.

    UPDATE dbo.ORG_EmployeeDevice
    SET
        IsActive = 0,
        IsLoggedIn = 0,
        LastSeenOn = GETDATE(),
        UpdatedOn = GETDATE(),
        UpdatedBy = @EmployeeId
    WHERE Ref_EmployeeId = @EmployeeId
      AND DeviceId = @DeviceId
      AND IsActive = 1
      AND IsLoggedIn = 1;

    -- STEP 4
    -- Return OLD FCM token to .NET

    IF @@ROWCOUNT > 0
    BEGIN

        SELECT
            CAST(1 AS BIT) AS Status,'Previous device logged out successfully.' AS Message,@OldDeviceToken AS DeviceToken;
        RETURN;
    END;

    -- STEP 5
    -- Fallback

    SELECT
        CAST(0 AS BIT) AS Status,'Unable to logout old device.' AS Message,NULL AS DeviceToken;
    RETURN;

END

ELSE IF (@Status = 'LOGOUT_DEVICE')
BEGIN

    IF @EmployeeId IS NULL THROW 50054, 'EmployeeId is required.', 1;
    IF NULLIF(LTRIM(RTRIM(@DeviceId)), '') IS NULL THROW 50055, 'DeviceId is required.', 1;
   
    -- NORMAL LOGOUT
    UPDATE dbo.ORG_EmployeeDevice
    SET
        IsLoggedIn       = 0,
        IsActive         = 1,
        LastSeenOn       = GETDATE(),
        UpdatedOn        = GETDATE(),
        UpdatedBy        = @EmployeeId
    WHERE Ref_EmployeeId = @EmployeeId
      AND DeviceId       = @DeviceId
      AND IsActive       = 1
      AND IsLoggedIn     = 1;

    -- LOGOUT SUCCESS

    IF @@ROWCOUNT > 0
    BEGIN

        SELECT
            CAST(1 AS BIT) AS Status,
            'Logout successful.' AS Message;

        RETURN;

    END;
    -- NO LOGGED-IN DEVICE FOUND
    SELECT
        CAST(0 AS BIT) AS Status,
        'Logged-in device not found.' AS Message;

    RETURN;

END
ELSE IF(@Status='GENERATE_RESET_OTP')
    BEGIN
        IF @EmployeeId IS NULL
    BEGIN
    SELECT CAST(0 AS BIT) AS Status,'EmployeeId is required.' AS Message;
    RETURN;
    END;
        IF NULLIF(LTRIM(RTRIM(@OTPHash)),'') IS NULL
            BEGIN
            SELECT CAST(0 AS BIT) AS Status,'OTP hash is required.' AS Message;
            RETURN;
            END;
        IF NOT EXISTS(SELECT 1 FROM dbo.ORG_Employee WHERE EmployeeId=@EmployeeId AND IsActive=1 AND ISNULL(IsLocked,0)=0)
            BEGIN
            SELECT CAST(0 AS BIT) AS Status,'Password reset is not available for this account.' AS Message;
            RETURN;
            END;
        UPDATE dbo.ORG_PasswordResetOTP
        SET IsUsed=1
        WHERE Ref_EmployeeId=@EmployeeId AND IsUsed=0;
            INSERT INTO dbo.ORG_PasswordResetOTP(Ref_EmployeeId,OTPHash,ExpiresOn,IsUsed,IsVerified,AttemptCount,ResetToken,ResetTokenExpiresOn,CreatedOn,VerifiedOn)
            VALUES(@EmployeeId,@OTPHash,DATEADD(MINUTE,10,GETDATE()),0,0,0,NULL,NULL,GETDATE(),NULL);
        SELECT CAST(1 AS BIT) AS Status,'OTP generated successfully.' AS Message;
        RETURN;
    END

ELSE IF(@Status='GET_RESET_OTP')
    BEGIN
        IF @EmployeeId IS NULL
        BEGIN
        SELECT CAST(0 AS BIT) AS Status,'EmployeeId is required.' AS Message;
        RETURN;
        END;
        SELECT TOP 1
            CAST(1 AS BIT) AS Status,
            'OTP record found.' AS Message,
            PasswordResetOTPId,Ref_EmployeeId,OTPHash,ExpiresOn,IsUsed,IsVerified,AttemptCount,ResetToken,ResetTokenExpiresOn,CreatedOn,VerifiedOn
        FROM dbo.ORG_PasswordResetOTP
        WHERE Ref_EmployeeId=@EmployeeId AND IsUsed=0
        ORDER BY PasswordResetOTPId DESC;
     RETURN;
    END

ELSE IF(@Status='INCREMENT_OTP_ATTEMPT')
    BEGIN
        IF @EmployeeId IS NULL
        BEGIN
        SELECT CAST(0 AS BIT) AS Status,'EmployeeId is required.' AS Message;
        RETURN;
        END;
        UPDATE TOP(1) dbo.ORG_PasswordResetOTP
        SET AttemptCount=AttemptCount+1,
            IsUsed=CASE WHEN AttemptCount+1>=5 THEN 1 ELSE IsUsed END
        WHERE Ref_EmployeeId=@EmployeeId AND IsUsed=0 AND ExpiresOn>GETDATE();
        SELECT CAST(1 AS BIT) AS Status,'OTP attempt recorded.' AS Message;
      RETURN;
    END

ELSE IF(@Status='VERIFY_RESET_OTP')
    BEGIN
        IF @EmployeeId IS NULL
            BEGIN
            SELECT CAST(0 AS BIT) AS Status,'EmployeeId is required.' AS Message;
            RETURN;
        END;
        DECLARE @PasswordResetOTPId INT,@AttemptCount INT,@IsUsed BIT,@IsVerified BIT,@ExpiresOn DATETIME;
        SELECT TOP 1
            @PasswordResetOTPId=PasswordResetOTPId,
            @AttemptCount=AttemptCount,
            @IsUsed=IsUsed,
            @IsVerified=IsVerified,
            @ExpiresOn=ExpiresOn
        FROM dbo.ORG_PasswordResetOTP
        WHERE Ref_EmployeeId=@EmployeeId
        ORDER BY PasswordResetOTPId DESC;

        IF @PasswordResetOTPId IS NULL
            BEGIN
                SELECT CAST(0 AS BIT) AS Status,'OTP request not found.' AS Message;
                RETURN;
            END;
        IF @IsUsed=1
            BEGIN
                SELECT CAST(0 AS BIT) AS Status,'OTP has expired or has already been used.' AS Message;
                RETURN;
            END;
        IF @IsVerified=1
            BEGIN
                SELECT CAST(0 AS BIT) AS Status,'OTP has already been verified.' AS Message;
                RETURN;
            END;
        IF @ExpiresOn<=GETDATE()
            BEGIN
                UPDATE dbo.ORG_PasswordResetOTP SET IsUsed=1 WHERE PasswordResetOTPId=@PasswordResetOTPId;
                SELECT CAST(0 AS BIT) AS Status,'OTP has expired. Please request a new OTP.' AS Message;
                RETURN;
            END;
        IF ISNULL(@AttemptCount,0)>=5
            BEGIN
                UPDATE dbo.ORG_PasswordResetOTP SET IsUsed=1 WHERE PasswordResetOTPId=@PasswordResetOTPId;
                SELECT CAST(0 AS BIT) AS Status,'Maximum OTP attempts exceeded. Please request a new OTP.' AS Message;
                RETURN;
            END;
        DECLARE @NewResetToken UNIQUEIDENTIFIER=NEWID();
        UPDATE dbo.ORG_PasswordResetOTP
        SET IsVerified=1,VerifiedOn=GETDATE(),ResetToken=@NewResetToken,ResetTokenExpiresOn=DATEADD(MINUTE,10,GETDATE())
        WHERE PasswordResetOTPId=@PasswordResetOTPId;
        SELECT CAST(1 AS BIT) AS Status,'OTP verified successfully.' AS Message,@NewResetToken AS ResetToken;
      RETURN;
    END

ELSE IF(@Status='RESET_PASSWORD')
    BEGIN
    IF @EmployeeId IS NULL
        BEGIN
            SELECT CAST(0 AS BIT) AS Status,'EmployeeId is required.' AS Message;
            RETURN;
        END;

    IF @ResetToken IS NULL
        BEGIN
            SELECT CAST(0 AS BIT) AS Status,'Reset token is required.' AS Message;
            RETURN;
        END;

    IF NULLIF(LTRIM(RTRIM(@NewPassword)),'') IS NULL
        BEGIN
            SELECT CAST(0 AS BIT) AS Status,'New password is required.' AS Message;
            RETURN;
        END;

    DECLARE @ResetOTPId INT,@ResetTokenExpiresOn DATETIME,@Verified BIT,@Used BIT;
    SELECT TOP 1
        @ResetOTPId             =PasswordResetOTPId,
        @ResetTokenExpiresOn    =ResetTokenExpiresOn,
        @Verified               =IsVerified,
        @Used                   =IsUsed
    FROM dbo.ORG_PasswordResetOTP
    WHERE Ref_EmployeeId=@EmployeeId AND ResetToken=@ResetToken
    ORDER BY PasswordResetOTPId DESC;

    IF @ResetOTPId IS NULL
        BEGIN
            SELECT CAST(0 AS BIT) AS Status,'Invalid reset token.' AS Message;
            RETURN;
        END;

    IF ISNULL(@Verified,0)=0
        BEGIN
            SELECT CAST(0 AS BIT) AS Status,'OTP verification is required before resetting the password.' AS Message;
            RETURN;
        END;

    IF ISNULL(@Used,0)=1
        BEGIN
            SELECT CAST(0 AS BIT) AS Status,'This password reset request has already been used.' AS Message;
            RETURN;
        END;

    IF @ResetTokenExpiresOn IS NULL OR @ResetTokenExpiresOn<=GETDATE()
        BEGIN
            UPDATE dbo.ORG_PasswordResetOTP SET IsUsed=1 WHERE PasswordResetOTPId=@ResetOTPId;
            SELECT CAST(0 AS BIT) AS Status,'Reset session has expired. Please request a new OTP.' AS Message;
            RETURN;
        END;

    IF NOT EXISTS(SELECT 1 FROM dbo.ORG_Employee WHERE EmployeeId=@EmployeeId AND IsActive=1)
        BEGIN
            SELECT CAST(0 AS BIT) AS Status,'Employee account is inactive.' AS Message;
            RETURN;
        END;

    UPDATE dbo.ORG_Employee
    SET PasswordHash    =@NewPassword,FailedLoginAttempts=0,IsLocked=0
    WHERE EmployeeId    =@EmployeeId;
    UPDATE dbo.ORG_PasswordResetOTP
    SET IsUsed=1
    WHERE PasswordResetOTPId=@ResetOTPId;
    SELECT CAST(1 AS BIT) AS Status,'Password reset successfully.' AS Message;
    RETURN;
    END

ELSE IF(@Status='GET_RESET_EMPLOYEE')
    BEGIN
        IF @ResetToken IS NULL
        BEGIN
            SELECT
                CAST(0 AS BIT) AS Status,
                'Reset token is required.' AS Message;
            RETURN;
        END;

        SELECT TOP 1
            CAST(1 AS BIT) AS Status,
            'Reset token is valid.' AS Message,
            Ref_EmployeeId AS EmployeeId
        FROM dbo.ORG_PasswordResetOTP
        WHERE ResetToken=@ResetToken
          AND IsVerified=1
          AND IsUsed=0
          AND ResetTokenExpiresOn>GETDATE()
        ORDER BY PasswordResetOTPId DESC;

        RETURN;
    END

  ELSE
    BEGIN
    SELECT CAST(0 AS BIT) AS Status,'Invalid authentication status.' AS Message;
    RETURN;
  END
END
GO

DROP FUNCTION IF EXISTS dbo.FN_ORG_EmployeeEffectivePermission;
GO

DROP TABLE IF EXISTS dbo.ORG_EmployeePermissionOverride;
GO
