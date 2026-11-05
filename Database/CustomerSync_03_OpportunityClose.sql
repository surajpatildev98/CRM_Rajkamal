/* CRM <-> WMS customer sync  —  Step 3: USP_CRM_OpportunityManagement (CLOSE_OPPORTUNITY)
   Full procedure (UAT/Live logic identical). Changes vs current: in CLOSE_OPPORTUNITY won-customer
   conversion (1) reuse a master customer with the same name, (2) EXEC PUSH_TO_WMS before linking. */
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE OR ALTER PROCEDURE [dbo].[USP_CRM_OpportunityManagement]

    @Status NVARCHAR(80),   
   
    -- Common Parameters   
    @Ref_LeadId                     INT = NULL,
    @Ref_OpportunityId              INT = NULL,
    @SearchText                     NVARCHAR(255) = NULL,
    @CreatedBy                      INT = NULL,
    -- Opportunity Form Parameters (for CONVERT)   
    @OpportunityName                NVARCHAR(255) = NULL,
    @ExpectedRevenue                DECIMAL(18,2) = NULL,
    @ExpectedCloseDate              DATETIME = NULL,
    @Probability                    INT = NULL,
    @Ref_PriorityId                 INT = NULL,
    @OpportunityRemark              NVARCHAR(500) = NULL,      
    -- Opportunity Master Dropdowns   
    @Ref_OpportunityTypeId          INT = NULL,
    @Ref_OpportunityStageId         INT = NULL,
    --- Transfer Opportunity
    @AssignedToEmployeeId           INT = NULL,
    @AssignmentRemark               NVARCHAR(500) = NULL,
    @FilterAssignedToId             INT = NULL,
     @LoggedInEmployeeId            INT = NULL,   
    -- Opportunity List Filters   
    @FilterStageId                  INT = NULL,
    @FilterOwnerId                  INT = NULL,
    @FilterTypeId                   INT = NULL,
    @Ref_ActivityTypeId             INT = NULL,
    @Subject                        NVARCHAR(255) = NULL,
    @Description                    NVARCHAR(MAX) = NULL,
    @Outcome                        NVARCHAR(MAX) = NULL,
    @ActivityDateTime               DATETIME = NULL,
    @DurationInMinutes              INT = NULL,
    @Latitude                       DECIMAL(9,6) = NULL,
    @Longitude                      DECIMAL(9,6) = NULL,
     @LocationAddress               NVARCHAR(500) = NULL,
    @Ref_ActivityStatusId           INT = NULL,
    @Ref_NewOpportunityStageId      INT = NULL,
    @NextFollowUpDate               DATETIME = NULL,
    @AttachmentsMeta                NVARCHAR(MAX) = NULL,
    @FollowUpAssignedEmployeeId     INT = NULL,
    @FollowUpRemarks                NVARCHAR(500) = NULL,
    @FollowUpPriority               TINYINT = NULL,
    @Ref_ProductId                  BIGINT = NULL,
    @Quantity                       DECIMAL(18,2) = NULL,
    @RequirementRemark              NVARCHAR(500) = NULL,
    @Ref_UnitId                     INT = NULL,
    @Ref_OpportunityRequirementId   INT = NULL,
    @Ref_LeadRequirementId          INT =NULL,
    @Ref_OpportunityStageIdClose    INT = NULL,
    @ClosureRemark                  NVARCHAR(500) = NULL,
    @FollowUpId                     INT = NULL, 
    @NewScheduledOn                 DATETIME = NULL,
    @DeleteRemark                   NVARCHAR(500)=NULL,
    @StatusRemark                   NVARCHAR(500) = NULL,
     @Ref_ProductCategoryId         INT=NULL,
    @Ref_OfferingId                 INT=NULL,
    -- Activity Expense
    @Ref_ActivityId                 INT = NULL,
    @ExpenseItems                   NVARCHAR(MAX) = NULL,
    @ExpenseId                      INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

   
IF(@Status = 'GET_WON_LEADS_LIST_FULL')
    BEGIN
        -- Filtered lead IDs first - only WON leads not converted
        DECLARE @MatchingLeads TABLE (LeadId INT);

        INSERT INTO @MatchingLeads
        SELECT L.LeadId
        FROM CRM_Lead L
        INNER JOIN CRM_LeadOrganization LO ON LO.LeadOrganizationId = L.Ref_LeadOrganizationId
        WHERE L.Ref_LeadStatusId = 4  -- (Qualified) status
        AND L.LeadId NOT IN (SELECT ISNULL(Ref_LeadId, 0) FROM CRM_Opportunity WHERE Ref_LeadId IS NOT NULL)
       -- AND L.IsActive = 1
       AND (
            @FilterAssignedToId IS NULL
            OR L.Ref_CurrentAssignedEmployeeId = @FilterAssignedToId
           )

        AND
            (   -- Lead creator can always see their own lead
                    L.CreatedBy=@LoggedInEmployeeId

                    OR            
                EXISTS
                (
                    SELECT 1
                    FROM ORG_EmployeeRole ER
                    INNER JOIN ORG_Role R
                        ON R.RoleId = ER.Ref_RoleId
                    WHERE ER.Ref_EmployeeId = @LoggedInEmployeeId
                        AND ER.IsPrimaryRole = 1
                        AND ER.IsActive = 1
                        AND R.RoleCode = 'ADMIN'
                )

                OR
                (
                    EXISTS
                    (
                        SELECT 1
                        FROM ORG_EmployeeRole ER
                        INNER JOIN ORG_Role R
                            ON R.RoleId = ER.Ref_RoleId
                        WHERE ER.Ref_EmployeeId = @LoggedInEmployeeId
                            AND ER.IsPrimaryRole = 1
                            AND ER.IsActive = 1
                            AND R.RoleCode = 'SALES MANAGER'
                    )

                    AND
                    (
                        -- Current assigned employee belongs to this manager
                        EXISTS
                        (
                            SELECT 1
                            FROM ORG_Employee E
                            WHERE E.EmployeeId = L.Ref_CurrentAssignedEmployeeId
                                AND E.Ref_ReportingManagerId = @LoggedInEmployeeId
                                AND E.IsActive = 1
                        )

                        OR

                        -- Lead was created by this manager's team member
                        EXISTS
                        (
                            SELECT 1
                            FROM ORG_Employee E
                            WHERE E.EmployeeId = L.CreatedBy
                                AND E.Ref_ReportingManagerId = @LoggedInEmployeeId
                                AND E.IsActive = 1
                        )

                        OR

                        -- Lead created by manager
                        L.CreatedBy = @LoggedInEmployeeId

                        OR

                        -- Lead currently assigned to manager
                        L.Ref_CurrentAssignedEmployeeId = @LoggedInEmployeeId
                    )
                )

                OR
                (
                    EXISTS
                    (
                        SELECT 1
                        FROM ORG_EmployeeRole ER
                        INNER JOIN ORG_Role R
                            ON R.RoleId = ER.Ref_RoleId
                        WHERE ER.Ref_EmployeeId = @LoggedInEmployeeId
                            AND ER.IsPrimaryRole = 1
                            AND ER.IsActive = 1
                            AND R.RoleCode = 'SALES'
                    )

                    AND
                    (
                        L.CreatedBy = @LoggedInEmployeeId
                        OR L.Ref_CurrentAssignedEmployeeId = @LoggedInEmployeeId
                    )
                )
            )
        AND (@SearchText IS NULL OR 
             LO.CustomerName LIKE '%'+@SearchText+'%' OR 
             L.LeadNo LIKE '%'+@SearchText+'%' OR
             L.LeadTitle LIKE '%'+@SearchText+'%');

        -- Result Set 0: Lead + Status + Company + Lead Attachments (combined)
        SELECT
            L.LeadId,L.LeadNo,LS.LeadStatusId AS StatusId,LS.StatusName,L.ExpectedValue,L.LeadRemark,L.LeadRequirementRemark,L.CreatedOn,
            LO.LeadOrganizationId,LO.CustomerName AS CompanyName,LO.Address,C.CountryName,ST.StateName,CT.CityName,LO.PinCode,
            LO.Gst AS GstNo,LO.Pan AS PanNo,LSrc.SourceName AS LeadSource,E.EmployeeId AS AssignedToEmployeeId,
            E.FirstName + ' ' + ISNULL(E.LastName,'') AS AssignedToEmployee,
            -- Get all lead attachments as JSON
            (
                SELECT 
                    AttachmentId,FileName,FilePath,FileType,FileSize,CreatedOn
                FROM CRM_LeadAttachment LA
                WHERE LA.Ref_LeadId = L.LeadId
                FOR JSON PATH
            ) AS LeadAttachmentsJson
        FROM CRM_Lead L
        INNER JOIN @MatchingLeads ML ON ML.LeadId = L.LeadId
        INNER JOIN CRM_LeadOrganization LO ON LO.LeadOrganizationId = L.Ref_LeadOrganizationId
        LEFT JOIN CRM_LeadStatus LS ON LS.LeadStatusId = L.Ref_LeadStatusId
        LEFT JOIN MST_Country C ON C.CountryId = LO.Ref_CountryId
        LEFT JOIN MST_State ST ON ST.StateId = LO.Ref_StateId
        LEFT JOIN MST_City CT ON CT.CityId = LO.Ref_CityId
        LEFT JOIN CRM_LeadSource LSrc ON LSrc.LeadSourceId = L.Ref_LeadSourceId
        LEFT JOIN ORG_Employee E ON E.EmployeeId = L.Ref_CurrentAssignedEmployeeId
        ORDER BY L.CreatedOn DESC;

        -- Result Set 1: Contacts (multiple rows per lead, includes LeadId for grouping)
        SELECT
            L.LeadId,LC.LeadContactId,LC.ContactPerson,D.DesignationName,LC.Mobile AS MobileNo,LC.Email
        FROM CRM_LeadContact LC
        INNER JOIN CRM_Lead L ON L.Ref_LeadOrganizationId = LC.Ref_LeadOrganizationId
        INNER JOIN @MatchingLeads ML ON ML.LeadId = L.LeadId
        LEFT JOIN ORG_Designation D ON D.DesignationId = LC.Ref_DesignationId
       -- WHERE LC.IsActive = 1;

        
        -- Result Set 2: Requirements
        SELECT LR.Ref_LeadId AS LeadId,LR.LeadRequirementId,PC.ProductCategoryId AS CategoryId,PC.CategoryName,O.ProductId AS OfferingId,O.ProductName AS OfferingName,P.ProductId,
            P.ItemName AS ProductName,LR.RequirementRemark AS Remark,LR.Quantity,U.Symbol AS Unit
        FROM CRM_LeadRequirement LR
        INNER JOIN @MatchingLeads ML ON ML.LeadId=LR.Ref_LeadId
        LEFT JOIN MST_ProductCategory PC ON PC.ProductCategoryId=LR.Ref_ProductCategoryId
        LEFT JOIN MST_Offering O ON O.ProductId=LR.Ref_OfferingId
        LEFT JOIN MST_Product P ON P.ProductId=LR.Ref_ProductId
        LEFT JOIN MST_Unit U ON U.UnitId=LR.Ref_UnitId
        WHERE LR.IsActive=1
        AND LR.IsDeleted=0;

        -- Result Set 3: Activities (ONLY — no Follow-ups here)
        SELECT
            A.Ref_LeadId AS LeadId,A.ActivityId,AT.ActivityName AS ActivityType,A.ActivityDateTime AS ActivityDate,A.Subject,A.Description,A.Outcome,
            E.FirstName + ' ' + ISNULL(E.LastName,'') AS CreatedBy,A.Ref_ActivityStatusId,AST.ActivityStatusName AS ActivityStatus,AST.ColorCode AS ActivityStatusColor
        FROM CRM_LeadActivity A
        INNER JOIN @MatchingLeads ML ON ML.LeadId = A.Ref_LeadId
        LEFT JOIN CRM_LeadActivityType AT ON AT.ActivityTypeId = A.Ref_ActivityTypeId
        LEFT JOIN ORG_Employee E ON E.EmployeeId = A.Ref_EmployeeId
        LEFT JOIN CRM_LeadActivityStatus AST ON AST.ActivityStatusId = A.Ref_ActivityStatusId
        WHERE A.IsActive = 1
        AND A.Ref_ActivityTypeId <> 8  -- exclude FollowUp type
        ORDER BY A.ActivityDateTime DESC;

        -- Result Set 4: Files (Activity Attachments)
        SELECT
            A.Ref_LeadId AS LeadId,LAA.AttachmentId,LAA.FileName,LAA.FilePath,LAA.FileType,LAA.FileSize,LAA.CreatedOn
        FROM CRM_LeadActivityAttachment LAA
        INNER JOIN CRM_LeadActivity A ON A.ActivityId = LAA.Ref_ActivityId
        INNER JOIN @MatchingLeads ML ON ML.LeadId = A.Ref_LeadId
        ORDER BY LAA.CreatedOn DESC;

        -- Result Set 5: Follow-ups
        SELECT
            LF.Ref_LeadId AS LeadId,LF.FollowUpId,LF.Ref_ActivityId AS ActivityId,LF.Ref_AssignedEmployeeId AS AssignedEmployeeId,FE.FirstName + ' ' + ISNULL(FE.LastName,'') AS AssignedTo,
            LF.ScheduledOn AS FollowUpDate,LF.Priority,LF.Remarks,LF.IsCompleted,LF.CompletedOn,AST.ActivityStatusName AS FollowUpStatus,
            AST.ColorCode AS FollowUpStatusColor,LF.CreatedOn,LF.CreatedBy,E.FirstName + ' ' + ISNULL(E.LastName,'') AS CreatedByName
        FROM CRM_LeadFollowUp LF
        INNER JOIN @MatchingLeads ML ON ML.LeadId = LF.Ref_LeadId
        INNER JOIN CRM_LeadActivity A ON A.ActivityId = LF.Ref_ActivityId
        LEFT JOIN CRM_LeadActivityStatus AST ON AST.ActivityStatusId = A.Ref_ActivityStatusId
        LEFT JOIN ORG_Employee FE ON FE.EmployeeId = LF.Ref_AssignedEmployeeId
        LEFT JOIN ORG_Employee E ON E.EmployeeId = LF.CreatedBy
       -- WHERE LF.IsActive = 1
        ORDER BY LF.ScheduledOn DESC;
    END
    
ELSE IF(@Status = 'GET_WON_LEAD_FULL_DETAILS')
    BEGIN
        -- Result Set 0: Lead + Status + Company + Lead Attachments (combined)
        SELECT
            L.LeadId,L.LeadNo,LS.LeadStatusId AS StatusId,LS.StatusName,L.ExpectedValue,L.LeadRemark,L.LeadRequirementRemark,L.CreatedOn,LO.LeadOrganizationId,
            LO.CustomerName AS CompanyName,LO.Address,C.CountryName,ST.StateName,CT.CityName,LO.PinCode,LO.Gst AS GstNo,LO.Pan AS PanNo,LSrc.SourceName AS LeadSource,
            E.EmployeeId AS AssignedToEmployeeId,E.FirstName + ' ' + ISNULL(E.LastName,'') AS AssignedToEmployee,
            -- Get all lead attachments as JSON
            (
                SELECT 
                    AttachmentId,FileName,FilePath,FileType,FileSize,CreatedOn
                FROM CRM_LeadAttachment LA
                WHERE LA.Ref_LeadId = L.LeadId
                FOR JSON PATH
            ) AS LeadAttachmentsJson
        FROM CRM_Lead L
        INNER JOIN CRM_LeadOrganization LO ON LO.LeadOrganizationId = L.Ref_LeadOrganizationId
        LEFT JOIN CRM_LeadStatus LS ON LS.LeadStatusId = L.Ref_LeadStatusId
        LEFT JOIN MST_Country C ON C.CountryId = LO.Ref_CountryId
        LEFT JOIN MST_State ST ON ST.StateId = LO.Ref_StateId
        LEFT JOIN MST_City CT ON CT.CityId = LO.Ref_CityId
        LEFT JOIN CRM_LeadSource LSrc ON LSrc.LeadSourceId = L.Ref_LeadSourceId
        LEFT JOIN ORG_Employee E ON E.EmployeeId = L.Ref_CurrentAssignedEmployeeId
        WHERE L.LeadId = @Ref_LeadId
         AND L.Ref_LeadStatusId = 4  -- Qualified status       

        -- Result Set 1: Contacts
        SELECT
            LC.LeadContactId,LC.ContactPerson,D.DesignationName,LC.Mobile AS MobileNo,LC.Email
        FROM CRM_LeadContact LC
        INNER JOIN CRM_Lead L ON L.Ref_LeadOrganizationId = LC.Ref_LeadOrganizationId
        LEFT JOIN ORG_Designation D ON D.DesignationId = LC.Ref_DesignationId
        WHERE L.LeadId = @Ref_LeadId 
       
        -- Result Set 2: Requirements
            SELECT
                LR.LeadRequirementId,
                PC.ProductCategoryId AS CategoryId,
                PC.CategoryName,
                O.ProductId AS OfferingId,
                O.ProductName AS OfferingName,
                P.ProductId,
                P.ItemName AS ProductName,
                LR.RequirementRemark AS Remark,
                LR.Quantity,
                U.Symbol AS Unit
            FROM CRM_LeadRequirement LR
            LEFT JOIN MST_ProductCategory PC ON PC.ProductCategoryId=LR.Ref_ProductCategoryId
            LEFT JOIN MST_Offering O ON O.ProductId=LR.Ref_OfferingId
            LEFT JOIN MST_Product P ON P.ProductId=LR.Ref_ProductId
            LEFT JOIN MST_Unit U ON U.UnitId=LR.Ref_UnitId
            WHERE LR.Ref_LeadId=@Ref_LeadId
            AND LR.IsActive=1
            AND LR.IsDeleted=0;

        -- Result Set 3: Activities (ONLY — no Follow-ups here)
        SELECT
            A.ActivityId,AT.ActivityName AS ActivityType,A.ActivityDateTime AS ActivityDate,A.Subject,A.Description,A.Outcome,
            E.FirstName + ' ' + ISNULL(E.LastName,'') AS CreatedBy,A.Ref_ActivityStatusId,AST.ActivityStatusName AS ActivityStatus,AST.ColorCode AS ActivityStatusColor
        FROM CRM_LeadActivity A
        LEFT JOIN CRM_LeadActivityType AT ON AT.ActivityTypeId = A.Ref_ActivityTypeId
        LEFT JOIN ORG_Employee E ON E.EmployeeId = A.Ref_EmployeeId
        LEFT JOIN CRM_LeadActivityStatus AST ON AST.ActivityStatusId = A.Ref_ActivityStatusId
        WHERE A.Ref_LeadId = @Ref_LeadId
        AND A.IsActive = 1
        AND A.Ref_ActivityTypeId <> 8  -- exclude FollowUp type
        ORDER BY A.ActivityDateTime DESC;

        -- Result Set 4: Files (Activity Attachments)
        SELECT
            LAA.AttachmentId,LAA.FileName,LAA.FilePath,LAA.FileType,LAA.FileSize,LAA.CreatedOn
        FROM CRM_LeadActivityAttachment LAA
        INNER JOIN CRM_LeadActivity A ON A.ActivityId = LAA.Ref_ActivityId
        WHERE A.Ref_LeadId = @Ref_LeadId
        ORDER BY LAA.CreatedOn DESC;

        -- Result Set 5: Follow-ups
        SELECT
            LF.FollowUpId,LF.Ref_ActivityId AS ActivityId,LF.Ref_AssignedEmployeeId AS AssignedEmployeeId,
            FE.FirstName + ' ' + ISNULL(FE.LastName,'') AS AssignedTo,LF.ScheduledOn AS FollowUpDate,LF.Priority,
            LF.Remarks,LF.IsCompleted,LF.CompletedOn,AST.ActivityStatusName AS FollowUpStatus,AST.ColorCode AS FollowUpStatusColor,
            LF.CreatedOn,LF.CreatedBy,E.FirstName + ' ' + ISNULL(E.LastName,'') AS CreatedByName
        FROM CRM_LeadFollowUp LF
        INNER JOIN CRM_LeadActivity A ON A.ActivityId = LF.Ref_ActivityId
        LEFT JOIN CRM_LeadActivityStatus AST ON AST.ActivityStatusId = A.Ref_ActivityStatusId
        LEFT JOIN ORG_Employee FE ON FE.EmployeeId = LF.Ref_AssignedEmployeeId
        LEFT JOIN ORG_Employee E ON E.EmployeeId = LF.CreatedBy
        WHERE LF.Ref_LeadId = @Ref_LeadId
        --AND LF.IsActive = 1
        ORDER BY LF.ScheduledOn DESC;
    END  
    
ELSE IF(@Status = 'CONVERT_LEAD_TO_OPPORTUNITY')
    BEGIN
        BEGIN TRY
            BEGIN TRANSACTION;           
            -- Step 1: Validate Lead exists, is WON, and not converted
           
            IF NOT EXISTS (SELECT 1 FROM CRM_Lead WHERE LeadId = @Ref_LeadId ) --AND IsActive = 1
            BEGIN
                RAISERROR('Lead not found.', 16, 1);
            END

            DECLARE @CurrentStatusId INT;
            SELECT @CurrentStatusId = Ref_LeadStatusId 
            FROM CRM_Lead 
            WHERE LeadId = @Ref_LeadId;

            IF @CurrentStatusId != 4  -- Not Qualified
            BEGIN
                RAISERROR('Lead must be in Qualified status to convert.', 16, 1);
            END

            IF EXISTS (SELECT 1 FROM CRM_Opportunity WHERE Ref_LeadId = @Ref_LeadId)
            BEGIN
                RAISERROR('This lead has already been converted to an opportunity.', 16, 1);
            END
            
            -- Step 2: Get Lead Organization Details
           
            DECLARE @LeadOrgId INT, 
                    @CustomerName NVARCHAR(255), 
                    @Address NVARCHAR(500), 
                    @CountryId INT, 
                    @StateId INT, 
                    @CityId INT, 
                    @PinCode NVARCHAR(20), 
                    @Gst NVARCHAR(50), 
                    @Pan NVARCHAR(50),
                    @AssignedEmployeeId INT,
                    @LeadTitle NVARCHAR(255),
                    @ExpectedValue DECIMAL(18,2);
            
            SELECT 
                @LeadOrgId = LO.LeadOrganizationId,
                @CustomerName = LO.CustomerName,
                @Address = LO.Address,
                @CountryId = LO.Ref_CountryId,
                @StateId = LO.Ref_StateId,
                @CityId = LO.Ref_CityId,
                @PinCode = LO.PinCode,
                @Gst = LO.Gst,
                @Pan = LO.Pan,
                --@AssignedEmployeeId = L.Ref_CurrentAssignedEmployeeId,  // CMT For assign Owner from here 
                @LeadTitle = ISNULL(L.LeadTitle, LO.CustomerName),
                @ExpectedValue = L.ExpectedValue
            FROM CRM_Lead L
            INNER JOIN CRM_LeadOrganization LO ON LO.LeadOrganizationId = L.Ref_LeadOrganizationId
            WHERE L.LeadId = @Ref_LeadId;

             SET @AssignedEmployeeId = @AssignedToEmployeeId;             

            DECLARE @OppNo NVARCHAR(50);
            DECLARE @DateStr NVARCHAR(6) = FORMAT(GETDATE(), 'ddMMyy');

            DECLARE @Seq INT = ISNULL(
            (
                SELECT MAX(CAST(RIGHT(OpportunityNo, 3) AS INT))
                FROM CRM_Opportunity
                WHERE OpportunityNo LIKE 'OP' + @DateStr + '%'
            ), 0) + 1;

            SET @OppNo = 'OP' + @DateStr + RIGHT('000' + CAST(@Seq AS VARCHAR(3)), 3);

           
            -- Step 6: Create Opportunity
           
            DECLARE @InitialStageId INT;
            SELECT @InitialStageId = OpportunityStageId 
            FROM CRM_OpportunityStage 
            WHERE StageCode = 'NEW' AND IsActive = 1;   -- changed from 'QUOTE' to 'NEW'

            IF(@InitialStageId IS NULL)
            BEGIN
                RAISERROR('Initial opportunity stage (NEW) is not configured or inactive.', 16, 1);
            END

            IF @AssignedEmployeeId IS NULL
                BEGIN
                    RAISERROR('Opportunity owner is required.',16,1);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM ORG_Employee
                    WHERE EmployeeId = @AssignedEmployeeId
                      AND IsActive = 1
                )
                BEGIN
                    RAISERROR('Selected opportunity owner is invalid or inactive.',16,1);
                END

            DECLARE @NewOpportunityId INT;            

            INSERT INTO CRM_Opportunity 
            (
                OpportunityNo,Ref_LeadId,Ref_CustomerId,OpportunityName,Ref_OpportunityTypeId,Ref_OpportunityStageId,
                ExpectedRevenue,ExpectedCloseDate,Probability,Ref_OwnerEmployeeId,Ref_PriorityId,CompetitorName,OpportunityRemark,
                IsActive,CreatedOn,CreatedBy
            )
            VALUES (
                @OppNo,
                @Ref_LeadId,
                  NULL,-- @NewCustomerId,
                @OpportunityName,  -- From form (pre-filled with LeadTitle)
                @Ref_OpportunityTypeId,  -- From form (default = 1? Need to confirm)
                --@Ref_OpportunityStageId,  -- From form (default = 1? Need to confirm)
                @InitialStageId,  -- Always starts at Qualification (StageOrder=1),
                @ExpectedRevenue,  -- From form (pre-filled with ExpectedValue)
                @ExpectedCloseDate,  -- From form (user selects)
                @Probability,  -- From form (user selects)
                @AssignedEmployeeId,  -- AUTO from lead (READ-ONLY)
                @Ref_PriorityId,  -- From form (user selects)
                NULL,  -- CompetitorName (not in form yet)
                @OpportunityRemark,  -- From form (optional)
                1,
                GETDATE(),
                @CreatedBy
            );

            SET @NewOpportunityId = SCOPE_IDENTITY();
 
            -- Step 7 : Copy Lead Requirements to Opportunity Requirements
            INSERT INTO CRM_OpportunityRequirement
                (Ref_OpportunityId,Ref_LeadRequirementId,Ref_ProductCategoryId,Ref_OfferingId,Ref_ProductId,Quantity,RequirementRemark,IsActive,Ref_UnitId,CreatedOn,CreatedBy)
            SELECT @NewOpportunityId,LR.LeadRequirementId,LR.Ref_ProductCategoryId,LR.Ref_OfferingId,LR.Ref_ProductId,LR.Quantity,LR.RequirementRemark,1,LR.Ref_UnitId,GETDATE(),@CreatedBy
                FROM CRM_LeadRequirement LR
                WHERE LR.Ref_LeadId=@Ref_LeadId
                AND LR.IsActive=1
                AND LR.IsDeleted=0;

            -- Insert the Assigned to Whom
            INSERT INTO CRM_OpportunityAssignment
            (Ref_OpportunityId, Ref_FromEmployeeId, Ref_ToEmployeeId, Ref_AssignedByEmployeeId, AssignmentType, AssignmentRemark, AssignedOn, IsCurrent, IsActive, CreatedOn, CreatedBy)
            VALUES
            (@NewOpportunityId, NULL, @AssignedEmployeeId, @CreatedBy, 'CREATE', 'Assigned during Lead to Opportunity conversion', GETDATE(), 1, 1, GETDATE(), @CreatedBy);
                       
            -- Step 8: Log in Lead Status History
           
            INSERT INTO CRM_LeadStatusHistory 
            (
                Ref_LeadId,Ref_LeadStatusId,Ref_ChangedByEmployeeId,StatusRemark,ChangedOn,CreatedOn,CreatedBy
            )
            VALUES (
                @Ref_LeadId,4,  -- Reamains Qualified 
                @CreatedBy,'Lead converted to Opportunity #' + @OppNo,GETDATE(),GETDATE(),@CreatedBy
            );

           
            -- Step 9: Return Success with IDs
           
            COMMIT TRANSACTION;

            SELECT 
                1 AS Status,
                'Lead successfully converted to Opportunity.' AS Message,
                @NewOpportunityId AS OpportunityId,
                --@NewCustomerId AS CustomerId,
                    NULL AS CustomerId,
                @OppNo AS OpportunityNo,
                @AssignedEmployeeId AS AssignedToEmployeeId;

        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;            
            SELECT  0 AS Status,ERROR_MESSAGE() AS Message,
                NULL AS OpportunityId,
                NULL AS CustomerId,
                NULL AS OpportunityNo;
        END CATCH
    END

ELSE IF(@Status='TRANSFER_OPPORTUNITY')
    BEGIN
       BEGIN TRY
        BEGIN TRAN

        IF(@Ref_OpportunityId IS NULL)
        BEGIN
            RAISERROR('Opportunity Id is required.',16,1);
        END

        IF NOT EXISTS (SELECT 1 FROM CRM_Opportunity WHERE OpportunityId=@Ref_OpportunityId AND IsActive=1)
        BEGIN
            RAISERROR('Opportunity not found or inactive.',16,1);
        END

         IF EXISTS (
        SELECT 1 FROM CRM_Opportunity O
        INNER JOIN CRM_OpportunityStage OS ON OS.OpportunityStageId = O.Ref_OpportunityStageId
        WHERE O.OpportunityId = @Ref_OpportunityId AND OS.IsClosed = 1
            )
            BEGIN
                RAISERROR('This opportunity is closed and cannot be transferred.',16,1);
            END

        IF(@AssignedToEmployeeId IS NULL)
        BEGIN
            RAISERROR('New owner is required.',16,1);
        END

        IF NOT EXISTS (SELECT 1 FROM ORG_Employee WHERE EmployeeId=@AssignedToEmployeeId AND IsActive=1)
        BEGIN
            RAISERROR('Selected employee is invalid or inactive.',16,1);
        END
        
        DECLARE @Opp_CurrentOwnerId INT;
        DECLARE @Opp_CreatorId INT;
        DECLARE @Opp_No NVARCHAR(50);

        SELECT
            @Opp_CurrentOwnerId = Ref_OwnerEmployeeId,
            @Opp_CreatorId = CreatedBy,
            @Opp_No = OpportunityNo
        FROM CRM_Opportunity
        WHERE OpportunityId = @Ref_OpportunityId;

        IF(@Opp_CurrentOwnerId = @AssignedToEmployeeId)
        BEGIN
            RAISERROR('Opportunity is already assigned to this employee.',16,1);
        END
        
        -- Block transfer if the opportunity is already Closed (Qualified/Disqualified)   
        IF EXISTS
        (
            SELECT 1 FROM CRM_Opportunity O
            INNER JOIN CRM_OpportunityStage OS ON OS.OpportunityStageId = O.Ref_OpportunityStageId
            WHERE O.OpportunityId=@Ref_OpportunityId AND OS.IsClosed=1
        )
        BEGIN
            RAISERROR('This opportunity is closed and cannot be reassigned.',16,1);
        END
        
        -- Close the previous "current" assignment row     
        UPDATE CRM_OpportunityAssignment
        SET IsCurrent = 0,
            UpdatedOn = GETDATE(),
            UpdatedBy = @CreatedBy
        WHERE Ref_OpportunityId=@Ref_OpportunityId
        AND IsCurrent=1;
      
        -- Insert new assignment row        
        INSERT INTO CRM_OpportunityAssignment
            (Ref_OpportunityId, Ref_FromEmployeeId, Ref_ToEmployeeId, Ref_AssignedByEmployeeId, AssignmentType, AssignmentRemark, AssignedOn, IsCurrent, IsActive, CreatedOn, CreatedBy)
        VALUES
            (@Ref_OpportunityId,@Opp_CurrentOwnerId,@AssignedToEmployeeId,@CreatedBy,CASE WHEN @CreatedBy=@AssignedToEmployeeId THEN 'MANUAL' ELSE 'TRANSFER' END,
                @AssignmentRemark,GETDATE(),1,1,GETDATE(),@CreatedBy);
                
        -- Update the Opportunity's current owner     
        UPDATE CRM_Opportunity
        SET Ref_OwnerEmployeeId = @AssignedToEmployeeId,
            UpdatedOn = GETDATE(),
            UpdatedBy = @CreatedBy
        WHERE OpportunityId=@Ref_OpportunityId;

        COMMIT TRAN;

        SELECT
            1 AS Status,
            'Opportunity Reassigned Successfully.' AS Message,
            @Ref_OpportunityId AS OpportunityId,
            ISNULL(@Opp_No, '') AS OpportunityNo,
            @AssignedToEmployeeId AS AssignedToEmployeeId,
            ISNULL(@Opp_CreatorId, 0) AS CreatedBy;

        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT>0 ROLLBACK TRAN;
            SELECT 0 AS Status,ERROR_MESSAGE() AS Message;
        END CATCH
    END
   
ELSE IF(@Status = 'GET_OPPORTUNITY_LIST')
    BEGIN

        DECLARE @MatchingOpportunities TABLE (OpportunityId INT);

        INSERT INTO @MatchingOpportunities
        SELECT O.OpportunityId
        FROM CRM_Opportunity O
        LEFT JOIN CRM_Lead L ON L.LeadId = O.Ref_LeadId
        LEFT JOIN CRM_LeadOrganization LO ON LO.LeadOrganizationId = L.Ref_LeadOrganizationId
        WHERE O.IsActive = 1       
        AND (
            @SearchText IS NULL
            OR ISNULL(LO.CustomerName, '') LIKE '%' + @SearchText + '%'
            OR O.OpportunityNo LIKE '%' + @SearchText + '%'
            OR O.OpportunityName LIKE '%' + @SearchText + '%'
        )
        AND (@FilterStageId IS NULL OR O.Ref_OpportunityStageId = @FilterStageId)          
       -- ACCESS FILTER
        AND(
            L.CreatedBy=@LoggedInEmployeeId
             OR            
                -- ADMIN: Show all opportunities
                EXISTS 
                (SELECT 1 FROM ORG_EmployeeRole ER
                    INNER JOIN ORG_Role R ON R.RoleId = ER.Ref_RoleId
                    WHERE ER.Ref_EmployeeId = @LoggedInEmployeeId
                      AND ER.IsPrimaryRole = 1
                      AND ER.IsActive = 1
                      AND R.RoleCode = 'ADMIN'
                )
                OR
                (-- SALES MANAGER: opportunities owned by employees reporting to them, or owned by themselves
                    EXISTS
                    (SELECT 1 FROM ORG_EmployeeRole ER
                        INNER JOIN ORG_Role R ON R.RoleId = ER.Ref_RoleId
                        WHERE ER.Ref_EmployeeId = @LoggedInEmployeeId
                          AND ER.IsPrimaryRole = 1
                          AND ER.IsActive = 1
                          AND R.RoleCode = 'SALES MANAGER'
                    )
                    AND
                    (
                        EXISTS
                        (SELECT 1 FROM ORG_Employee E
                            WHERE E.EmployeeId = O.Ref_OwnerEmployeeId
                              AND E.Ref_ReportingManagerId = @LoggedInEmployeeId
                              AND E.IsActive = 1
                        )
                        OR O.Ref_OwnerEmployeeId = @LoggedInEmployeeId
                    )
                )
                -- SALES EXECUTIVE / everyone else: own opportunities only
                OR
                (
                    @LoggedInEmployeeId IS NOT NULL
                    AND O.Ref_OwnerEmployeeId = @LoggedInEmployeeId
                )
                OR
                (
                    EXISTS
                    (
                        SELECT 1
                        FROM CRM_Ticket T
                         INNER JOIN CRM_TicketStatus TS ON TS.TicketStatusId = T.Ref_TicketStatusId   ----- VISIBLE when ticket is not completed 
                        WHERE T.Ref_OpportunityId = O.OpportunityId
                          AND T.Ref_AssignedEmployeeId = @LoggedInEmployeeId
                          AND T.IsActive = 1
                          AND TS.StatusCode <> 'COMPLETED'
                    )
                )
            )

        SELECT
            O.OpportunityId,O.OpportunityNo,O.OpportunityName,O.Ref_LeadId,LO.CustomerName AS CompanyName,OT.OpportunityTypeName,
            --OS.StageName AS OpportunityStage,
            CASE

                -- ODA Approval Pending
                WHEN EXISTS
                (
                    SELECT 1
                    FROM CRM_OpportunityQuotation Q
                    INNER JOIN CRM_OpportunityQuotationODAApproval OA
                        ON OA.Ref_OpportunityQuotationId =
                           Q.OpportunityQuotationId
                    WHERE Q.Ref_OpportunityId = O.OpportunityId
                      AND Q.IsActive = 1
                      AND Q.IsCurrent = 1
                      AND OA.DecisionCode = 'PENDING'
                )
                THEN 'ODA Approval'

                -- ODA Approval Rejected
                WHEN EXISTS
                (
                    SELECT 1
                    FROM CRM_OpportunityQuotation Q
                    INNER JOIN CRM_OpportunityQuotationODAApproval OA
                        ON OA.Ref_OpportunityQuotationId =
                           Q.OpportunityQuotationId
                    WHERE Q.Ref_OpportunityId = O.OpportunityId
                      AND Q.IsActive = 1
                      AND Q.IsCurrent = 1
                      AND OA.DecisionCode = 'REJECTED'
                )
                THEN 'ODA Approval Rejected'

                -- Management Review Pending
                WHEN EXISTS
                (
                    SELECT 1
                    FROM CRM_OpportunityQuotation Q
                    INNER JOIN CRM_OpportunityManagementReview MR
                        ON MR.Ref_OpportunityQuotationId =
                           Q.OpportunityQuotationId
                    WHERE Q.Ref_OpportunityId = O.OpportunityId
                      AND Q.IsActive = 1
                      AND Q.IsCurrent = 1
                      AND MR.DecisionCode = 'PENDING'
                )
                THEN 'Management Review'

                ELSE OS.StageName

            END AS OpportunityStage,
            OS.IsClosed,OS.IsWon,OS.IsLost,O.ExpectedRevenue,O.ExpectedCloseDate,O.Probability,P.PriorityName,E.EmployeeId AS OwnerEmployeeId,
            E.FirstName + ' ' + ISNULL(E.LastName, '') AS OwnerEmployee,O.CreatedOn
        FROM CRM_Opportunity O
        INNER JOIN @MatchingOpportunities MO ON MO.OpportunityId = O.OpportunityId        
        LEFT JOIN CRM_Lead L    ON L.LeadId = O.Ref_LeadId
        LEFT JOIN CRM_LeadOrganization LO    ON LO.LeadOrganizationId = L.Ref_LeadOrganizationId  
        LEFT JOIN CRM_OpportunityType OT ON OT.OpportunityTypeId = O.Ref_OpportunityTypeId
        LEFT JOIN CRM_OpportunityStage OS ON OS.OpportunityStageId = O.Ref_OpportunityStageId
        LEFT JOIN MST_Priority P ON P.PriorityId = O.Ref_PriorityId
        LEFT JOIN ORG_Employee E ON E.EmployeeId = O.Ref_OwnerEmployeeId
        ORDER BY O.CreatedOn DESC;
    END  

ELSE IF(@Status = 'GET_OPPORTUNITY_DETAILS')
    BEGIN      
        -- Result Set 0 : Opportunity Details       
        SELECT O.OpportunityId,O.OpportunityNo,O.Ref_LeadId,O.OpportunityName,O.ExpectedRevenue,O.ExpectedCloseDate,O.Probability,O.CompetitorName,O.OpportunityRemark,O.CreatedOn,
            -- Organization
            LO.LeadOrganizationId AS CustomerId,LO.CustomerName AS CompanyName,LO.Address,CTY.CountryName,
            ST.StateName,CI.CityName,LO.PinCode,LO.Gst AS GstNo,LO.Pan AS PanNo,
            -- Type
            OT.OpportunityTypeId,OT.OpportunityTypeName,
            -- Stage
            OS.OpportunityStageId,
            --OS.StageName AS OpportunityStage,
             CASE

                WHEN EXISTS
                (
                    SELECT 1
                    FROM CRM_OpportunityQuotation Q
                    INNER JOIN CRM_OpportunityQuotationODAApproval OA
                        ON OA.Ref_OpportunityQuotationId =
                           Q.OpportunityQuotationId
                    WHERE Q.Ref_OpportunityId = O.OpportunityId
                      AND Q.IsActive = 1
                      AND Q.IsCurrent = 1
                      AND OA.DecisionCode = 'PENDING'
                )
                THEN 'ODA Approval'

                WHEN EXISTS
                (
                    SELECT 1
                    FROM CRM_OpportunityQuotation Q
                    INNER JOIN CRM_OpportunityQuotationODAApproval OA
                        ON OA.Ref_OpportunityQuotationId =
                           Q.OpportunityQuotationId
                    WHERE Q.Ref_OpportunityId = O.OpportunityId
                      AND Q.IsActive = 1
                      AND Q.IsCurrent = 1
                      AND OA.DecisionCode = 'REJECTED'
                )
                THEN 'ODA Approval Rejected'

                WHEN EXISTS
                (
                    SELECT 1
                    FROM CRM_OpportunityQuotation Q
                    INNER JOIN CRM_OpportunityManagementReview MR
                        ON MR.Ref_OpportunityQuotationId =
                           Q.OpportunityQuotationId
                    WHERE Q.Ref_OpportunityId = O.OpportunityId
                      AND Q.IsActive = 1
                      AND Q.IsCurrent = 1
                      AND MR.DecisionCode = 'PENDING'
                )
                THEN 'Management Review'

                ELSE OS.StageName

            END AS OpportunityStage,
            OS.IsClosed,OS.IsWon,OS.IsLost,
            -- Priority
            P.PriorityId,P.PriorityName,P.PriorityLevel,
            -- Owner
            E.EmployeeId AS OwnerEmployeeId,E.FirstName + ' ' + ISNULL(E.LastName,'') AS OwnerEmployeeName,E.OfficialEmail AS OwnerEmail,E.Mobile AS OwnerMobileNo,R.RoleId AS OwnerRoleId,R.RoleName AS OwnerRoleName
        FROM CRM_Opportunity O
        LEFT JOIN CRM_Lead L ON L.LeadId = O.Ref_LeadId
        LEFT JOIN CRM_LeadOrganization LO ON LO.LeadOrganizationId = L.Ref_LeadOrganizationId
        LEFT JOIN MST_Country CTY ON CTY.CountryId = LO.Ref_CountryId
        LEFT JOIN MST_State ST ON ST.StateId = LO.Ref_StateId
        LEFT JOIN MST_City CI ON CI.CityId = LO.Ref_CityId
        LEFT JOIN CRM_OpportunityType OT ON OT.OpportunityTypeId = O.Ref_OpportunityTypeId
        LEFT JOIN CRM_OpportunityStage OS ON OS.OpportunityStageId = O.Ref_OpportunityStageId
        LEFT JOIN MST_Priority P ON P.PriorityId = O.Ref_PriorityId
        LEFT JOIN ORG_Employee E ON E.EmployeeId = O.Ref_OwnerEmployeeId
        LEFT JOIN ORG_EmployeeRole ER ON ER.Ref_EmployeeId = E.EmployeeId
            AND ER.IsPrimaryRole = 1
            AND ER.IsActive = 1
        LEFT JOIN ORG_Role R ON R.RoleId = ER.Ref_RoleId
        WHERE O.OpportunityId = @Ref_OpportunityId;
      
        -- Result Set 1 : Contacts (Read directly from Lead)
        SELECT
            LC.LeadContactId AS CustomerContactId,LC.ContactPerson,D.DesignationName,LC.Mobile,LC.Email,CAST(1 AS BIT) AS IsPrimary
        FROM CRM_LeadContact LC
        LEFT JOIN ORG_Designation D ON D.DesignationId = LC.Ref_DesignationId
        WHERE LC.Ref_LeadOrganizationId =
        (
            SELECT L.Ref_LeadOrganizationId
            FROM CRM_Opportunity O
            INNER JOIN CRM_Lead L ON L.LeadId = O.Ref_LeadId
            WHERE O.OpportunityId = @Ref_OpportunityId
        )
        ORDER BY LC.ContactPerson;
        ------------------------------------------------------------
        -- Result Set 2 : Requirements
        -- Lead Requirements + Opportunity Overrides + New Rows
        ------------------------------------------------------------

        SELECT LR.LeadRequirementId,ISNULL(ORQ.OpportunityRequirementId,0) AS OpportunityRequirementId,PC.ProductCategoryId AS CategoryId,PC.CategoryName,O.ProductId AS OfferingId,O.ProductName AS OfferingName,
            P.ProductId,P.ItemName AS ProductName,ISNULL(ORQ.Quantity,LR.Quantity) AS Quantity,ISNULL(ORQ.RequirementRemark,LR.RequirementRemark) AS Remark,U.Symbol AS Unit,CAST(CASE WHEN ORQ.OpportunityRequirementId IS NULL THEN 0 ELSE 1 END AS BIT) AS IsOpportunityRequirement,
            T.TicketId,T.TicketNo,T.Ref_AssignedEmployeeId AS TicketAssignedEmployeeId,TS.StatusCode,TS.StatusName
        FROM CRM_LeadRequirement LR
        LEFT JOIN MST_ProductCategory PC ON PC.ProductCategoryId=LR.Ref_ProductCategoryId
        LEFT JOIN MST_Offering O ON O.ProductId=LR.Ref_OfferingId
        LEFT JOIN MST_Product P ON P.ProductId=LR.Ref_ProductId
        LEFT JOIN MST_Unit U ON U.UnitId=LR.Ref_UnitId
        LEFT JOIN CRM_OpportunityRequirement ORQ
            ON ORQ.Ref_LeadRequirementId=LR.LeadRequirementId
            AND ORQ.Ref_OpportunityId=@Ref_OpportunityId
            AND ORQ.IsActive=1
            AND ORQ.IsDeleted=0       
        OUTER APPLY
            (SELECT TOP 1 T.TicketId,T.TicketNo,T.Ref_AssignedEmployeeId,T.Ref_TicketStatusId
                FROM CRM_Ticket T
                WHERE T.Ref_OpportunityRequirementId=ORQ.OpportunityRequirementId
                AND T.IsActive=1
                ORDER BY T.CreatedOn DESC,T.TicketId DESC
            ) T
        LEFT JOIN CRM_TicketStatus TS ON TS.TicketStatusId=T.Ref_TicketStatusId
        WHERE LR.Ref_LeadId=
        (
            SELECT Ref_LeadId
            FROM CRM_Opportunity
            WHERE OpportunityId=@Ref_OpportunityId
        )
        AND LR.IsActive=1
        AND LR.IsDeleted=0
        AND ORQ.OpportunityRequirementId IS NOT NULL
        AND NOT EXISTS
        (
            SELECT 1
            FROM CRM_OpportunityRequirement ORQD
            WHERE ORQD.Ref_OpportunityId=@Ref_OpportunityId
            AND ORQD.Ref_LeadRequirementId=LR.LeadRequirementId
            AND ORQD.IsDeleted=1
        )

        UNION ALL

        SELECT NULL AS LeadRequirementId,ORQ.OpportunityRequirementId,PC.ProductCategoryId AS CategoryId,PC.CategoryName,O.ProductId AS OfferingId,O.ProductName AS OfferingName,P.ProductId,P.ItemName AS ProductName,
            ORQ.Quantity,ORQ.RequirementRemark AS Remark,U.Symbol AS Unit,CAST(1 AS BIT) AS IsOpportunityRequirement,T.TicketId,T.TicketNo,T.Ref_AssignedEmployeeId AS TicketAssignedEmployeeId,TS.StatusCode,TS.StatusName
        FROM CRM_OpportunityRequirement ORQ
        LEFT JOIN MST_Product P ON P.ProductId=ORQ.Ref_ProductId
        LEFT JOIN MST_Offering O ON O.ProductId=ORQ.Ref_OfferingId
        LEFT JOIN MST_ProductCategory PC ON PC.ProductCategoryId=ORQ.Ref_ProductCategoryId
        LEFT JOIN MST_Unit U ON U.UnitId=ORQ.Ref_UnitId        
        OUTER APPLY
             (SELECT TOP 1 T.TicketId,T.TicketNo,T.Ref_AssignedEmployeeId,T.Ref_TicketStatusId
                FROM CRM_Ticket T
                WHERE T.Ref_OpportunityRequirementId=ORQ.OpportunityRequirementId
                AND T.IsActive=1
                ORDER BY T.CreatedOn DESC,T.TicketId DESC
            ) T 
        LEFT JOIN CRM_TicketStatus TS ON TS.TicketStatusId=T.Ref_TicketStatusId
        WHERE ORQ.Ref_OpportunityId=@Ref_OpportunityId
        AND ORQ.Ref_LeadRequirementId IS NULL
        AND ORQ.IsActive=1
        AND ORQ.IsDeleted=0;
     
        -- Result Set 3 : Opportunity Activities
        -- with Activity Attachments + Expenses + Expense Attachments    
        SELECT OA.ActivityId,AT.ActivityName AS ActivityType,OA.ActivityDateTime AS ActivityDate,OA.Subject,OA.Description,OA.Outcome,OA.LocationAddress,E.FirstName + ' ' + ISNULL(E.LastName,'') AS CreatedBy,
            OA.Ref_ActivityStatusId,AST.ActivityStatusName AS ActivityStatus,AST.ColorCode AS ActivityStatusColor,           
            -- NORMAL ACTIVITY ATTACHMENTS      
            (SELECT OAA.AttachmentId,OAA.FileName,OAA.FilePath,OAA.FileType,OAA.FileSize,OAA.CreatedOn
                FROM CRM_OpportunityActivityAttachment OAA
                WHERE OAA.Ref_OpportunityActivityId = OA.ActivityId
                  AND OAA.Ref_ActivityExpenseId IS NULL
                ORDER BY OAA.CreatedOn DESC
                FOR JSON PATH
            ) AS AttachmentsJson,
            
            -- EXPENSES          
            (SELECT AE.ActivityExpenseId,AE.Ref_ExpenseCategoryId,EC.CategoryCode,EC.CategoryName,AE.Ref_TransportModeId,TM.ModeCode,TM.ModeName,AE.ExpenseDate,AE.Amount,AE.Description,
                    AE.FromLocation,AE.ToLocation,AE.CreatedOn,                  
                    -- ATTACHMENTS BELONGING TO THIS EXPENSE ONLY                  
                    (SELECT EA.AttachmentId,EA.FileName,EA.FilePath,EA.FileType,EA.FileSize,EA.CreatedOn 
                    FROM CRM_OpportunityActivityAttachment EA
                        WHERE EA.Ref_ActivityExpenseId = AE.ActivityExpenseId
                          AND EA.Ref_OpportunityActivityId = OA.ActivityId
                        ORDER BY EA.CreatedOn DESC
                        FOR JSON PATH
                    ) AS AttachmentsJson

                FROM CRM_ActivityExpense AE
                INNER JOIN CRM_ActivityExpenseCategory EC ON EC.ExpenseCategoryId = AE.Ref_ExpenseCategoryId
                LEFT JOIN CRM_ActivityTransportMode TM ON TM.TransportModeId = AE.Ref_TransportModeId
                WHERE AE.Ref_OpportunityId = @Ref_OpportunityId
                  AND AE.Ref_OpportunityActivityId = OA.ActivityId
                  AND AE.IsActive = 1
                ORDER BY AE.ExpenseDate DESC, AE.ActivityExpenseId DESC

                FOR JSON PATH
            ) AS ExpensesJson

        FROM CRM_OpportunityActivity OA
        LEFT JOIN CRM_LeadActivityType AT ON AT.ActivityTypeId = OA.Ref_ActivityTypeId
        LEFT JOIN ORG_Employee E ON E.EmployeeId = OA.Ref_EmployeeId
        LEFT JOIN CRM_LeadActivityStatus AST ON AST.ActivityStatusId = OA.Ref_ActivityStatusId
        WHERE OA.Ref_OpportunityId = @Ref_OpportunityId
          AND OA.IsActive = 1
          AND OA.Ref_ActivityTypeId <> 8
        ORDER BY OA.ActivityDateTime DESC;
       
        -- Result Set 4 : Opportunity Followups
        SELECT
            OFU.OpportunityFollowUpId AS FollowUpId,OFU.Ref_OpportunityActivityId AS ActivityId,OFU.Ref_AssignedEmployeeId AS AssignedEmployeeId,
            FE.FirstName + ' ' + ISNULL(FE.LastName,'') AS AssignedTo,OFU.ScheduledOn AS FollowUpDate,OFU.Priority,OFU.Remarks,OFU.IsCompleted,OFU.CompletedOn,
            AST.ActivityStatusName AS FollowUpStatus,AST.ColorCode AS FollowUpStatusColor,OFU.CreatedOn,OFU.CreatedBy,E.FirstName + ' ' + ISNULL(E.LastName,'') AS CreatedByName
        FROM CRM_OpportunityFollowUp OFU

        INNER JOIN CRM_OpportunityActivity OA ON OA.ActivityId=OFU.Ref_OpportunityActivityId
        LEFT JOIN CRM_LeadActivityStatus AST ON AST.ActivityStatusId=OFU.Ref_ActivityStatusId
        LEFT JOIN ORG_Employee FE ON FE.EmployeeId=OFU.Ref_AssignedEmployeeId
        LEFT JOIN ORG_Employee E ON E.EmployeeId=OFU.CreatedBy
        WHERE OFU.Ref_OpportunityId=@Ref_OpportunityId
        ORDER BY OFU.ScheduledOn DESC;
      
        -- Result Set 5 : Original Lead Attachments (general files)
        SELECT LA.AttachmentId,LA.FileName,LA.FilePath,LA.FileType,LA.FileSize,LA.CreatedOn
        FROM CRM_LeadAttachment LA
        WHERE LA.Ref_LeadId =
        (
            SELECT Ref_LeadId FROM CRM_Opportunity WHERE OpportunityId = @Ref_OpportunityId
        )
        ORDER BY LA.CreatedOn DESC;
        
        -- Result Set 6 : Unified Activity Timeline
        -- (Opportunity Activities + Opportunity FollowUps merged, true chronological order)
        SELECT 'ACTIVITY' AS ItemType,OA.ActivityId AS ItemId,AT.ActivityName AS ActivityType,OA.ActivityDateTime AS ActivityDate,OA.Subject,OA.Description,OA.Outcome,OA.LocationAddress,
            E.FirstName + ' ' + ISNULL(E.LastName,'') AS CreatedBy,AST.ActivityStatusName AS Status,AST.ColorCode AS StatusColor,OA.CreatedOn,
            ( SELECT OAA.AttachmentId,OAA.FileName,OAA.FilePath,OAA.FileType,OAA.FileSize,OAA.CreatedOn
                FROM CRM_OpportunityActivityAttachment OAA
                WHERE OAA.Ref_OpportunityActivityId = OA.ActivityId
                  AND OAA.Ref_ActivityExpenseId IS NULL
                ORDER BY OAA.CreatedOn DESC
                FOR JSON PATH
            ) AS AttachmentsJson,
            ( SELECT AE.ActivityExpenseId,AE.Ref_ExpenseCategoryId,EC.CategoryCode,EC.CategoryName,AE.Ref_TransportModeId,TM.ModeCode,TM.ModeName,AE.ExpenseDate,AE.Amount,AE.Description,
                    AE.FromLocation,AE.ToLocation,AE.CreatedOn,
                    ( SELECT EA.AttachmentId,EA.FileName,EA.FilePath,EA.FileType,EA.FileSize,EA.CreatedOn
                        FROM CRM_OpportunityActivityAttachment EA
                        WHERE EA.Ref_ActivityExpenseId = AE.ActivityExpenseId
                          AND EA.Ref_OpportunityActivityId = OA.ActivityId
                        ORDER BY EA.CreatedOn DESC
                        FOR JSON PATH
                    ) AS AttachmentsJson

                FROM CRM_ActivityExpense AE
                INNER JOIN CRM_ActivityExpenseCategory EC ON EC.ExpenseCategoryId = AE.Ref_ExpenseCategoryId
                LEFT JOIN CRM_ActivityTransportMode TM ON TM.TransportModeId = AE.Ref_TransportModeId
                WHERE AE.Ref_OpportunityId = @Ref_OpportunityId
                  AND AE.Ref_OpportunityActivityId = OA.ActivityId
                  AND AE.IsActive = 1
                ORDER BY AE.ExpenseDate DESC, AE.ActivityExpenseId DESC
                FOR JSON PATH
            ) AS ExpensesJson

        FROM CRM_OpportunityActivity OA
        LEFT JOIN CRM_LeadActivityType AT ON AT.ActivityTypeId = OA.Ref_ActivityTypeId
        LEFT JOIN ORG_Employee E ON E.EmployeeId = OA.Ref_EmployeeId
        LEFT JOIN CRM_LeadActivityStatus AST ON AST.ActivityStatusId = OA.Ref_ActivityStatusId
        WHERE OA.Ref_OpportunityId = @Ref_OpportunityId
          AND OA.IsActive = 1
          AND OA.Ref_ActivityTypeId <> 8

        UNION ALL

        SELECT 'FOLLOWUP' AS ItemType,OFU.OpportunityFollowUpId AS ItemId,'Follow Up' AS ActivityType,OFU.CreatedOn AS ActivityDate,'Follow-up ' AS Subject,OFU.Remarks AS Description,NULL AS Outcome,
            OA2.LocationAddress,E2.FirstName + ' ' + ISNULL(E2.LastName,'') AS CreatedBy,AST2.ActivityStatusName AS Status,AST2.ColorCode AS StatusColor,
            OFU.CreatedOn,'[]' AS AttachmentsJson,'[]' AS ExpensesJson
        FROM CRM_OpportunityFollowUp OFU 
        INNER JOIN CRM_OpportunityActivity OA2 ON OA2.ActivityId = OFU.Ref_OpportunityActivityId
        LEFT JOIN ORG_Employee E2 ON E2.EmployeeId = OFU.CreatedBy
        LEFT JOIN CRM_LeadActivityStatus AST2 ON AST2.ActivityStatusId = OFU.Ref_ActivityStatusId
        WHERE OFU.Ref_OpportunityId = @Ref_OpportunityId
        ORDER BY CreatedOn DESC, ItemId DESC;
 
        -- Result Set 7 : Tickets
        SELECT T.TicketId,T.TicketNo,T.Ref_OpportunityId AS OpportunityId,T.Ref_OpportunityRequirementId AS OpportunityRequirementId,P.ProductId,P.ItemName AS ProductName,T.Priority,
            T.ScheduledOn,T.Description,T.Ref_AssignedEmployeeId AS AssignedEmployeeId,AE.FirstName+' '+ISNULL(AE.LastName,'') AS AssignedEmployeeName,T.Ref_TicketStatusId AS TicketStatusId,
            TS.StatusCode,TS.StatusName,T.CompletedOn,T.CompletedBy,T.CreatedOn,T.CreatedBy,T.UpdatedOn,T.UpdatedBy,
            -- Latest work/activity on this ticket
            LA.ActivityId AS LastActivityId,LAT.ActivityName AS LastActivityType,LAS.ActivityStatusName AS LastActivityStatus,LA.ActivityDateTime AS LastActivityDate,LA.Subject AS LastActivitySubject,
            LA.Description AS LastActivityDescription,LA.Outcome AS LastActivityOutcome,
            -- Complete work history of this ticket
           ISNULL
           (
                ( SELECT OA.ActivityId,OA.Ref_TicketId AS TicketId,OA.Ref_ActivityTypeId AS ActivityTypeId,LAT2.ActivityCode,LAT2.ActivityName,OA.Ref_EmployeeId AS EmployeeId,
                        E2.FirstName+' '+ISNULL(E2.LastName,'') AS EmployeeName,OA.ActivityDateTime,OA.Subject,OA.Description,OA.Outcome,OA.Ref_ActivityStatusId AS ActivityStatusId,
                        LAS2.ActivityStatusCode,LAS2.ActivityStatusName,OA.NextFollowUpDate,OA.DurationInMinutes,OA.Latitude,OA.Longitude,OA.LocationAddress,OA.CreatedOn,OA.CreatedBy,OA.UpdatedOn,OA.UpdatedBy
                    FROM CRM_OpportunityActivity OA
                    LEFT JOIN CRM_LeadActivityType LAT2 ON LAT2.ActivityTypeId=OA.Ref_ActivityTypeId
                    LEFT JOIN CRM_LeadActivityStatus LAS2 ON LAS2.ActivityStatusId=OA.Ref_ActivityStatusId
                    LEFT JOIN ORG_Employee E2 ON E2.EmployeeId=OA.Ref_EmployeeId
                    WHERE OA.Ref_TicketId=T.TicketId
                    AND OA.IsActive=1
                    ORDER BY OA.ActivityDateTime DESC,OA.ActivityId DESC

                    FOR JSON PATH
                ),'[]') AS WorkHistory

        FROM CRM_Ticket T 
        INNER JOIN CRM_OpportunityRequirement ORQ ON ORQ.OpportunityRequirementId=T.Ref_OpportunityRequirementId
        LEFT JOIN MST_Product P ON P.ProductId=ORQ.Ref_ProductId
        LEFT JOIN CRM_TicketStatus TS ON TS.TicketStatusId=T.Ref_TicketStatusId
        LEFT JOIN ORG_Employee AE ON AE.EmployeeId=T.Ref_AssignedEmployeeId
        OUTER APPLY
            ( SELECT TOP 1 OA.ActivityId,OA.Ref_ActivityTypeId,OA.Ref_ActivityStatusId,OA.ActivityDateTime,OA.Subject,OA.Description,OA.Outcome
                FROM CRM_OpportunityActivity OA
                WHERE OA.Ref_TicketId=T.TicketId
                AND OA.IsActive=1
                ORDER BY OA.ActivityDateTime DESC,OA.ActivityId DESC
            ) LA
        LEFT JOIN CRM_LeadActivityType LAT ON LAT.ActivityTypeId=LA.Ref_ActivityTypeId
        LEFT JOIN CRM_LeadActivityStatus LAS ON LAS.ActivityStatusId=LA.Ref_ActivityStatusId
        WHERE T.Ref_OpportunityId=@Ref_OpportunityId
        AND T.IsActive=1       
        AND
        (
            T.Ref_AssignedEmployeeId=@LoggedInEmployeeId

            OR EXISTS
            (
                SELECT 1
                FROM CRM_Opportunity OPP
                WHERE OPP.OpportunityId=T.Ref_OpportunityId
                AND
                (
                    OPP.Ref_OwnerEmployeeId=@LoggedInEmployeeId
                    OR OPP.CreatedBy=@LoggedInEmployeeId
                )
            )

            -- Manager of Opportunity Owner
            OR EXISTS
            (
                SELECT 1
                FROM CRM_Opportunity OPP
                INNER JOIN ORG_Employee OE
                    ON OE.EmployeeId=OPP.Ref_OwnerEmployeeId
                WHERE OPP.OpportunityId=T.Ref_OpportunityId
                AND OE.Ref_ReportingManagerId=@LoggedInEmployeeId
            )

            OR EXISTS
            (
                SELECT 1
                FROM ORG_EmployeeRole ER
                INNER JOIN ORG_Role R
                    ON R.RoleId=ER.Ref_RoleId
                WHERE ER.Ref_EmployeeId=@LoggedInEmployeeId
                AND ER.IsPrimaryRole=1
                AND ER.IsActive=1
                AND R.RoleCode='ADMIN'
            )
        )

        ORDER BY T.CreatedOn DESC;
    END  
    
ELSE IF(@Status = 'UPDATE_OPPORTUNITY')
    BEGIN
        BEGIN TRY
            IF NOT EXISTS (SELECT 1 FROM CRM_Opportunity WHERE OpportunityId = @Ref_OpportunityId AND IsActive = 1)
            BEGIN
                RAISERROR('Opportunity not found.', 16, 1);
            END

            UPDATE CRM_Opportunity
            SET 
                OpportunityName = ISNULL(@OpportunityName, OpportunityName),
                Ref_OpportunityTypeId = ISNULL(@Ref_OpportunityTypeId, Ref_OpportunityTypeId),
                Ref_OpportunityStageId = ISNULL(@Ref_OpportunityStageId, Ref_OpportunityStageId),
                ExpectedRevenue = ISNULL(@ExpectedRevenue, ExpectedRevenue),
                ExpectedCloseDate = ISNULL(@ExpectedCloseDate, ExpectedCloseDate),
                Probability = ISNULL(@Probability, Probability),
                Ref_PriorityId = ISNULL(@Ref_PriorityId, Ref_PriorityId),
                OpportunityRemark = ISNULL(@OpportunityRemark, OpportunityRemark),
                UpdatedOn = GETDATE(),
                UpdatedBy = @CreatedBy
            WHERE OpportunityId = @Ref_OpportunityId;

            SELECT 
                1 AS Status,
                'Opportunity updated successfully.' AS Message;

        END TRY
        BEGIN CATCH
            SELECT 0 AS Status,ERROR_MESSAGE() AS Message;
        END CATCH
    END  
     
ELSE IF(@Status = 'DELETE_OPPORTUNITY')
    BEGIN
        BEGIN TRY
            IF NOT EXISTS (SELECT 1 FROM CRM_Opportunity WHERE OpportunityId = @Ref_OpportunityId AND IsActive = 1)
            BEGIN
                RAISERROR('Opportunity not found.', 16, 1);
            END

            UPDATE CRM_Opportunity
            SET 
                IsActive = 0,
                UpdatedOn = GETDATE(),
                UpdatedBy = @CreatedBy
            WHERE OpportunityId = @Ref_OpportunityId;

            SELECT 1 AS Status,'Opportunity deleted successfully.' AS Message;

        END TRY
        BEGIN CATCH
            SELECT 0 AS Status,ERROR_MESSAGE() AS Message;
        END CATCH
    END
     
ELSE IF(@Status = 'GET_OPPORTUNITY_TYPES')
    BEGIN
        SELECT OpportunityTypeId AS Id,OpportunityTypeName AS Name
        FROM CRM_OpportunityType
        WHERE IsActive = 1
        ORDER BY OpportunityTypeName;
    END

ELSE IF(@Status = 'GET_OPPORTUNITY_STAGES')
    BEGIN
        SELECT OpportunityStageId AS Id,StageName AS Name,IsClosed,IsWon,IsLost
        FROM CRM_OpportunityStage
        WHERE IsActive = 1
        ORDER BY StageOrder;
    END

ELSE IF(@Status = 'GET_OPPORTUNITY_PRIORITIES')
    BEGIN
        SELECT PriorityId AS Id,PriorityName AS Name,PriorityLevel
        FROM MST_Priority
        WHERE IsActive = 1
        ORDER BY PriorityLevel;
    END

ELSE IF @Status = 'GET_ASSIGNED_EMPLOYEE'
    BEGIN
        SELECT TOP 1 Ref_ToEmployeeId AS Ref_CurrentAssignedEmployeeId
        FROM CRM_OpportunityAssignment
        WHERE Ref_OpportunityId = @Ref_OpportunityId
          AND IsCurrent = 1
          AND IsActive = 1;
    END

ELSE IF(@Status = 'GET_OPPORTUNITY_ACCESS_INFO')
    BEGIN
        SELECT CreatedBy,Ref_OwnerEmployeeId
        FROM CRM_Opportunity
        WHERE OpportunityId = @Ref_OpportunityId;

        RETURN;
    END

ELSE IF(@Status='CREATE_OPPORTUNITY_ACTIVITY')
    BEGIN
        BEGIN TRY
        BEGIN TRAN

        IF EXISTS (SELECT 1 FROM CRM_Opportunity O INNER JOIN CRM_OpportunityStage OS ON OS.OpportunityStageId=O.Ref_OpportunityStageId WHERE O.OpportunityId=@Ref_OpportunityId AND OS.IsClosed=1)
        BEGIN
            RAISERROR('This opportunity is closed (Qualified/Disqualified) and cannot be modified further.',16,1);
        END

        IF(@Ref_OpportunityId IS NULL)
        BEGIN
            RAISERROR('Opportunity is required.',16,1);
        END

        IF NOT EXISTS (SELECT 1 FROM CRM_Opportunity WHERE OpportunityId=@Ref_OpportunityId)
        BEGIN
            RAISERROR('Opportunity not found.',16,1);
        END

        IF(@Ref_ActivityTypeId IS NULL)
        BEGIN
            RAISERROR('Activity Type is required.',16,1);
        END

        IF NOT EXISTS (SELECT 1 FROM CRM_LeadActivityType WHERE ActivityTypeId=@Ref_ActivityTypeId AND IsActive=1)
        BEGIN
            RAISERROR('Selected Activity Type is invalid or inactive.',16,1);
        END

        IF(@Subject IS NULL OR LTRIM(RTRIM(@Subject))='')
        BEGIN
            RAISERROR('Subject / Title is required.',16,1);
        END

        IF(@Description IS NULL OR LTRIM(RTRIM(@Description))='')
        BEGIN
            RAISERROR('Description / Outcome is required.',16,1);
        END

        IF(@ActivityDateTime IS NULL)
        BEGIN
            RAISERROR('Activity Date & Time is required.',16,1);
        END

        IF(@DurationInMinutes IS NOT NULL AND @DurationInMinutes < 0)
        BEGIN
            RAISERROR('Duration cannot be negative.',16,1);
        END

        IF(@Ref_ActivityStatusId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM CRM_LeadActivityStatus WHERE ActivityStatusId=@Ref_ActivityStatusId AND IsActive=1))
        BEGIN
            RAISERROR('Selected Activity Status is invalid or inactive.',16,1);
        END

        IF(@Ref_NewOpportunityStageId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM CRM_OpportunityStage WHERE OpportunityStageId=@Ref_NewOpportunityStageId AND IsActive=1))
        BEGIN
            RAISERROR('Selected Opportunity Stage is invalid or inactive.',16,1);
        END

        -- Auto-resolve current assignment and current stage       
        DECLARE @Ref_OpportunityAssignmentId INT;
        DECLARE @Ref_PreviousOpportunityStageId INT;

        SELECT @Ref_OpportunityAssignmentId = OpportunityAssignmentId
        FROM CRM_OpportunityAssignment
        WHERE Ref_OpportunityId=@Ref_OpportunityId AND IsCurrent=1;

        SELECT @Ref_PreviousOpportunityStageId = Ref_OpportunityStageId
        FROM CRM_Opportunity
        WHERE OpportunityId=@Ref_OpportunityId;

        DECLARE @FinalActivityStatusId INT = ISNULL(@Ref_ActivityStatusId, 3); -- 3 = Completed
        DECLARE @FinalNewOpportunityStageId INT = ISNULL(@Ref_NewOpportunityStageId, @Ref_PreviousOpportunityStageId);

        -- Insert Activity — brand new, NOT copied from Lead stage,
        -- so Ref_SourceLeadActivityId stays NULL
        INSERT INTO CRM_OpportunityActivity
        (
            Ref_OpportunityId,Ref_OpportunityAssignmentId,Ref_ActivityTypeId,Ref_EmployeeId,ActivityDateTime, Subject,Description,Outcome,NextFollowUpDate,
            DurationInMinutes,Latitude,Longitude,LocationAddress,Ref_OpportunityStageId,Ref_ActivityStatusId,Ref_PreviousOpportunityStageId,Ref_NewOpportunityStageId,Ref_SourceLeadActivityId,IsActive,CreatedOn,CreatedBy
        )
        VALUES
        (
            @Ref_OpportunityId,@Ref_OpportunityAssignmentId,@Ref_ActivityTypeId,@CreatedBy,@ActivityDateTime,@Subject,@Description,@Outcome,@NextFollowUpDate,
            @DurationInMinutes,@Latitude,@Longitude,@LocationAddress,@FinalNewOpportunityStageId,@FinalActivityStatusId,@Ref_PreviousOpportunityStageId,@FinalNewOpportunityStageId,
            NULL,  -- new activity, not carried over from a Lead
            1,
            GETDATE(),
            @CreatedBy
        );

        DECLARE @OppActivityId INT;
        SET @OppActivityId = SCOPE_IDENTITY();
       
        -- If a real stage change happened, update CRM_Opportunity + log history
        IF(@FinalNewOpportunityStageId <> @Ref_PreviousOpportunityStageId)
        BEGIN
            UPDATE CRM_Opportunity
            SET Ref_OpportunityStageId = @FinalNewOpportunityStageId,
                UpdatedOn = GETDATE(),
                UpdatedBy = @CreatedBy
            WHERE OpportunityId=@Ref_OpportunityId;

            INSERT INTO CRM_OpportunityStatusHistory
            (
                Ref_OpportunityId,Ref_OpportunityStageId,Ref_ChangedByEmployeeId,StatusRemark,ChangedOn,CreatedOn,CreatedBy,Ref_PreviousOpportunityStageId
            )
            VALUES
            (
                @Ref_OpportunityId,@FinalNewOpportunityStageId,@CreatedBy,'Changed via activity: ' + @Subject,GETDATE(),GETDATE(),@CreatedBy,@Ref_PreviousOpportunityStageId
            );
        END

        -- Insert Attachments (metadata only)      
        IF(@AttachmentsMeta IS NOT NULL AND LTRIM(RTRIM(@AttachmentsMeta))<>'')
        BEGIN
            INSERT INTO CRM_OpportunityActivityAttachment
            (
                Ref_OpportunityActivityId,FileName,FilePath,FileType,FileSize,CreatedOn,CreatedBy
            )
            SELECT
                @OppActivityId,FileName,FilePath,FileType,FileSize,GETDATE(),@CreatedBy
            FROM OPENJSON(@AttachmentsMeta)
            WITH
            (
                FileName    NVARCHAR(300) '$.FileName',
                FilePath    NVARCHAR(500) '$.FilePath',
                FileType    NVARCHAR(50)  '$.FileType',
                FileSize    BIGINT        '$.FileSize'
            );
        END

        COMMIT TRAN;

        SELECT
        1 AS Status,
        'Activity Logged Successfully.' AS Message,
        @OppActivityId AS ActivityId;

        END TRY

        BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK TRAN;
        SELECT 0 AS Status,ERROR_MESSAGE() AS Message;

        END CATCH
    END

ELSE IF(@Status='CREATE_OPPORTUNITY_FOLLOWUP')
    BEGIN
        BEGIN TRY
        BEGIN TRAN

        IF EXISTS (SELECT 1 FROM CRM_Opportunity O INNER JOIN CRM_OpportunityStage OS ON OS.OpportunityStageId=O.Ref_OpportunityStageId WHERE O.OpportunityId=@Ref_OpportunityId AND OS.IsClosed=1)
        BEGIN
            RAISERROR('This opportunity is closed (Qualified/Disqualified) and cannot be modified further.',16,1);
        END

        IF(@FollowUpAssignedEmployeeId = 0)
        BEGIN
            SET @FollowUpAssignedEmployeeId = NULL;
        END

        IF(@Ref_OpportunityId IS NULL)
        BEGIN
            RAISERROR('Opportunity is required.',16,1);
        END

        IF NOT EXISTS (SELECT 1 FROM CRM_Opportunity WHERE OpportunityId=@Ref_OpportunityId)
        BEGIN
            RAISERROR('Opportunity not found.',16,1);
        END

        IF(@NextFollowUpDate IS NULL)
        BEGIN
            RAISERROR('Follow-up Date & Time is required.',16,1);
        END

        IF(@FollowUpAssignedEmployeeId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ORG_Employee WHERE EmployeeId=@FollowUpAssignedEmployeeId AND IsActive=1))
        BEGIN
            RAISERROR('Selected Follow-up Assignee is invalid or inactive.',16,1);
        END

        DECLARE @Ref_OppLeadAssignmentId INT;
        DECLARE @Ref_CurrentOpportunityStageId INT;

        SELECT @Ref_OppLeadAssignmentId = OpportunityAssignmentId
        FROM CRM_OpportunityAssignment
        WHERE Ref_OpportunityId=@Ref_OpportunityId AND IsCurrent=1;

        SELECT @Ref_CurrentOpportunityStageId = Ref_OpportunityStageId
        FROM CRM_Opportunity
        WHERE OpportunityId=@Ref_OpportunityId;

        -------------------------------------------------------------
        -- Auto-create the linked Activity (Ref_ActivityTypeId=8, FOLLOWUP)
        -- Status = 1 (Scheduled). New activity — Ref_SourceLeadActivityId NULL
        -------------------------------------------------------------
        --select * from CRM_OpportunityActivity
        INSERT INTO CRM_OpportunityActivity
        (
            Ref_OpportunityId,Ref_OpportunityAssignmentId,Ref_ActivityTypeId,Ref_EmployeeId,ActivityDateTime,Subject,Description,NextFollowUpDate,
            Ref_OpportunityStageId,Ref_ActivityStatusId,Ref_PreviousOpportunityStageId,Ref_NewOpportunityStageId,Ref_SourceLeadActivityId,IsActive,CreatedOn,CreatedBy
        )
        VALUES
        (
            @Ref_OpportunityId,@Ref_OppLeadAssignmentId,8, -- FOLLOWUP activity type (shared with Lead)
            ISNULL(@FollowUpAssignedEmployeeId, @CreatedBy),GETDATE(),'Follow-up Scheduled',
            ISNULL(@FollowUpRemarks, 'Follow-up scheduled for ' + CONVERT(VARCHAR(20), @NextFollowUpDate, 113)),
             @NextFollowUpDate, @Ref_CurrentOpportunityStageId,1, -- Scheduled
            @Ref_CurrentOpportunityStageId,@Ref_CurrentOpportunityStageId,NULL,1,GETDATE(),@CreatedBy
        );

        DECLARE @NewOppActivityId INT;
        SET @NewOppActivityId = SCOPE_IDENTITY();

        -------------------------------------------------------------
        -- Insert Follow-up, linked to the activity just created
        -------------------------------------------------------------

        DECLARE @FinalOppPriority TINYINT = ISNULL(@FollowUpPriority, 2); -- default MEDIUM

        INSERT INTO CRM_OpportunityFollowUp
        (
            Ref_OpportunityId,Ref_OpportunityActivityId,Ref_AssignedEmployeeId,ScheduledOn,Priority,Remarks,IsCompleted,Ref_ActivityStatusId,
            Ref_SourceLeadFollowUpId,CreatedOn,CreatedBy
        )
        VALUES
        (
            @Ref_OpportunityId,@NewOppActivityId,ISNULL(@FollowUpAssignedEmployeeId, @CreatedBy),@NextFollowUpDate,@FinalOppPriority,@FollowUpRemarks,
            0,1,NULL,GETDATE(),@CreatedBy
        );

        DECLARE @OppFollowUpId INT;
        SET @OppFollowUpId = SCOPE_IDENTITY();

        COMMIT TRAN;

        SELECT
        1 AS Status,
        'Follow-up Scheduled Successfully.' AS Message,
        @OppFollowUpId AS FollowUpId,
        @NewOppActivityId AS ActivityId;

        END TRY

        BEGIN CATCH
        IF @@TRANCOUNT>0
            ROLLBACK TRAN;

        SELECT
        0 AS Status,
        ERROR_MESSAGE() AS Message;

        END CATCH
    END

ELSE IF(@Status='ADD_OPPORTUNITY_REQUIREMENT')
BEGIN
    BEGIN TRY
        BEGIN TRAN

        IF NOT EXISTS(SELECT 1 FROM CRM_Opportunity WHERE OpportunityId=@Ref_OpportunityId)
        BEGIN
            RAISERROR('Opportunity not found.',16,1);
        END

        IF EXISTS(
            SELECT 1
            FROM CRM_Opportunity O
            INNER JOIN CRM_OpportunityStage OS ON OS.OpportunityStageId=O.Ref_OpportunityStageId
            WHERE O.OpportunityId=@Ref_OpportunityId
            AND OS.IsClosed=1
        )
        BEGIN
            RAISERROR('This opportunity is closed and cannot be modified.',16,1);
        END

        IF(@Ref_ProductCategoryId IS NULL)
        BEGIN
            RAISERROR('Product category is required.',16,1);
        END

        IF(@Ref_OfferingId IS NULL)
        BEGIN
            RAISERROR('Offering is required.',16,1);
        END

        IF(@Ref_ProductId IS NULL)
        BEGIN
            RAISERROR('Product is required.',16,1);
        END

        IF NOT EXISTS(
            SELECT 1
            FROM MST_ProductCategory
            WHERE ProductCategoryId=@Ref_ProductCategoryId
            AND IsActive=1
        )
        BEGIN
            RAISERROR('Invalid product category.',16,1);
        END

        IF NOT EXISTS(
            SELECT 1
            FROM MST_Offering
            WHERE ProductId=@Ref_OfferingId
            AND Ref_ProductCategoryId=@Ref_ProductCategoryId
            AND IsActive=1
        )
        BEGIN
            RAISERROR('Selected offering does not belong to the selected category.',16,1);
        END

        IF NOT EXISTS(
            SELECT 1
            FROM MST_Product
            WHERE ProductId=@Ref_ProductId
            AND RefOfferingId=@Ref_OfferingId
            AND IsActive=1
        )
        BEGIN
            RAISERROR('Selected product does not belong to the selected offering.',16,1);
        END

        IF(@Quantity IS NULL OR @Quantity<=0)
        BEGIN
            RAISERROR('Quantity must be greater than zero.',16,1);
        END

        IF(@Ref_UnitId IS NULL)
        BEGIN
            RAISERROR('Unit is required.',16,1);
        END

        IF EXISTS(
            SELECT 1
            FROM CRM_OpportunityRequirement
            WHERE Ref_OpportunityId=@Ref_OpportunityId
            AND Ref_ProductId=@Ref_ProductId
            AND IsActive=1
            AND IsDeleted=0
        )
        BEGIN
            RAISERROR('This product is already added to the opportunity.',16,1);
        END

        INSERT INTO CRM_OpportunityRequirement
        (
            Ref_OpportunityId,
            Ref_LeadRequirementId,
            Ref_ProductCategoryId,
            Ref_OfferingId,
            Ref_ProductId,
            Quantity,
            RequirementRemark,
            IsActive,
            Ref_UnitId,
            CreatedOn,
            CreatedBy
        )
        VALUES
        (
            @Ref_OpportunityId,
            NULL,
            @Ref_ProductCategoryId,
            @Ref_OfferingId,
            @Ref_ProductId,
            @Quantity,
            @RequirementRemark,
            1,
            @Ref_UnitId,
            GETDATE(),
            @CreatedBy
        );

        COMMIT TRAN;

        SELECT
            1 AS Status,
            'Requirement Added Successfully.' AS Message,
            SCOPE_IDENTITY() AS OpportunityRequirementId;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK TRAN;
        SELECT 0 AS Status,ERROR_MESSAGE() AS Message;
    END CATCH
END

ELSE IF(@Status='UPDATE_OPPORTUNITY_REQUIREMENT')
BEGIN
    BEGIN TRY
        BEGIN TRAN

        IF NOT EXISTS(SELECT 1 FROM CRM_Opportunity WHERE OpportunityId=@Ref_OpportunityId)
        BEGIN
            RAISERROR('Opportunity not found.',16,1);
        END

        IF EXISTS(
            SELECT 1
            FROM CRM_Opportunity O
            INNER JOIN CRM_OpportunityStage OS ON OS.OpportunityStageId=O.Ref_OpportunityStageId
            WHERE O.OpportunityId=@Ref_OpportunityId
            AND OS.IsClosed=1
        )
        BEGIN
            RAISERROR('This opportunity is closed and cannot be modified.',16,1);
        END

        IF(@Quantity IS NULL OR @Quantity<=0)
        BEGIN
            RAISERROR('Quantity must be greater than zero.',16,1);
        END

        IF(@Ref_OpportunityRequirementId IS NOT NULL)
        BEGIN
            IF NOT EXISTS(
                SELECT 1
                FROM CRM_OpportunityRequirement
                WHERE OpportunityRequirementId=@Ref_OpportunityRequirementId
                AND Ref_OpportunityId=@Ref_OpportunityId
                AND IsActive=1
                AND IsDeleted=0
            )
            BEGIN
                RAISERROR('Requirement not found.',16,1);
            END

            IF(@Ref_ProductCategoryId IS NULL OR @Ref_OfferingId IS NULL OR @Ref_ProductId IS NULL)
            BEGIN
                RAISERROR('Category, Offering and Product are required.',16,1);
            END

            IF NOT EXISTS(
                SELECT 1
                FROM MST_Offering
                WHERE ProductId=@Ref_OfferingId
                AND Ref_ProductCategoryId=@Ref_ProductCategoryId
                AND IsActive=1
            )
            BEGIN
                RAISERROR('Selected offering does not belong to the selected category.',16,1);
            END

            IF NOT EXISTS(
                SELECT 1
                FROM MST_Product
                WHERE ProductId=@Ref_ProductId
                AND RefOfferingId=@Ref_OfferingId
                AND IsActive=1
            )
            BEGIN
                RAISERROR('Selected product does not belong to the selected offering.',16,1);
            END

            UPDATE CRM_OpportunityRequirement
            SET
                Ref_ProductCategoryId=@Ref_ProductCategoryId,
                Ref_OfferingId=@Ref_OfferingId,
                Ref_ProductId=@Ref_ProductId,
                Quantity=@Quantity,
                RequirementRemark=ISNULL(@RequirementRemark,RequirementRemark),
                Ref_UnitId=ISNULL(@Ref_UnitId,Ref_UnitId),
                UpdatedOn=GETDATE(),
                UpdatedBy=@CreatedBy
            WHERE OpportunityRequirementId=@Ref_OpportunityRequirementId
            AND Ref_OpportunityId=@Ref_OpportunityId
            AND IsDeleted=0;
        END

        ELSE IF(@Ref_LeadRequirementId IS NOT NULL)
        BEGIN
            IF NOT EXISTS(
                SELECT 1
                FROM CRM_LeadRequirement
                WHERE LeadRequirementId=@Ref_LeadRequirementId
                AND IsActive=1
                AND IsDeleted=0
            )
            BEGIN
                RAISERROR('Lead requirement not found.',16,1);
            END

            IF EXISTS(
                SELECT 1
                FROM CRM_OpportunityRequirement
                WHERE Ref_OpportunityId=@Ref_OpportunityId
                AND Ref_LeadRequirementId=@Ref_LeadRequirementId
                AND IsActive=1
                AND IsDeleted=0
            )
            BEGIN
                UPDATE ORQ
                SET
                    Ref_ProductCategoryId=LR.Ref_ProductCategoryId,
                    Ref_OfferingId=LR.Ref_OfferingId,
                    Ref_ProductId=LR.Ref_ProductId,
                    Quantity=@Quantity,
                    RequirementRemark=ISNULL(@RequirementRemark,ORQ.RequirementRemark),
                    Ref_UnitId=ISNULL(@Ref_UnitId,ORQ.Ref_UnitId),
                    UpdatedOn=GETDATE(),
                    UpdatedBy=@CreatedBy
                FROM CRM_OpportunityRequirement ORQ
                INNER JOIN CRM_LeadRequirement LR ON LR.LeadRequirementId=ORQ.Ref_LeadRequirementId
                WHERE ORQ.Ref_OpportunityId=@Ref_OpportunityId
                AND ORQ.Ref_LeadRequirementId=@Ref_LeadRequirementId
                AND ORQ.IsDeleted=0;
            END
            ELSE
            BEGIN
                INSERT INTO CRM_OpportunityRequirement
                (
                    Ref_OpportunityId,
                    Ref_LeadRequirementId,
                    Ref_ProductCategoryId,
                    Ref_OfferingId,
                    Ref_ProductId,
                    Quantity,
                    RequirementRemark,
                    IsActive,
                    Ref_UnitId,
                    CreatedOn,
                    CreatedBy
                )
                SELECT
                    @Ref_OpportunityId,
                    LR.LeadRequirementId,
                    LR.Ref_ProductCategoryId,
                    LR.Ref_OfferingId,
                    LR.Ref_ProductId,
                    @Quantity,
                    ISNULL(@RequirementRemark,LR.RequirementRemark),
                    1,
                    ISNULL(@Ref_UnitId,LR.Ref_UnitId),
                    GETDATE(),
                    @CreatedBy
                FROM CRM_LeadRequirement LR
                WHERE LR.LeadRequirementId=@Ref_LeadRequirementId;
            END
        END
        ELSE
        BEGIN
            RAISERROR('Either OpportunityRequirementId or LeadRequirementId must be provided.',16,1);
        END

        COMMIT TRAN;

        SELECT 1 AS Status,'Requirement Updated Successfully.' AS Message;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK TRAN;
        SELECT 0 AS Status,ERROR_MESSAGE() AS Message;
    END CATCH
END

ELSE IF(@Status='DELETE_OPPORTUNITY_REQUIREMENT')
BEGIN
    BEGIN TRY
        BEGIN TRAN;

        IF(@Ref_OpportunityRequirementId IS NULL)
            RAISERROR('Opportunity Requirement is required.',16,1);

        IF NOT EXISTS(
            SELECT 1
            FROM CRM_OpportunityRequirement
            WHERE OpportunityRequirementId=@Ref_OpportunityRequirementId
            AND IsActive=1
            AND IsDeleted=0
        )
            RAISERROR('Opportunity Requirement not found.',16,1);

        IF EXISTS(
            SELECT 1
            FROM CRM_Opportunity O
            INNER JOIN CRM_OpportunityRequirement ORQ ON ORQ.Ref_OpportunityId=O.OpportunityId
            INNER JOIN CRM_OpportunityStage OS ON OS.OpportunityStageId=O.Ref_OpportunityStageId
            WHERE ORQ.OpportunityRequirementId=@Ref_OpportunityRequirementId
            AND OS.IsClosed=1
        )
            RAISERROR('This opportunity is closed and cannot be modified.',16,1);

        -- Ticket already created
        IF EXISTS(
            SELECT 1
            FROM CRM_Ticket
            WHERE Ref_OpportunityRequirementId=@Ref_OpportunityRequirementId
        )
            RAISERROR('This requirement has a ticket and cannot be deleted.',16,1);

        -- Already used in quotation
        IF EXISTS(
            SELECT 1
            FROM CRM_OpportunityQuotationItem
            WHERE Ref_OpportunityRequirementId=@Ref_OpportunityRequirementId
        )
            RAISERROR('This requirement has already been used in a quotation and cannot be deleted.',16,1);

        IF(@DeleteRemark IS NULL OR LTRIM(RTRIM(@DeleteRemark))='')
            RAISERROR('Delete reason is required.',16,1);

        UPDATE CRM_OpportunityRequirement
        SET
            IsActive=0,
            IsDeleted=1,
            DeleteRemark=@DeleteRemark,
            DeletedOn=GETDATE(),
            DeletedBy=@CreatedBy,
            UpdatedOn=GETDATE(),
            UpdatedBy=@CreatedBy
        WHERE OpportunityRequirementId=@Ref_OpportunityRequirementId;

        COMMIT TRAN;

        SELECT 1 AS Status,'Requirement deleted successfully.' AS Message;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK TRAN;
        SELECT 0 AS Status,ERROR_MESSAGE() AS Message;
    END CATCH
END

ELSE IF(@Status='UPDATE_OPPORTUNITY_FOLLOWUP_STATUS')
    BEGIN
        BEGIN TRY
        BEGIN TRAN

        IF(@FollowUpId IS NULL)
        BEGIN
            RAISERROR('Follow-up Id is required.',16,1);
        END

        IF NOT EXISTS (SELECT 1 FROM CRM_OpportunityFollowUp WHERE OpportunityFollowUpId = @FollowUpId)
        BEGIN
            RAISERROR('Follow-up not found.',16,1);
        END

        DECLARE @OFU_OpportunityId INT;
        DECLARE @OFU_LinkedActivityId INT;

        SELECT
            @OFU_OpportunityId = Ref_OpportunityId,
            @OFU_LinkedActivityId = Ref_OpportunityActivityId
        FROM CRM_OpportunityFollowUp
        WHERE OpportunityFollowUpId = @FollowUpId;

        IF(@OFU_OpportunityId <> @Ref_OpportunityId)
        BEGIN
            RAISERROR('Invalid Opportunity for the selected Follow-up.',16,1);
        END

        IF EXISTS (SELECT 1 FROM CRM_Opportunity O INNER JOIN CRM_OpportunityStage OS ON OS.OpportunityStageId = O.Ref_OpportunityStageId WHERE O.OpportunityId = @OFU_OpportunityId AND OS.IsClosed = 1)
        BEGIN
            RAISERROR('This opportunity is closed (Qualified/Disqualified) and cannot be modified further.',16,1);
        END

        IF(@Ref_ActivityStatusId IS NULL)
        BEGIN
            RAISERROR('Status is required.',16,1);
        END

        IF NOT EXISTS(SELECT 1 FROM CRM_LeadActivityStatus WHERE ActivityStatusId = @Ref_ActivityStatusId AND ActivityStatusCode IN ('SCH','COM','CAN','RES','MIS','NA') AND IsActive = 1)
        BEGIN
            RAISERROR('Invalid status for a follow-up.',16,1);
        END

        DECLARE @OFU_StatusCode VARCHAR(10);
        DECLARE @OFU_CurrentStageId INT;
        DECLARE @OFU_OpportunityAssignmentId INT;

        SELECT @OFU_CurrentStageId = Ref_OpportunityStageId
        FROM CRM_Opportunity
        WHERE OpportunityId = @OFU_OpportunityId;

        SELECT @OFU_OpportunityAssignmentId = OpportunityAssignmentId
        FROM CRM_OpportunityAssignment
        WHERE Ref_OpportunityId = @OFU_OpportunityId AND IsCurrent = 1;

        SELECT @OFU_StatusCode = ActivityStatusCode
        FROM CRM_LeadActivityStatus
        WHERE ActivityStatusId = @Ref_ActivityStatusId;

        -- Update FollowUp
        UPDATE CRM_OpportunityFollowUp
        SET
            Ref_ActivityStatusId = @Ref_ActivityStatusId,
            ScheduledOn = ISNULL(@NewScheduledOn, ScheduledOn),
            IsCompleted = CASE WHEN @OFU_StatusCode IN ('COM','CAN','MIS') THEN 1 ELSE 0 END,
            CompletedOn = CASE WHEN @OFU_StatusCode IN ('COM','CAN','MIS') THEN GETDATE() ELSE NULL END,
            Ref_CompletedByEmployeeId = CASE WHEN @OFU_StatusCode IN ('COM','CAN','MIS') THEN @CreatedBy ELSE NULL END,
            UpdatedOn = GETDATE(),
            UpdatedBy = @CreatedBy
        WHERE OpportunityFollowUpId = @FollowUpId;

        -- Insert Status History
        INSERT INTO CRM_OpportunityFollowUpStatusHistory
        (Ref_OpportunityFollowUpId, Ref_ActivityStatusId, Remark, ChangedOn, ChangedBy)
        VALUES
        (@FollowUpId, @Ref_ActivityStatusId, @StatusRemark, GETDATE(), @CreatedBy);

        DECLARE @OFU_StatusName NVARCHAR(100);
        SELECT @OFU_StatusName = ActivityStatusName
        FROM CRM_LeadActivityStatus
        WHERE ActivityStatusId = @Ref_ActivityStatusId;

        -------------------------------------------------------------
        -- Update linked Activity status
        -------------------------------------------------------------
        IF(@OFU_LinkedActivityId IS NOT NULL)
        BEGIN
            UPDATE CRM_OpportunityActivity
            SET
                Ref_ActivityStatusId = @Ref_ActivityStatusId,
                NextFollowUpDate = ISNULL(@NewScheduledOn, NextFollowUpDate),
                UpdatedOn = GETDATE(),
                UpdatedBy = @CreatedBy
            WHERE ActivityId = @OFU_LinkedActivityId;
        END

        -------------------------------------------------------------
        -- Insert NEW Activity row for status change timeline
        -- (new to Opportunity stage, not carried from Lead)
        -------------------------------------------------------------
        INSERT INTO CRM_OpportunityActivity
        (
            Ref_OpportunityId, Ref_OpportunityAssignmentId, Ref_ActivityTypeId, Ref_EmployeeId,
            ActivityDateTime, Subject, Description, NextFollowUpDate,
            Ref_OpportunityStageId, Ref_ActivityStatusId,
            Ref_PreviousOpportunityStageId, Ref_NewOpportunityStageId,
            Ref_SourceLeadActivityId, IsActive, CreatedOn, CreatedBy
        )
        VALUES
        (
            @OFU_OpportunityId, @OFU_OpportunityAssignmentId, 12, -- Status Change
            @CreatedBy, GETDATE(), 'Follow-up status changed to ' + @OFU_StatusName,
            ISNULL(@StatusRemark, 'Status updated to ' + @OFU_StatusName),
            @NewScheduledOn, @OFU_CurrentStageId, @Ref_ActivityStatusId,
            @OFU_CurrentStageId, @OFU_CurrentStageId,
            NULL, 1, GETDATE(), @CreatedBy
        );

        COMMIT TRAN;

        SELECT
            1 AS Status, 'Follow-up Status Updated Successfully.' AS Message,
            @FollowUpId AS FollowUpId;

        END TRY

        BEGIN CATCH
            IF @@TRANCOUNT > 0 ROLLBACK TRAN;
            SELECT 0 AS Status, ERROR_MESSAGE() AS Message;
        END CATCH
    END

ELSE IF(@Status = 'GET_OPPORTUNITY_CLOSE_STAGES')
    BEGIN
        SELECT OpportunityStageId AS Id,StageName AS Name,IsWon,IsLost
        FROM CRM_OpportunityStage
        WHERE IsActive = 1
        AND IsClosed = 1
        ORDER BY StageOrder;
    END

ELSE IF(@Status='CLOSE_OPPORTUNITY')
    BEGIN
        BEGIN TRY
        BEGIN TRAN

        IF(@Ref_OpportunityId IS NULL)
        BEGIN
            RAISERROR('Opportunity is required.',16,1);
        END

        IF NOT EXISTS (SELECT 1 FROM CRM_Opportunity WHERE OpportunityId = @Ref_OpportunityId)
        BEGIN
            RAISERROR('Opportunity not found.',16,1);
        END

        IF(@Ref_OpportunityStageIdClose IS NULL OR NOT EXISTS (SELECT 1 FROM CRM_OpportunityStage WHERE OpportunityStageId = @Ref_OpportunityStageIdClose AND IsClosed = 1 AND IsActive = 1))
        BEGIN
            RAISERROR('Final stage must be Closed Won or Closed Lost.',16,1);
        END

        DECLARE @IsClosingAsWon BIT = (SELECT IsWon FROM CRM_OpportunityStage WHERE OpportunityStageId = @Ref_OpportunityStageIdClose);
        DECLARE @IsClosingAsLost BIT = (SELECT IsLost FROM CRM_OpportunityStage WHERE OpportunityStageId = @Ref_OpportunityStageIdClose);

        -- Lost requires a remark; Won does not
        IF(@IsClosingAsLost = 1 AND (@ClosureRemark IS NULL OR LTRIM(RTRIM(@ClosureRemark)) = ''))
        BEGIN
            RAISERROR('Please provide a reason for marking this opportunity as Lost.',16,1);
        END

        -- Cannot close while the current quotation is still pending internal Management Review
        IF EXISTS (
            SELECT 1
            FROM CRM_OpportunityQuotation Q
            INNER JOIN CRM_OpportunityManagementReview MR ON MR.Ref_OpportunityQuotationId = Q.OpportunityQuotationId
            WHERE Q.Ref_OpportunityId = @Ref_OpportunityId
              AND Q.IsCurrent = 1
              AND Q.IsActive = 1
              AND MR.DecisionCode = 'PENDING'
        )
        BEGIN
            RAISERROR('This opportunity has a quotation pending Management Review. Please complete the review before closing.',16,1);
        END

        -------------------------------------------------------------
        -- Cannot close opportunity while any ticket is still pending
        -------------------------------------------------------------
        --IF EXISTS
        --(
        --    SELECT 1
        --    FROM CRM_Ticket T
        --    INNER JOIN CRM_TicketStatus TS
        --        ON TS.TicketStatusId=T.Ref_TicketStatusId
        --    WHERE T.Ref_OpportunityId=@Ref_OpportunityId
        --    AND T.IsActive=1
        --    AND TS.IsClosed=0
        --)
        --BEGIN
        --    RAISERROR('This opportunity cannot be closed because one or more tickets are still pending. Please complete or cancel all tickets before closing the opportunity.',16,1);
        --END

        -------------------------------------------------------------
        -- Every active requirement must have an active ticket
        -------------------------------------------------------------
        IF EXISTS
        (
            SELECT 1
            FROM CRM_OpportunityRequirement ORQ
            WHERE ORQ.Ref_OpportunityId=@Ref_OpportunityId
            AND ORQ.IsActive=1
            AND ORQ.IsDeleted=0
            AND NOT EXISTS
            (
                SELECT 1
                FROM CRM_Ticket T
                WHERE T.Ref_OpportunityId=@Ref_OpportunityId
                AND T.Ref_OpportunityRequirementId=ORQ.OpportunityRequirementId
                AND T.IsActive=1
            )
        )
        BEGIN
            RAISERROR('This opportunity cannot be closed because one or more requirements do not have a ticket. Please raise a ticket for every requirement before closing the opportunity.',16,1);
        END

        -------------------------------------------------------------
        -- Cannot close opportunity while any ticket is still pending
        -------------------------------------------------------------
        IF EXISTS
        (
            SELECT 1
            FROM CRM_Ticket T
            INNER JOIN CRM_TicketStatus TS
                ON TS.TicketStatusId=T.Ref_TicketStatusId
            WHERE T.Ref_OpportunityId=@Ref_OpportunityId
            AND T.IsActive=1
            AND TS.IsClosed=0
        )
        BEGIN
            RAISERROR('This opportunity cannot be closed because one or more tickets are still pending. Please complete or cancel all tickets before closing the opportunity.',16,1);
        END
        -------------------------------------------------------------
        -- Validation: At least one activity must exist before closing
        -------------------------------------------------------------
        IF NOT EXISTS (SELECT 1 FROM CRM_OpportunityActivity WHERE Ref_OpportunityId = @Ref_OpportunityId)
        BEGIN
            RAISERROR('At least one activity must be created before closing the opportunity.',16,1);
        END

        DECLARE @CO_CurrentStageId INT;
        DECLARE @CO_OpportunityAssignmentId INT;
        DECLARE @CO_CurrentStageName NVARCHAR(100);

        SELECT @CO_CurrentStageId = Ref_OpportunityStageId
        FROM CRM_Opportunity WHERE OpportunityId = @Ref_OpportunityId;

        SELECT @CO_CurrentStageName = StageName
        FROM CRM_OpportunityStage WHERE OpportunityStageId = @CO_CurrentStageId;

        SELECT @CO_OpportunityAssignmentId = OpportunityAssignmentId
        FROM CRM_OpportunityAssignment WHERE Ref_OpportunityId = @Ref_OpportunityId AND IsCurrent = 1;

        -- Check if already closed
        IF EXISTS (SELECT 1 FROM CRM_OpportunityStage WHERE OpportunityStageId = @CO_CurrentStageId AND IsClosed = 1)
        BEGIN
            RAISERROR('This opportunity is already closed (Current Stage: %s).',16,1, @CO_CurrentStageName);
        END

        -------------------------------------------------------------
        -- Update the Opportunity stage
        -------------------------------------------------------------
        UPDATE CRM_Opportunity
        SET
            Ref_OpportunityStageId = @Ref_OpportunityStageIdClose,
            UpdatedOn = GETDATE(),
            UpdatedBy = @CreatedBy
        WHERE OpportunityId = @Ref_OpportunityId;

        DECLARE @CO_NewStageName NVARCHAR(100);
        SELECT @CO_NewStageName = StageName FROM CRM_OpportunityStage WHERE OpportunityStageId = @Ref_OpportunityStageIdClose;

        INSERT INTO CRM_OpportunityStatusHistory
        (
            Ref_OpportunityId,Ref_OpportunityStageId,Ref_ChangedByEmployeeId,StatusRemark,
            ChangedOn,CreatedOn,CreatedBy,Ref_PreviousOpportunityStageId
        )
        VALUES
        (
            @Ref_OpportunityId,@Ref_OpportunityStageIdClose,@CreatedBy,
            ISNULL(@ClosureRemark, 'Opportunity closed'),
            GETDATE(),GETDATE(),@CreatedBy,@CO_CurrentStageId
        );

        INSERT INTO CRM_OpportunityActivity
        (
            Ref_OpportunityId,Ref_OpportunityAssignmentId,Ref_ActivityTypeId,Ref_EmployeeId,ActivityDateTime,
            Subject,Description,Ref_OpportunityStageId,Ref_ActivityStatusId,Ref_PreviousOpportunityStageId,
            Ref_NewOpportunityStageId,Ref_SourceLeadActivityId,IsActive,CreatedOn,CreatedBy
        )
        VALUES
        (
            @Ref_OpportunityId,@CO_OpportunityAssignmentId,
            12, @CreatedBy,GETDATE(),'Opportunity Closed: ' + @CO_NewStageName,
            ISNULL(@ClosureRemark, 'Opportunity marked as ' + @CO_NewStageName),
            @Ref_OpportunityStageIdClose,3, @CO_CurrentStageId,@Ref_OpportunityStageIdClose,NULL,1,GETDATE(),@CreatedBy
        );

        -------------------------------------------------------------
        -- If Closed Lost, cancel any pending follow-ups/activities
        -------------------------------------------------------------
        IF(@IsClosingAsLost = 1)
        BEGIN
            UPDATE CRM_OpportunityFollowUp
            SET
                Ref_ActivityStatusId = 4,
                IsCompleted = 1,
                CompletedOn = GETDATE(),
                Ref_CompletedByEmployeeId = @CreatedBy,
                UpdatedOn = GETDATE(),
                UpdatedBy = @CreatedBy
            WHERE Ref_OpportunityId = @Ref_OpportunityId
            AND IsCompleted = 0;

            UPDATE CRM_OpportunityActivity
            SET
                Ref_ActivityStatusId = 4,
                UpdatedOn = GETDATE(),
                UpdatedBy = @CreatedBy
            WHERE Ref_OpportunityId = @Ref_OpportunityId
            AND Ref_ActivityStatusId IN (1, 2)
            AND IsActive = 1;
        END

        -------------------------------------------------------------
        -- If Closed Won: create the Customer master record now
        -------------------------------------------------------------
        DECLARE @CO_NewCustomerId INT = NULL;

        IF(@IsClosingAsWon = 1)
        BEGIN
            DECLARE @CO_LeadOrgId INT, @CO_CustomerName NVARCHAR(255), @CO_Address NVARCHAR(500),
                    @CO_CountryId INT, @CO_StateId INT, @CO_CityId INT,
                    @CO_PinCode NVARCHAR(20), @CO_Gst NVARCHAR(50), @CO_Pan NVARCHAR(50);

            SELECT
                @CO_LeadOrgId = LO.LeadOrganizationId,
                @CO_CustomerName = LO.CustomerName,
                @CO_Address = LO.Address,
                @CO_CountryId = LO.Ref_CountryId,
                @CO_StateId = LO.Ref_StateId,
                @CO_CityId = LO.Ref_CityId,
                @CO_PinCode = LO.PinCode,
                @CO_Gst = LO.Gst,
                @CO_Pan = LO.Pan
            FROM CRM_Opportunity O
            INNER JOIN CRM_Lead L ON L.LeadId = O.Ref_LeadId
            INNER JOIN CRM_LeadOrganization LO ON LO.LeadOrganizationId = L.Ref_LeadOrganizationId
            WHERE O.OpportunityId = @Ref_OpportunityId;

            -- Reuse an existing customer for this LeadOrganization if one already exists
            SELECT @CO_NewCustomerId = CustomerId
            FROM CUS_Customer
            WHERE Ref_LeadOrganizationId = @CO_LeadOrgId
              AND IsActive = 1;

            -- Same company already in the master (e.g. imported from WMS): same company key
            -- ('Macfos Ltd.' = 'MACFOS LIMITED-') or same GST -> reuse it and link the lead org.
            IF(@CO_NewCustomerId IS NULL)
            BEGIN
                DECLARE @CO_Key NVARCHAR(400) = dbo.fn_CRM_CompanyKey(@CO_CustomerName);
                SELECT TOP 1 @CO_NewCustomerId = CustomerId
                FROM CUS_Customer
                WHERE IsActive = 1
                  AND (dbo.fn_CRM_CompanyKey(CustomerName) = @CO_Key
                       OR (NULLIF(LTRIM(RTRIM(@CO_Gst)), '') IS NOT NULL AND LTRIM(RTRIM(Gst)) = LTRIM(RTRIM(@CO_Gst))))
                ORDER BY CustomerId;

                IF(@CO_NewCustomerId IS NOT NULL)
                    UPDATE CUS_Customer
                    SET Ref_LeadOrganizationId = @CO_LeadOrgId, UpdatedOn = GETDATE(), UpdatedBy = @CreatedBy
                    WHERE CustomerId = @CO_NewCustomerId
                      AND Ref_LeadOrganizationId IS NULL;
            END

            IF(@CO_NewCustomerId IS NULL)
            BEGIN
                DECLARE @CO_CustomerCode NVARCHAR(50);
                SELECT @CO_CustomerCode = 'CUST' + RIGHT('0000' + CAST(ISNULL(MAX(CustomerId), 0) + 1 AS VARCHAR(10)), 5)
                FROM CUS_Customer;

                INSERT INTO CUS_Customer
                (
                    CustomerCode, CustomerName, Address, Ref_CountryId, Ref_StateId, Ref_CityId,
                    PinCode, Gst, Pan, Ref_LeadOrganizationId, IsActive, CreatedOn, CreatedBy
                )
                VALUES
                (
                    @CO_CustomerCode, @CO_CustomerName, @CO_Address, @CO_CountryId, @CO_StateId, @CO_CityId,
                    @CO_PinCode, @CO_Gst, @CO_Pan, @CO_LeadOrgId, 1, GETDATE(), @CreatedBy
                );

                SET @CO_NewCustomerId = SCOPE_IDENTITY();

            END

            -- Lead contacts -> customer contacts, for new AND reused customers (e.g. a WMS customer
            -- imported without contacts). Skip contacts the customer already has (same mobile, same
            -- email, or same name when both are blank). Primary only when the customer has none.
            DECLARE @CO_HasPrimary BIT = CASE WHEN EXISTS (SELECT 1 FROM CUS_CustomerContact
                                                           WHERE Ref_CustomerId = @CO_NewCustomerId
                                                             AND IsActive = 1 AND IsPrimary = 1) THEN 1 ELSE 0 END;

            DECLARE @CO_NewContacts TABLE (LeadContactId INT PRIMARY KEY);
            INSERT INTO @CO_NewContacts (LeadContactId)
            SELECT LC.LeadContactId
            FROM CRM_LeadContact LC
            WHERE LC.Ref_LeadOrganizationId = @CO_LeadOrgId
              AND NULLIF(LTRIM(RTRIM(LC.ContactPerson)), '') IS NOT NULL
              AND NOT EXISTS (SELECT 1 FROM CUS_CustomerContact CC
                              WHERE CC.Ref_CustomerId = @CO_NewCustomerId AND CC.IsActive = 1
                                AND (   (NULLIF(LTRIM(RTRIM(LC.Mobile)), '') IS NOT NULL AND LTRIM(RTRIM(CC.Mobile)) = LTRIM(RTRIM(LC.Mobile)))
                                     OR (NULLIF(LTRIM(RTRIM(LC.Email)), '')  IS NOT NULL AND LTRIM(RTRIM(CC.Email))  = LTRIM(RTRIM(LC.Email)))
                                     OR (NULLIF(LTRIM(RTRIM(LC.Mobile)), '') IS NULL AND NULLIF(LTRIM(RTRIM(LC.Email)), '') IS NULL
                                         AND LTRIM(RTRIM(CC.ContactPerson)) = LTRIM(RTRIM(LC.ContactPerson)))));

            DECLARE @CO_PrimaryContactId INT = CASE WHEN @CO_HasPrimary = 1 THEN NULL
                                                    ELSE (SELECT MIN(LeadContactId) FROM @CO_NewContacts) END;

            INSERT INTO CUS_CustomerContact
            (
                Ref_CustomerId, ContactPerson, Mobile, Email, Ref_DesignationId,
                IsPrimary, IsActive, CreatedOn, CreatedBy
            )
            SELECT
                @CO_NewCustomerId, LC.ContactPerson, LC.Mobile, LC.Email, LC.Ref_DesignationId,
                CASE WHEN LC.LeadContactId = @CO_PrimaryContactId THEN 1 ELSE 0 END,
                1, GETDATE(), @CreatedBy
            FROM CRM_LeadContact LC
            JOIN @CO_NewContacts N ON N.LeadContactId = LC.LeadContactId;

            -- Keep WMS in sync: create (or link) this customer in WMS. Any error rolls back the close.
            EXEC dbo.USP_CRM_CustomerManagement @Status = 'PUSH_TO_WMS', @CustomerId = @CO_NewCustomerId, @CreatedBy = @CreatedBy;

            UPDATE CRM_Opportunity
            SET Ref_CustomerId = @CO_NewCustomerId
            WHERE OpportunityId = @Ref_OpportunityId;
        END

        COMMIT TRAN;

        SELECT
            1 AS Status,
            'Opportunity Closed Successfully.' AS Message,
            @Ref_OpportunityId AS OpportunityId,
            @Ref_OpportunityStageIdClose AS OpportunityStageId,
            @CO_NewStageName AS StageName,
            @CO_NewCustomerId AS CustomerId;

        END TRY

        BEGIN CATCH
            IF @@TRANCOUNT > 0 ROLLBACK TRAN;
            SELECT 0 AS Status,ERROR_MESSAGE() AS Message;
        END CATCH
    END

ELSE IF(@Status = 'ADD_ACTIVITY_EXPENSE')
BEGIN
    BEGIN TRY
        BEGIN TRAN;

        -- 1. Validate Opportunity

        IF(@Ref_OpportunityId IS NULL)
        BEGIN
            RAISERROR('Opportunity is required.',16,1);
        END

        IF NOT EXISTS
        (
            SELECT 1 FROM CRM_Opportunity WHERE OpportunityId = @Ref_OpportunityId AND IsActive = 1
        )
        BEGIN
            RAISERROR('Opportunity not found or inactive.',16,1);
        END

        -- 2. Do not allow expense on closed Opportunity
        IF EXISTS
        (
            SELECT 1 FROM CRM_Opportunity O
            INNER JOIN CRM_OpportunityStage OS ON OS.OpportunityStageId = O.Ref_OpportunityStageId
            WHERE O.OpportunityId = @Ref_OpportunityId AND OS.IsClosed = 1
        )
        BEGIN
            RAISERROR('This opportunity is closed and cannot be modified.',16,1);
        END

        -- 3. Validate Activity
        IF(@Ref_ActivityId IS NULL)
        BEGIN
            RAISERROR('Activity is required.',16,1);
        END

        IF NOT EXISTS
        (
            SELECT 1 FROM CRM_OpportunityActivity
            WHERE ActivityId = @Ref_ActivityId AND Ref_OpportunityId = @Ref_OpportunityId AND IsActive = 1
        )
        BEGIN
            RAISERROR('Activity not found for the selected opportunity.',16,1);
        END      

        -- 4. Expense JSON required
        IF(@ExpenseItems IS NULL OR LTRIM(RTRIM(@ExpenseItems)) = '')
        BEGIN
            RAISERROR('Expense items are required.',16,1);
        END

        -- 5. Parse Expense JSON
       DECLARE @ExpenseTable TABLE
        (
            ExpenseRowNo INT IDENTITY(1,1),

            Ref_ExpenseCategoryId INT,
            Ref_TransportModeId   INT NULL,
            ExpenseDate           DATETIME NULL,
            Amount                DECIMAL(18,2),
            Description            NVARCHAR(1000),
            FromLocation           NVARCHAR(500) NULL,
            ToLocation             NVARCHAR(500) NULL,

            AttachmentsJson        NVARCHAR(MAX) NULL
        );

       INSERT INTO @ExpenseTable
            (Ref_ExpenseCategoryId,Ref_TransportModeId,ExpenseDate,Amount,Description,FromLocation,ToLocation,AttachmentsJson)
            SELECT
                Ref_ExpenseCategoryId,
                NULLIF(Ref_TransportModeId, 0),
                ISNULL(ExpenseDate, GETDATE()),
                Amount,
                NULLIF(LTRIM(RTRIM(Description)), ''),
                NULLIF(LTRIM(RTRIM(FromLocation)), ''),
                NULLIF(LTRIM(RTRIM(ToLocation)), ''),
                AttachmentsJson
            FROM OPENJSON(@ExpenseItems)
            WITH
            (
                Ref_ExpenseCategoryId INT   '   $.Ref_ExpenseCategoryId',
                Ref_TransportModeId INT         '$.Ref_TransportModeId',
                ExpenseDate DATETIME            '$.ExpenseDate',
                Amount DECIMAL(18,2)            '$.Amount',
                Description NVARCHAR(1000)      '$.Description',
                FromLocation NVARCHAR(500)      '$.FromLocation',
                ToLocation NVARCHAR(500)        '$.ToLocation',
                AttachmentsJson NVARCHAR(MAX)   '$.Attachments' AS JSON
            );

        -- 6. At least one expense
        IF NOT EXISTS (SELECT 1 FROM @ExpenseTable)
        BEGIN
            RAISERROR('At least one expense is required.',16,1);
        END

        -- 7. Category required
        IF EXISTS(SELECT 1 FROM @ExpenseTable WHERE ISNULL(Ref_ExpenseCategoryId,0) = 0)
        BEGIN
            RAISERROR('Expense Category is required for every expense.',16,1);
        END

        -- 8. Validate Category     
        IF EXISTS( SELECT 1 FROM @ExpenseTable E
            WHERE NOT EXISTS
            (SELECT 1 FROM CRM_ActivityExpenseCategory C WHERE C.ExpenseCategoryId = E.Ref_ExpenseCategoryId AND C.IsActive = 1))
        BEGIN
            RAISERROR('One or more Expense Categories are invalid or inactive.',16,1);
        END

        -- 9. Validate Transport Mode requirement
        IF EXISTS
        (SELECT 1 FROM @ExpenseTable E
            INNER JOIN CRM_ActivityExpenseCategory C ON C.ExpenseCategoryId = E.Ref_ExpenseCategoryId
            WHERE C.RequiresTransportMode = 1 AND E.Ref_TransportModeId IS NULL
        )
        BEGIN
            RAISERROR('Transport Mode is required for the selected Expense Category.',16,1);
        END
      
        -- 10. Validate Transport Mode
        IF EXISTS (SELECT 1 FROM @ExpenseTable E WHERE E.Ref_TransportModeId IS NOT NULL
              AND NOT EXISTS ( SELECT 1 FROM CRM_ActivityTransportMode T WHERE T.TransportModeId = E.Ref_TransportModeId AND T.IsActive = 1))
        BEGIN
            RAISERROR('One or more Transport Modes are invalid or inactive.',16,1);
        END
     
        -- 11. From Location       
        IF EXISTS
        (SELECT 1 FROM @ExpenseTable E 
        INNER JOIN CRM_ActivityExpenseCategory C ON C.ExpenseCategoryId = E.Ref_ExpenseCategoryId
            WHERE C.RequiresFromLocation = 1
              AND (E.FromLocation IS NULL OR LTRIM(RTRIM(E.FromLocation)) = ''))
        BEGIN
            RAISERROR('From Location is required for the selected Expense Category.',16,1);
        END
  
        -- 12. To Location
        IF EXISTS
        (
            SELECT 1
            FROM @ExpenseTable E
            INNER JOIN CRM_ActivityExpenseCategory C
                ON C.ExpenseCategoryId = E.Ref_ExpenseCategoryId
            WHERE C.RequiresToLocation = 1
              AND (
                    E.ToLocation IS NULL
                    OR LTRIM(RTRIM(E.ToLocation)) = ''
                  )
        )
        BEGIN
            RAISERROR(
                'To Location is required for the selected Expense Category.',
                16,
                1
            );
        END
        
        -- 13. Amount     
        IF EXISTS
        (
            SELECT 1
            FROM @ExpenseTable
            WHERE Amount IS NULL
               OR Amount <= 0
        )
        BEGIN
            RAISERROR(
                'Expense Amount must be greater than zero.',
                16,
                1
            );
        END

     
        -- 14. Description    
        DECLARE @ExpenseLeadId INT;
        SELECT
            @ExpenseLeadId = Ref_LeadId
        FROM CRM_Opportunity
        WHERE OpportunityId = @Ref_OpportunityId
          AND IsActive = 1;

        IF @ExpenseLeadId IS NULL
        BEGIN
            RAISERROR(
                'Lead reference could not be found for this opportunity.',
                16,
                1
            );
        END
       
        -- 15. Insert Opportunity Expenses Capture generated ActivityExpenseId    
        DECLARE @ExpenseMap TABLE
        (
            ExpenseRowNo INT,
            ActivityExpenseId BIGINT
        );

        DECLARE
            @ExpenseRowNo INT,
            @ActivityExpenseId BIGINT;

        DECLARE ExpenseCursor CURSOR LOCAL FAST_FORWARD
        FOR
        SELECT ExpenseRowNo
        FROM @ExpenseTable
        ORDER BY ExpenseRowNo;

        OPEN ExpenseCursor;

        FETCH NEXT FROM ExpenseCursor
        INTO @ExpenseRowNo;

        WHILE @@FETCH_STATUS = 0
        BEGIN

            INSERT INTO CRM_ActivityExpense
            (Ref_LeadId,Ref_OpportunityId,Ref_LeadActivityId,Ref_OpportunityActivityId,Ref_ExpenseCategoryId,Ref_TransportModeId,ExpenseDate,Amount,Description,
                FromLocation,ToLocation,CreatedOn,CreatedBy,IsActive
            )
            SELECT
                @ExpenseLeadId,@Ref_OpportunityId,NULL,@Ref_ActivityId,E.Ref_ExpenseCategoryId,E.Ref_TransportModeId,E.ExpenseDate,E.Amount,
                E.Description,E.FromLocation,E.ToLocation,GETDATE(),@CreatedBy,1
            FROM @ExpenseTable E
            WHERE E.ExpenseRowNo = @ExpenseRowNo;

            SET @ActivityExpenseId =
                CONVERT(BIGINT, SCOPE_IDENTITY());

            INSERT INTO @ExpenseMap
            (
                ExpenseRowNo,
                ActivityExpenseId
            )
            VALUES
            (
                @ExpenseRowNo,
                @ActivityExpenseId
            );

            FETCH NEXT FROM ExpenseCursor
            INTO @ExpenseRowNo;

        END;

        CLOSE ExpenseCursor;
        DEALLOCATE ExpenseCursor; 
      
        -- 16. Insert Opportunity Expense Attachments    
        INSERT INTO CRM_OpportunityActivityAttachment
        (Ref_OpportunityActivityId,Ref_ActivityExpenseId,FileName,FilePath,FileType,FileSize,CreatedOn,CreatedBy)
        SELECT
            @Ref_ActivityId,EM.ActivityExpenseId,A.FileName,A.FilePath,A.FileType,A.FileSize,
            GETDATE(),@CreatedBy
        FROM @ExpenseTable E
        INNER JOIN @ExpenseMap EM ON EM.ExpenseRowNo = E.ExpenseRowNo
        CROSS APPLY OPENJSON(E.AttachmentsJson)
        WITH
        (
            FileName NVARCHAR(500)  '$.FileName',
            FilePath NVARCHAR(1000) '$.FilePath',
            FileType NVARCHAR(100)  '$.FileType',
            FileSize BIGINT         '$.FileSize'
        ) A
        WHERE E.AttachmentsJson IS NOT NULL
          AND ISJSON(E.AttachmentsJson) = 1;
      
        -- 16. Result       
        SELECT
            1 AS Status,
            'Expenses added successfully.' AS Message,
            @Ref_OpportunityId AS OpportunityId,
            @Ref_ActivityId AS ActivityId,
            COUNT(*) AS TotalItems,
            SUM(Amount) AS TotalAmount
        FROM @ExpenseTable;

        COMMIT TRAN;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        SELECT 0 AS Status, ERROR_MESSAGE() AS Message;
    END CATCH
END

END

GO
