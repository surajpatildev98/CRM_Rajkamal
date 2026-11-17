USE [Rkl_EnterpriseSuiteDB_Live]
GO
-- CRM Reports: card drill-down lists (RS5 leads, RS6 quotations) + lead To-date fix + opp grid LEFT JOIN lead.
-- ============================================================================
-- Author:      Suraj
-- Create date: 13-10-2026
-- Description: This stored procedure will handle Reports Related Operations
-- ============================================================================
ALTER PROCEDURE [dbo].[USP_CRM_Reports]
(
    @Status                     VARCHAR(50),
    @SearchText                 NVARCHAR(200) = NULL,
    @FromDate                   DATETIME = NULL,
    @ToDate                     DATETIME = NULL,
    @DateType                   VARCHAR(30) = 'CREATED',
    @EmployeeId                 INT = NULL,
    @CompanyId                  INT = NULL,
    @PriorityId                 INT = NULL,
    @ProductId                  INT = NULL,
    @LeadStatusId               INT = NULL,
    @LeadSourceId               INT = NULL,
    @OpportunityStageId         INT = NULL,
    @OpportunityTypeId          INT = NULL,
    @MinProbability             DECIMAL(5,2) = NULL,
    @MaxProbability             DECIMAL(5,2) = NULL,
    @ExpectedCloseFromDate      DATETIME = NULL,
    @ExpectedCloseToDate        DATETIME = NULL,
    @ActivityTypeId             INT = NULL,
    @ActivityStatusId           INT = NULL,
    @FollowUpStatusId           INT = NULL,
    @BusinessState              VARCHAR(30) = 'ALL',    
    @IsActive                   BIT = 1,
    @LeadId                     INT = NULL,
    @OpportunityId              INT = NULL,
    @LoggedInEmployeeId         INT = NULL,
    @CanViewAll                 BIT = 0,
    @ShowCreatedAndAssigned     BIT = 0
)
AS
BEGIN

    SET NOCOUNT ON;   

    SET @BusinessState=UPPER(LTRIM(RTRIM(ISNULL(@BusinessState,'ALL'))));
    SET @DateType=UPPER(LTRIM(RTRIM(ISNULL(@DateType,'CREATED'))));

    IF @BusinessState NOT IN('ALL','LEAD','QUALIFIED','OPPORTUNITY','QUOTE','NEGOTIATION','REVIEW','WON','LOST')
    BEGIN
    RAISERROR('Invalid BusinessState. Allowed values: ALL, LEAD, QUALIFIED, OPPORTUNITY, QUOTE, NEGOTIATION, REVIEW, WON, LOST.',16,1);
    RETURN;
    END;

    IF @DateType NOT IN('CREATED','EXPECTED_CLOSE','ACTIVITY','FOLLOWUP','ASSIGNMENT','STATUS')
    BEGIN
    RAISERROR('Invalid DateType. Allowed values: CREATED, EXPECTED_CLOSE, ACTIVITY, FOLLOWUP, ASSIGNMENT, STATUS.',16,1);
    RETURN;
    END;

    IF @FromDate IS NOT NULL AND @ToDate IS NOT NULL AND CONVERT(DATE,@FromDate)>CONVERT(DATE,@ToDate)
    BEGIN
    RAISERROR('FromDate cannot be greater than ToDate.',16,1);
    RETURN;
    END;

    IF @ExpectedCloseFromDate IS NOT NULL AND @ExpectedCloseToDate IS NOT NULL AND CONVERT(DATE,@ExpectedCloseFromDate)>CONVERT(DATE,@ExpectedCloseToDate)
    BEGIN
    RAISERROR('ExpectedCloseFromDate cannot be greater than ExpectedCloseToDate.',16,1);
    RETURN;
    END;

    IF @MinProbability IS NOT NULL AND (@MinProbability<0 OR @MinProbability>100)
    BEGIN
    RAISERROR('MinProbability must be between 0 and 100.',16,1);
    RETURN;
    END;

    IF @MaxProbability IS NOT NULL AND (@MaxProbability<0 OR @MaxProbability>100)
    BEGIN
    RAISERROR('MaxProbability must be between 0 and 100.',16,1);
    RETURN;
    END;

    IF @MinProbability IS NOT NULL AND @MaxProbability IS NOT NULL AND @MinProbability>@MaxProbability
    BEGIN
    RAISERROR('MinProbability cannot be greater than MaxProbability.',16,1);
    RETURN;
    END;

    DECLARE @FromDateStart DATETIME=NULL;
    DECLARE @ToDateExclusive DATETIME=NULL;
    DECLARE @ExpectedCloseFromDateStart DATETIME=NULL;
    DECLARE @ExpectedCloseToDateExclusive DATETIME=NULL;

    IF @FromDate IS NOT NULL
    BEGIN
    SET @FromDateStart=CONVERT(DATE,@FromDate);
    END;

    IF @ToDate IS NOT NULL
    BEGIN
    SET @ToDateExclusive=DATEADD(DAY,1,CONVERT(DATE,@ToDate));
    END;

    IF @ExpectedCloseFromDate IS NOT NULL
    BEGIN
    SET @ExpectedCloseFromDateStart=CONVERT(DATE,@ExpectedCloseFromDate);
    END;

    IF @ExpectedCloseToDate IS NOT NULL
    BEGIN
    SET @ExpectedCloseToDateExclusive=DATEADD(DAY,1,CONVERT(DATE,@ExpectedCloseToDate));
    END;

IF @Status = 'REPORT_FILTERS'
        BEGIN          
            -- RESULT SET 1 : EMPLOYEES   

            SELECT E.EmployeeId AS Id,E.EmployeeCode AS Code,
                NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(E.FirstName, ''),' ',ISNULL(E.LastName, '')))),'') AS Name
            FROM ORG_Employee E
            WHERE E.IsActive = 1
            ORDER BY E.FirstName,E.LastName;

            -- RESULT SET 2 : COMPANIES   
            SELECT LO.LeadOrganizationId AS Id,NULL AS Code,LO.CustomerName AS Name
            FROM CRM_LeadOrganization LO
            WHERE LO.CustomerName IS NOT NULL AND LTRIM(RTRIM(LO.CustomerName)) <> ''
            ORDER BY LO.CustomerName;

            -- RESULT SET 3 : PRIORITIES
            SELECT P.PriorityId AS Id,P.PriorityCode AS Code,P.PriorityName AS Name
            FROM MST_Priority P WHERE P.IsActive = 1
            ORDER BY P.PriorityLevel, P.PriorityName;
         
            -- RESULT SET 4 : PRODUCTS
           --SELECT P.ProductId AS Id,P.ProductCode AS Code,P.ProductName AS Name
           -- FROM MST_Offering P
           -- WHERE P.IsActive = 1
           -- ORDER BY P.ProductName;
            
            -- RESULT SET 4 : PRODUCTS
            SELECT P.ProductId AS Id,P.ItemCode AS Code,P.ItemName AS Name
            FROM MST_Product P
            WHERE P.IsActive=1
            ORDER BY P.ItemName;

            -- RESULT SET 5 : LEAD STATUS
            SELECT LS.LeadStatusId AS Id,LS.StatusCode AS Code,LS.StatusName AS Name
            FROM CRM_LeadStatus LS
            WHERE LS.IsActive = 1
            ORDER BY LS.StatusName;
            
            -- RESULT SET 6 : LEAD SOURCE
            SELECT LS.LeadSourceId AS Id,LS.SourceCode AS Code,LS.SourceName AS Name
            FROM CRM_LeadSource LS
            WHERE LS.IsActive = 1
            ORDER BY LS.SourceName;
                
            -- RESULT SET 7 : OPPORTUNITY STAGE
            SELECT OS.OpportunityStageId AS Id,OS.StageCode AS Code,OS.StageName AS Name
            FROM CRM_OpportunityStage OS
            WHERE OS.IsActive = 1
            ORDER BY OS.StageOrder;
          
            -- RESULT SET 8 : OPPORTUNITY TYPE
            SELECT OT.OpportunityTypeId AS Id,OT.OpportunityTypeCode AS Code,OT.OpportunityTypeName AS Name
            FROM CRM_OpportunityType OT
            WHERE OT.IsActive = 1
            ORDER BY OT.OpportunityTypeName;
          
            -- RESULT SET 9 : ACTIVITY TYPE
            SELECT AT.ActivityTypeId AS Id,AT.ActivityCode AS Code,AT.ActivityName AS Name
            FROM CRM_LeadActivityType AT
            WHERE AT.IsActive = 1
            ORDER BY ISNULL(AT.DisplayOrder, 9999),AT.ActivityName;
          
            -- RESULT SET 10 : ACTIVITY STATUS          
            SELECT S.ActivityStatusId AS Id,S.ActivityStatusCode AS Code,S.ActivityStatusName AS Name
            FROM CRM_LeadActivityStatus S
            WHERE S.IsActive = 1
            ORDER BY S.ActivityStatusName;

            -- RESULT SET 11 : FOLLOW-UP STATUS
            --
            -- Lead FollowUp and OpportunityFollowUp both use
            -- Ref_ActivityStatusId        

            SELECT S.ActivityStatusId AS Id,S.ActivityStatusCode AS Code, S.ActivityStatusName AS Name
            FROM CRM_LeadActivityStatus S
            WHERE S.IsActive = 1
            ORDER BY S.ActivityStatusName;

            RETURN;
        END;

    -- 1. DASHBOARD
ELSE IF @Status = 'CRM_REPORT_DASHBOARD'
    BEGIN

        IF @ToDate IS NOT NULL
        BEGIN
            SET @ToDateExclusive =DATEADD(DAY, 1, CONVERT(DATE, @ToDate));
        END;

        IF OBJECT_ID('tempdb..#ReportLeads') IS NOT NULL
            DROP TABLE #ReportLeads;

        SELECT L.LeadId,L.LeadNo,L.Ref_LeadOrganizationId,L.Ref_LeadSourceId,L.Ref_CurrentAssignedEmployeeId,L.Ref_LeadStatusId,L.ExpectedValue,L.CreatedOn,L.UpdatedOn,L.ClosedOn
        INTO #ReportLeads
        FROM CRM_Lead L
        WHERE 1 = 1
            AND
            (@BusinessState = 'ALL'
                OR
                (@BusinessState = 'LEAD'
                    AND NOT EXISTS(SELECT 1 FROM CRM_Opportunity O WHERE O.Ref_LeadId = L.LeadId AND O.IsActive = 1)
                )
                OR
                (@BusinessState = 'QUALIFIED'
                    AND EXISTS(SELECT 1 FROM CRM_LeadStatus LS WHERE LS.LeadStatusId = L.Ref_LeadStatusId AND LS.StatusCode = 'QUALIFIED')
                )
                OR
                @BusinessState IN
                ('OPPORTUNITY','QUOTE','NEGOTIATION','REVIEW','WON','LOST')
                AND EXISTS( SELECT 1 FROM CRM_Opportunity O  WHERE O.Ref_LeadId = L.LeadId AND O.IsActive = 1)
            )

        -- COMPANY        
        AND (@CompanyId IS NULL OR L.Ref_LeadOrganizationId = @CompanyId )      
        -- EMPLOYEE   
        AND (@EmployeeId IS NULL OR L.Ref_CurrentAssignedEmployeeId = @EmployeeId)      
        -- LEAD STATUS
        AND( @LeadStatusId IS NULL OR L.Ref_LeadStatusId = @LeadStatusId )
        -- LEAD SOURCE
        AND (@LeadSourceId IS NULL OR L.Ref_LeadSourceId = @LeadSourceId)
        AND ( @PriorityId IS NULL OR L.Ref_PriorityId = @PriorityId)      
        -- DATE  
        AND( @FromDate IS NULL
            OR ( @DateType = 'CREATED'  AND L.CreatedOn >= @FromDate AND (@ToDateExclusive IS NULL OR L.CreatedOn < @ToDateExclusive))
            OR ( @DateType = 'ACTIVITY' AND EXISTS ( SELECT 1 FROM CRM_LeadActivity A WHERE A.Ref_LeadId = L.LeadId AND A.IsActive = 1 AND A.ActivityDateTime >= @FromDate AND (@ToDateExclusive IS NULL OR A.ActivityDateTime < @ToDateExclusive)))
            OR ( @DateType = 'FOLLOWUP' AND EXISTS ( SELECT 1 FROM CRM_LeadFollowUp F WHERE  F.Ref_LeadId = L.LeadId AND F.ScheduledOn >= @FromDate
                        AND(@ToDateExclusive IS NULL OR F.ScheduledOn < @ToDateExclusive)))
            OR ( @DateType = 'ASSIGNMENT' AND EXISTS ( SELECT 1 FROM CRM_LeadAssignment LA WHERE LA.Ref_LeadId = L.LeadId AND LA.AssignedOn >= @FromDate
                        AND(@ToDateExclusive IS NULL OR LA.AssignedOn < @ToDateExclusive)))
            OR ( @DateType = 'STATUS' AND EXISTS ( SELECT 1 FROM CRM_LeadStatusHistory SH WHERE SH.Ref_LeadId = L.LeadId AND SH.ChangedOn >= @FromDate
                        AND ( @ToDateExclusive IS NULL OR SH.ChangedOn < @ToDateExclusive ))))
        --AND ( @ToDateExclusive IS NULL OR L.CreatedOn < @ToDateExclusive)
        AND ( @ProductId IS NULL OR EXISTS ( SELECT 1 FROM CRM_LeadRequirement LR  WHERE LR.Ref_LeadId = L.LeadId AND LR.IsActive = 1 AND LR.Ref_ProductId = @ProductId ))
        AND ( @ActivityTypeId IS NULL OR EXISTS ( SELECT 1 FROM CRM_LeadActivity A WHERE A.Ref_LeadId = L.LeadId AND A.IsActive = 1 AND A.Ref_ActivityTypeId = @ActivityTypeId ))
        AND ( @ActivityStatusId IS NULL OR EXISTS ( SELECT 1 FROM CRM_LeadActivity A WHERE A.Ref_LeadId = L.LeadId AND A.IsActive = 1 AND A.Ref_ActivityStatusId = @ActivityStatusId ))
        AND ( @FollowUpStatusId IS NULL OR EXISTS ( SELECT 1 FROM CRM_LeadFollowUp F WHERE F.Ref_LeadId = L.LeadId AND F.Ref_ActivityStatusId = @FollowUpStatusId ))     
        -- SEARCH
        AND (NULLIF(LTRIM(RTRIM(@SearchText)),'') IS NULL
            OR L.LeadNo LIKE '%' + @SearchText + '%'
            OR EXISTS ( SELECT 1 FROM CRM_LeadOrganization LO WHERE LO.LeadOrganizationId = L.Ref_LeadOrganizationId  AND LO.CustomerName LIKE '%' + @SearchText + '%'));      
        -- ROLE ACCESS
        --IF @CanViewAll = 0
        --BEGIN
        --    DELETE RL FROM #ReportLeads RL
        --    WHERE @LoggedInEmployeeId IS NULL  OR ( L.Ref_CurrentAssignedEmployeeId <> @LoggedInEmployeeId
        --            AND NOT EXISTS (SELECT 1 FROM CRM_Lead L WHERE L.LeadId = RL.LeadId AND L.CreatedBy = LoggedInEmployeeId));
        --END;        
        -- FILTERED OPPORTUNITIES       

        IF OBJECT_ID('tempdb..#ReportOpportunities') IS NOT NULL
            DROP TABLE #ReportOpportunities;

        SELECT O.OpportunityId,O.OpportunityNo,O.Ref_LeadId,O.Ref_CustomerId,O.OpportunityName,
            O.Ref_OpportunityTypeId,O.Ref_OpportunityStageId,O.ExpectedRevenue,O.ExpectedCloseDate,O.Probability,O.Ref_OwnerEmployeeId,
            O.Ref_PriorityId,O.CompetitorName,O.OpportunityRemark,O.IsActive,O.CreatedOn,O.CreatedBy,O.UpdatedOn,O.UpdatedBy
        INTO #ReportOpportunities
        FROM CRM_Opportunity O
        LEFT JOIN CRM_Lead L ON L.LeadId = O.Ref_LeadId 
        WHERE (@IsActive IS NULL OR O.IsActive = @IsActive)
            AND ( @BusinessState IN ( 'ALL','QUALIFIED','OPPORTUNITY')
                    OR( @BusinessState = 'QUOTE'AND EXISTS ( SELECT 1 FROM CRM_OpportunityQuotation Q WHERE Q.Ref_OpportunityId =O.OpportunityId AND Q.IsActive = 1))
                    OR( @BusinessState = 'NEGOTIATION'  AND EXISTS (SELECT 1 FROM CRM_OpportunityStage BS WHERE BS.OpportunityStageId =O.Ref_OpportunityStageId AND BS.StageCode IN
                                ('NEGO','RENEGO')))
                    OR( @BusinessState = 'REVIEW'  AND EXISTS ( SELECT 1 FROM CRM_OpportunityStage BS WHERE BS.OpportunityStageId = O.Ref_OpportunityStageId AND BS.StageCode = 'REVW'))
                    OR( @BusinessState = 'WON' AND EXISTS  (SELECT 1 FROM CRM_OpportunityStage BS WHERE BS.OpportunityStageId = O.Ref_OpportunityStageId AND BS.IsWon = 1))
                    OR(@BusinessState = 'LOST' AND EXISTS (SELECT 1 FROM CRM_OpportunityStage BS WHERE BS.OpportunityStageId = O.Ref_OpportunityStageId AND BS.IsLost = 1))
                )      
        -- EMPLOYEE
        AND (@EmployeeId IS NULL OR O.Ref_OwnerEmployeeId = @EmployeeId )      
        -- STAGE
        AND (@OpportunityStageId IS NULL OR O.Ref_OpportunityStageId = @OpportunityStageId)     
        -- TYPE
        AND (@OpportunityTypeId IS NULL OR O.Ref_OpportunityTypeId =@OpportunityTypeId)
        -- COMPANY
        AND (@CompanyId IS NULL OR L.Ref_LeadOrganizationId =@CompanyId)
        AND ( @FromDate IS NULL OR  (@DateType = 'CREATED' AND O.CreatedOn >= @FromDate AND (@ToDateExclusive IS NULL OR O.CreatedOn < @ToDateExclusive))
                OR (@DateType = 'EXPECTED_CLOSE'AND O.ExpectedCloseDate >= @FromDate AND (@ToDateExclusive IS NULL OR O.ExpectedCloseDate < @ToDateExclusive))
                OR ( @DateType = 'ACTIVITY' AND EXISTS (SELECT 1 FROM CRM_OpportunityActivity A WHERE
                            A.Ref_OpportunityId =O.OpportunityId
                            AND A.IsActive = 1 AND A.ActivityDateTime >= @FromDate AND (@ToDateExclusive IS NULL OR A.ActivityDateTime < @ToDateExclusive)))
                OR (@DateType = 'FOLLOWUP' AND EXISTS (SELECT 1 FROM CRM_OpportunityFollowUp F WHERE F.Ref_OpportunityId = O.OpportunityId  AND F.ScheduledOn >= @FromDate 
                            AND (  @ToDateExclusive IS NULL OR F.ScheduledOn < @ToDateExclusive )))
                OR (@DateType = 'ASSIGNMENT' AND EXISTS ( SELECT 1 FROM CRM_OpportunityAssignment OA WHERE OA.Ref_OpportunityId = O.OpportunityId AND OA.AssignedOn >= @FromDate
                            AND ( @ToDateExclusive IS NULL OR OA.AssignedOn < @ToDateExclusive )))
                OR (@DateType = 'STATUS' AND EXISTS (SELECT 1 FROM CRM_OpportunityStatusHistory OSH WHERE OSH.Ref_OpportunityId = O.OpportunityId AND OSH.ChangedOn >= @FromDate
                            AND( @ToDateExclusive IS NULL OR OSH.ChangedOn < @ToDateExclusive )))
            )
        AND ( @ToDateExclusive IS NULL OR O.CreatedOn < @ToDateExclusive)
        AND ( @PriorityId IS NULL OR O.Ref_PriorityId = @PriorityId )
        AND ( @MinProbability IS NULL OR ISNULL(O.Probability, 0) >= @MinProbability )
        AND ( @MaxProbability IS NULL OR ISNULL(O.Probability, 0) <= @MaxProbability )
        AND ( @ExpectedCloseFromDate IS NULL OR O.ExpectedCloseDate >= @ExpectedCloseFromDate)
        AND ( @ExpectedCloseToDateExclusive IS NULL OR O.ExpectedCloseDate < @ExpectedCloseToDateExclusive )
        AND ( @ProductId IS NULL OR EXISTS ( SELECT 1 FROM CRM_OpportunityRequirement ORQ WHERE ORQ.Ref_OpportunityId = O.OpportunityId AND ORQ.IsActive = 1 AND ORQ.IsDeleted = 0 AND ORQ.Ref_ProductId = @ProductId))
        AND ( @ActivityTypeId IS NULL OR EXISTS ( SELECT 1 FROM CRM_OpportunityActivity A WHERE A.Ref_OpportunityId = O.OpportunityId AND A.IsActive = 1 AND A.Ref_ActivityTypeId = @ActivityTypeId))
        AND ( @ActivityStatusId IS NULL OR EXISTS ( SELECT 1 FROM CRM_OpportunityActivity A WHERE A.Ref_OpportunityId =O.OpportunityId AND A.IsActive = 1 AND A.Ref_ActivityStatusId = @ActivityStatusId))
        AND ( @FollowUpStatusId IS NULL OR EXISTS (SELECT 1 FROM CRM_OpportunityFollowUp F WHERE F.Ref_OpportunityId =O.OpportunityId AND F.Ref_ActivityStatusId =@FollowUpStatusId))      
        -- SEARCH
        AND (NULLIF(LTRIM(RTRIM(@SearchText)),'') IS NULL
            OR O.OpportunityNo LIKE '%' + @SearchText + '%'
            OR O.OpportunityName LIKE '%' + @SearchText + '%'
            OR L.LeadNo LIKE '%' + @SearchText + '%'
            OR EXISTS ( SELECT 1 FROM CRM_LeadOrganization LO  WHERE LO.LeadOrganizationId =L.Ref_LeadOrganizationId AND LO.CustomerName LIKE '%' + @SearchText + '%'));
            
        -- ROLE ACCESS
        --IF @CanViewAll = 0
        -- BEGIN 
        --    DELETE RO FROM #ReportOpportunities RO WHERE @LoggedInEmployeeId IS NULL OR RO.Ref_OwnerEmployeeId <>@LoggedInEmployeeId;
        --END;
        
        -- RESULT SET 1
        -- DASHBOARD KPI      

        SELECT ( SELECT COUNT(*) FROM #ReportLeads) AS TotalLeads,
               ( SELECT COUNT(DISTINCT O.Ref_LeadId)  FROM #ReportOpportunities O ) AS QualifiedLeads,
                (SELECT COUNT(*) FROM #ReportOpportunities ) AS TotalOpportunities,
                (SELECT COUNT(*) FROM CRM_OpportunityQuotation Q INNER JOIN #ReportOpportunities O ON O.OpportunityId = Q.Ref_OpportunityId WHERE Q.IsActive = 1) AS TotalQuotations,
                (SELECT COUNT(*) FROM #ReportOpportunities O INNER JOIN CRM_OpportunityStage S ON S.OpportunityStageId = O.Ref_OpportunityStageId WHERE
                        S.StageCode IN( 'NEGO','RENEGO')) AS NegotiationOpportunities,
                (SELECT COUNT(*) FROM #ReportOpportunities O INNER JOIN CRM_OpportunityStage S ON S.OpportunityStageId =O.Ref_OpportunityStageId  WHERE S.StageCode = 'REVW' ) AS ManagementReviewOpportunities,
                (SELECT COUNT(*) FROM #ReportOpportunities O INNER JOIN CRM_OpportunityStage S ON S.OpportunityStageId = O.Ref_OpportunityStageId WHERE S.IsWon = 1) AS WonOpportunities,
                (SELECT COUNT(*) FROM #ReportOpportunities O INNER JOIN CRM_OpportunityStage S ON S.OpportunityStageId = O.Ref_OpportunityStageId WHERE S.IsLost = 1 ) AS LostOpportunities,
                (SELECT ISNULL(SUM(ISNULL(O.ExpectedRevenue, 0)),0) FROM #ReportOpportunities O INNER JOIN CRM_OpportunityStage S ON S.OpportunityStageId =O.Ref_OpportunityStageId WHERE S.IsClosed = 0 ) AS OpenPipelineValue,
                (SELECT ISNULL(SUM(ISNULL(O.ExpectedRevenue, 0)),0)FROM #ReportOpportunities O INNER JOIN CRM_OpportunityStage S ON S.OpportunityStageId =O.Ref_OpportunityStageId WHERE S.IsWon = 1 ) AS WonValue,
                (SELECT ISNULL( AVG( ISNULL(O.Probability, 0)),0) FROM #ReportOpportunities O ) AS AverageProbability,
                CAST
                    (CASE WHEN ( SELECT COUNT(*) FROM #ReportLeads) = 0
                            THEN 0
                            ELSE((SELECT COUNT(DISTINCT O.Ref_LeadId) FROM #ReportOpportunities O) * 100.0)
                                /
                                (SELECT COUNT(*)FROM #ReportLeads )
                        END AS DECIMAL(10,2)) AS LeadToOpportunityConversionPercent;        
        -- RESULT SET 2
        -- STAGE PIPELINE      

        SELECT S.OpportunityStageId ,S.StageCode,S.StageName,S.StageOrder,S.IsClosed,S.IsWon,S.IsLost,
            COUNT(O.OpportunityId)AS OpportunityCount,
            ISNULL(SUM(ISNULL(O.ExpectedRevenue, 0)),0) AS ExpectedRevenue,
            ISNULL(AVG(ISNULL(O.Probability, 0)),0) AS AverageProbability
        FROM CRM_OpportunityStage S
        LEFT JOIN #ReportOpportunities O ON O.Ref_OpportunityStageId = S.OpportunityStageId
        WHERE S.IsActive = 1
        GROUP BY S.OpportunityStageId,S.StageCode,S.StageName,S.StageOrder,S.IsClosed,S.IsWon,S.IsLost
        ORDER BY S.StageOrder;
        
        -- RESULT SET 3
        -- OPPORTUNITY GRID

        --SELECT O.OpportunityId,O.OpportunityNo,O.Ref_LeadId,L.LeadNo,O.OpportunityName,L.Ref_LeadOrganizationId,
        --    LO.CustomerName AS CompanyName,O.Ref_OpportunityTypeId,OT.OpportunityTypeCode,OT.OpportunityTypeName,O.Ref_OpportunityStageId,
        --    S.StageCode,S.StageName,S.StageOrder,O.Ref_OwnerEmployeeId,
        --    NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(E.FirstName, ''),' ',ISNULL(E.LastName, '')))),'') AS OwnerName, 
        --    O.ExpectedRevenue,O.Probability,O.ExpectedCloseDate,O.Ref_PriorityId,O.CompetitorName,
        --    O.OpportunityRemark,O.CreatedOn,O.UpdatedOn,
        --    (SELECT COUNT(*) FROM CRM_OpportunityQuotation Q WHERE Q.Ref_OpportunityId = O.OpportunityId AND Q.IsActive = 1) AS QuotationCount,
        --    (SELECT COUNT(*) FROM CRM_OpportunityNegotiation N WHERE N.Ref_OpportunityId =O.OpportunityId AND N.IsActive = 1) AS NegotiationRoundCount,
        --    (SELECT MAX(A.ActivityDateTime) FROM CRM_OpportunityActivity A WHERE A.Ref_OpportunityId = O.OpportunityId AND A.IsActive = 1 ) AS LastActivityDate,
        --    (SELECT MIN(F.ScheduledOn)FROM CRM_OpportunityFollowUp F WHERE F.Ref_OpportunityId =O.OpportunityId AND F.IsCompleted = 0) AS NextFollowUpDate
        --FROM #ReportOpportunities O
        --INNER JOIN CRM_Lead L ON L.LeadId =O.Ref_LeadId
        --LEFT JOIN CRM_LeadOrganization LO ON LO.LeadOrganizationId =L.Ref_LeadOrganizationId
        --LEFT JOIN CRM_OpportunityType OT ON OT.OpportunityTypeId =O.Ref_OpportunityTypeId
        --LEFT JOIN CRM_OpportunityStage S ON S.OpportunityStageId =O.Ref_OpportunityStageId 
        --LEFT JOIN ORG_Employee E ON E.EmployeeId = O.Ref_OwnerEmployeeId
        --ORDER BY O.CreatedOn DESC;

        -- RESULT SET 3
        -- OPPORTUNITY GRID

        SELECT O.OpportunityId,O.OpportunityNo,ISNULL(O.Ref_LeadId, 0) AS Ref_LeadId,L.LeadNo,O.OpportunityName,
            ISNULL(L.Ref_LeadOrganizationId, 0) AS Ref_LeadOrganizationId,LO.CustomerName AS CompanyName,
            ISNULL(O.Ref_OpportunityTypeId, 0) AS Ref_OpportunityTypeId,OT.OpportunityTypeCode,OT.OpportunityTypeName,
            ISNULL(O.Ref_OpportunityStageId, 0) AS Ref_OpportunityStageId,S.StageCode,S.StageName,
            ISNULL(S.StageOrder, 0) AS StageOrder,ISNULL(O.Ref_OwnerEmployeeId, 0) AS Ref_OwnerEmployeeId,
            NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(E.FirstName, ''),' ',ISNULL(E.LastName, '')))),'') AS OwnerName,
            ISNULL(O.ExpectedRevenue, 0) AS ExpectedRevenue,ISNULL(O.Probability, 0) AS Probability,
            O.ExpectedCloseDate,ISNULL(O.Ref_PriorityId, 0) AS Ref_PriorityId,
            O.CompetitorName,O.OpportunityRemark,O.CreatedOn,O.UpdatedOn,

            (SELECT COUNT(*)FROM CRM_OpportunityQuotation Q
             WHERE Q.Ref_OpportunityId = O.OpportunityId
               AND Q.IsActive = 1) AS QuotationCount,

            (SELECT COUNT(*) FROM CRM_OpportunityNegotiation N
             WHERE N.Ref_OpportunityId = O.OpportunityId
               AND N.IsActive = 1) AS NegotiationRoundCount,

            (SELECT MAX(A.ActivityDateTime) FROM CRM_OpportunityActivity A
             WHERE A.Ref_OpportunityId = O.OpportunityId
               AND A.IsActive = 1) AS LastActivityDate,

            (SELECT MIN(F.ScheduledOn) FROM CRM_OpportunityFollowUp F
             WHERE F.Ref_OpportunityId = O.OpportunityId
               AND F.IsCompleted = 0) AS NextFollowUpDate

        FROM #ReportOpportunities O
        LEFT JOIN CRM_Lead L ON L.LeadId = O.Ref_LeadId
        LEFT JOIN CRM_LeadOrganization LO ON LO.LeadOrganizationId = L.Ref_LeadOrganizationId
        LEFT JOIN CRM_OpportunityType OT ON OT.OpportunityTypeId = O.Ref_OpportunityTypeId
        LEFT JOIN CRM_OpportunityStage S ON S.OpportunityStageId = O.Ref_OpportunityStageId
        LEFT JOIN ORG_Employee E ON E.EmployeeId = O.Ref_OwnerEmployeeId
        ORDER BY O.CreatedOn DESC;
        
        -- RESULT SET 4
        -- LEAD  OPPORTUNITY CONVERSION LIST        

        SELECT L.LeadId,L.LeadNo,L.CreatedOn AS LeadCreatedOn,L.Ref_LeadStatusId,O.OpportunityId,O.OpportunityNo,O.CreatedOn AS OpportunityCreatedOn,
            O.ExpectedRevenue,S.StageCode,S.StageName,
            NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(E.FirstName, ''),' ',ISNULL(E.LastName, '')))),'') AS OpportunityOwner,OA.AssignedOn AS ConversionDate,OA.AssignmentRemark
        FROM #ReportOpportunities O
        INNER JOIN CRM_Lead L ON L.LeadId =O.Ref_LeadId
        LEFT JOIN CRM_OpportunityStage S ON S.OpportunityStageId =O.Ref_OpportunityStageId
        LEFT JOIN ORG_Employee E ON E.EmployeeId =O.Ref_OwnerEmployeeId
        OUTER APPLY ( SELECT TOP 1 A.AssignedOn,A.AssignmentRemark
                        FROM CRM_OpportunityAssignment A WHERE A.Ref_OpportunityId =O.OpportunityId
                AND A.AssignmentType ='CREATE'
                AND A.IsActive = 1
            ORDER BY A.AssignedOn ASC
        ) OA
        ORDER BY OA.AssignedOn DESC;

        -- RESULT SET 5
        -- LEAD GRID (Total Leads card)

        SELECT RL.LeadId,RL.LeadNo,L.LeadTitle,ISNULL(RL.Ref_LeadOrganizationId, 0) AS Ref_LeadOrganizationId,LO.CustomerName AS CompanyName,
            RL.Ref_LeadStatusId,LS.StatusName AS LeadStatusName,RL.Ref_LeadSourceId,SRC.SourceName AS LeadSourceName,
            RL.Ref_CurrentAssignedEmployeeId,
            NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(E.FirstName, ''),' ',ISNULL(E.LastName, '')))),'') AS OwnerName,
            RL.ExpectedValue,RL.CreatedOn,RL.UpdatedOn,RL.ClosedOn,
            CAST(CASE WHEN EXISTS (SELECT 1 FROM #ReportOpportunities O WHERE O.Ref_LeadId = RL.LeadId) THEN 1 ELSE 0 END AS BIT) AS HasOpportunity
        FROM #ReportLeads RL
        INNER JOIN CRM_Lead L ON L.LeadId = RL.LeadId
        LEFT JOIN CRM_LeadOrganization LO ON LO.LeadOrganizationId = RL.Ref_LeadOrganizationId
        LEFT JOIN CRM_LeadStatus LS ON LS.LeadStatusId = RL.Ref_LeadStatusId
        LEFT JOIN CRM_LeadSource SRC ON SRC.LeadSourceId = RL.Ref_LeadSourceId
        LEFT JOIN ORG_Employee E ON E.EmployeeId = RL.Ref_CurrentAssignedEmployeeId
        ORDER BY RL.CreatedOn DESC;

        -- RESULT SET 6
        -- QUOTATION GRID (Total Quotations card)

        SELECT Q.OpportunityQuotationId,Q.QuotationNo,Q.RevisionNumber,Q.IsCurrent,Q.QuotationDate,Q.ValidUntil,
            O.OpportunityId,O.OpportunityNo,O.OpportunityName,ISNULL(O.Ref_LeadId, 0) AS Ref_LeadId,LO.CustomerName AS CompanyName,
            NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(E.FirstName, ''),' ',ISNULL(E.LastName, '')))),'') AS OwnerName,
            Q.SubTotal,Q.TaxAmount,Q.TotalAmount,Q.CustomerResponseCode,Q.SentOn,Q.CreatedOn
        FROM CRM_OpportunityQuotation Q
        INNER JOIN #ReportOpportunities O ON O.OpportunityId = Q.Ref_OpportunityId
        LEFT JOIN CRM_Lead L ON L.LeadId = O.Ref_LeadId
        LEFT JOIN CRM_LeadOrganization LO ON LO.LeadOrganizationId = L.Ref_LeadOrganizationId
        LEFT JOIN ORG_Employee E ON E.EmployeeId = O.Ref_OwnerEmployeeId
        WHERE Q.IsActive = 1
        ORDER BY Q.CreatedOn DESC;

        DROP TABLE #ReportLeads;
        DROP TABLE #ReportOpportunities;

        RETURN;
    END;
  
    -- 2. COMPLETE LEAD  OPPORTUNITY JOURNEY
ELSE IF @Status = 'CRM_REPORT_JOURNEY'
    BEGIN
        IF NULLIF( LTRIM(RTRIM(@SearchText)),'') IS NULL AND @LeadId IS NULL AND @OpportunityId IS NULL
        BEGIN 
            RAISERROR('Lead No, Opportunity No, SearchText, LeadId or OpportunityId is required.',16,1);
            RETURN;
        END;      

        -- Find Lead / Opportunity   
        DECLARE @JourneyLeadId INT = @LeadId;
        DECLARE @JourneyOpportunityId INT =@OpportunityId;
        
        -- SEARCH BY OPPORTUNITY NO
        IF @JourneyOpportunityId IS NULL AND NULLIF(LTRIM(RTRIM(@SearchText)),'') IS NOT NULL
        BEGIN
            SELECT TOP 1 @JourneyOpportunityId =O.OpportunityId,
                        @JourneyLeadId =O.Ref_LeadId
            FROM CRM_Opportunity O
            WHERE O.OpportunityNo =@SearchText
                OR O.OpportunityNo LIKE '%' + @SearchText + '%'
            ORDER BY
                CASE
                    WHEN O.OpportunityNo =@SearchText
                    THEN 1
                    ELSE 2
                END,
                O.CreatedOn DESC;
        END;
        
        -- SEARCH BY LEAD NO
        IF @JourneyLeadId IS NULL AND NULLIF(LTRIM(RTRIM(@SearchText)),'') IS NOT NULL
        BEGIN
            SELECT TOP 1 
                @JourneyLeadId =L.LeadId FROM CRM_Lead L
            WHERE L.LeadNo =@SearchText
                OR L.LeadNo LIKE '%' + @SearchText + '%'
            ORDER BY
                CASE
                    WHEN L.LeadNo =@SearchText
                    THEN 1
                    ELSE 2
                END,
                L.CreatedOn DESC;
        END;
      
        -- SEARCH BY COMPANY
        IF @JourneyLeadId IS NULL AND NULLIF(LTRIM(RTRIM(@SearchText)),'') IS NOT NULL
        BEGIN
            SELECT TOP 1 @JourneyLeadId =L.LeadId
            FROM CRM_Lead L
            INNER JOIN CRM_LeadOrganization LO ON LO.LeadOrganizationId = L.Ref_LeadOrganizationId
            WHERE LO.CustomerName LIKE '%' + @SearchText + '%'
            ORDER BY L.CreatedOn DESC;
        END;
        
        -- VALIDATE
        IF @JourneyLeadId IS NULL
        BEGIN
            RAISERROR('No Lead or Opportunity found for the supplied search.',16,1);
            RETURN;
        END;
        
        -- RESULT SET 1
        -- JOURNEY HEADER

        SELECT L.LeadId,L.LeadNo,L.LeadTitle,LO.CustomerName AS CompanyName,L.CreatedOn AS LeadCreatedOn,L.Ref_LeadStatusId,
            O.OpportunityId,O.OpportunityNo,O.OpportunityName,O.CreatedOn AS OpportunityCreatedOn,O.ExpectedRevenue,O.ExpectedCloseDate,O.Probability,S.StageCode,S.StageName,
            NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(E.FirstName, ''),' ',ISNULL(E.LastName, '')))),'') AS OpportunityOwnerName,
            OA.AssignedOn AS ConversionDate,
            OA.AssignmentRemark AS ConversionRemark
        FROM CRM_Lead L
        LEFT JOIN CRM_LeadOrganization LO ON LO.LeadOrganizationId =L.Ref_LeadOrganizationId
        OUTER APPLY ( SELECT TOP 1 O1.* FROM CRM_Opportunity O1  WHERE O1.Ref_LeadId = L.LeadId
                     AND(@JourneyOpportunityId IS NULL OR O1.OpportunityId = @JourneyOpportunityId )
            ORDER BY O1.CreatedOn ASC
        ) O
        LEFT JOIN CRM_OpportunityStage S ON S.OpportunityStageId = O.Ref_OpportunityStageId
        LEFT JOIN ORG_Employee E ON E.EmployeeId =O.Ref_OwnerEmployeeId
        OUTER APPLY (  SELECT TOP 1 A.AssignedOn, A.AssignmentRemark FROM CRM_OpportunityAssignment A WHERE A.Ref_OpportunityId =O.OpportunityId
                AND A.AssignmentType ='CREATE' AND A.IsActive = 1
            ORDER BY A.AssignedOn ASC
        ) OA
        WHERE L.LeadId = @JourneyLeadId;
        
        -- RESULT SET 2
        -- LEAD DETAILS

        SELECT L.LeadId,L.LeadNo,L.LeadTitle,L.Requirement,L.LeadRemark,L.Ref_LeadOrganizationId,LO.CustomerName AS CompanyName,
            L.Ref_LeadSourceId,L.Ref_LeadStatusId,L.Ref_CurrentAssignedEmployeeId,
            NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(E.FirstName, ''),' ',ISNULL(E.LastName, '')))),'') AS LeadOwnerName,
            L.Ref_PriorityId,L.ExpectedValue,L.ExpectedClosureDate,L.Probability,L.CreatedOn,L.UpdatedOn,L.ClosedOn
        FROM CRM_Lead L
        LEFT JOIN CRM_LeadOrganization LO ON LO.LeadOrganizationId = L.Ref_LeadOrganizationId
        LEFT JOIN ORG_Employee E ON E.EmployeeId =L.Ref_CurrentAssignedEmployeeId
        WHERE L.LeadId =@JourneyLeadId;     
       
        -- RESULT SET 3
        -- LEAD ACTIVITIES
        SELECT A.ActivityId,A.Ref_LeadId,A.Ref_LeadAssignmentId,A.Ref_ActivityTypeId,A.Ref_EmployeeId,
            NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(E.FirstName,''),' ',ISNULL(E.LastName,'')))),'') AS EmployeeName,
            A.ActivityDateTime,A.Subject,A.Description,A.Outcome,A.NextFollowUpDate,A.DurationInMinutes,A.Latitude,A.Longitude,A.LocationAddress,
            A.Ref_ActivityStatusId,A.Ref_PreviousLeadStatusId,A.Ref_NewLeadStatusId,A.CreatedOn,
            (
                SELECT AA.AttachmentId,AA.Ref_ActivityId,AA.FileName,AA.FilePath,AA.FileType,AA.FileSize,AA.CreatedBy,AA.CreatedOn,AA.Ref_ActivityExpenseId
                FROM CRM_LeadActivityAttachment AA
                WHERE AA.Ref_ActivityId =A.ActivityId
                FOR JSON PATH
            ) AS AttachmentsJson
        FROM CRM_LeadActivity A
        LEFT JOIN ORG_Employee E ON E.EmployeeId =A.Ref_EmployeeId
        WHERE A.Ref_LeadId =@JourneyLeadId
            AND A.IsActive =1
        ORDER BY A.ActivityDateTime DESC;
        
        -- RESULT SET 4
        -- LEAD FOLLOW-UPS       

        SELECT F.FollowUpId,F.Ref_LeadId,F.Ref_AssignedEmployeeId,
            NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(E.FirstName, ''),' ',ISNULL(E.LastName, '')))),'') AS AssignedEmployeeName,
            F.ScheduledOn,F.Priority,F.Remarks,F.IsCompleted,F.CompletedOn,F.Ref_CompletedByEmployeeId,
            NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(CE.FirstName, ''),' ',ISNULL(CE.LastName, '')))),'') AS CompletedByEmployeeName,
            F.Ref_ActivityStatusId,F.Ref_ActivityId,F.CreatedOn
        FROM CRM_LeadFollowUp F
        LEFT JOIN ORG_Employee E ON E.EmployeeId =F.Ref_AssignedEmployeeId
        LEFT JOIN ORG_Employee CE ON CE.EmployeeId = F.Ref_CompletedByEmployeeId
        WHERE F.Ref_LeadId =@JourneyLeadId
        ORDER BY F.ScheduledOn DESC
        
        -- RESULT SET 5
        -- OPPORTUNITY       

        --SELECT O.OpportunityId,O.OpportunityNo,O.Ref_LeadId,L.LeadNo,O.Ref_CustomerId,LO.CustomerName AS CompanyName,O.OpportunityName,O.Ref_OpportunityTypeId,OT.OpportunityTypeCode,OT.OpportunityTypeName,
        --       O.Ref_OpportunityStageId, S.StageCode, S.StageName, S.StageOrder,O.ExpectedRevenue, O.ExpectedCloseDate,O.Probability,O.Ref_OwnerEmployeeId,
        --    NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(E.FirstName, ''),' ',ISNULL(E.LastName, '')))),'') AS OwnerName,
        --    O.Ref_PriorityId,O.CompetitorName,O.OpportunityRemark,O.CreatedOn,O.CreatedBy,O.UpdatedOn,O.UpdatedBy

        SELECT O.OpportunityId,O.OpportunityNo,ISNULL(O.Ref_LeadId, 0) AS Ref_LeadId,L.LeadNo,
                ISNULL(O.Ref_CustomerId, 0) AS Ref_CustomerId,LO.CustomerName AS CompanyName,
                O.OpportunityName,ISNULL(O.Ref_OpportunityTypeId, 0) AS Ref_OpportunityTypeId,
                OT.OpportunityTypeCode,OT.OpportunityTypeName,
                ISNULL(O.Ref_OpportunityStageId, 0) AS Ref_OpportunityStageId,
                S.StageCode,S.StageName,
                ISNULL(S.StageOrder, 0) AS StageOrder,
                ISNULL(O.ExpectedRevenue, 0) AS ExpectedRevenue,
                O.ExpectedCloseDate,ISNULL(O.Probability, 0) AS Probability,
                ISNULL(O.Ref_OwnerEmployeeId, 0) AS Ref_OwnerEmployeeId,
                NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(E.FirstName, ''),' ',ISNULL(E.LastName, '')))),'') AS OwnerName,
                ISNULL(O.Ref_PriorityId, 0) AS Ref_PriorityId,
                O.CompetitorName,O.OpportunityRemark,O.CreatedOn,O.CreatedBy,O.UpdatedOn,O.UpdatedBy
        FROM CRM_Opportunity O
        INNER JOIN CRM_Lead L ON L.LeadId =O.Ref_LeadId
        LEFT JOIN CRM_LeadOrganization LO ON LO.LeadOrganizationId =L.Ref_LeadOrganizationId
        LEFT JOIN CRM_OpportunityType OT ON OT.OpportunityTypeId =O.Ref_OpportunityTypeId
        LEFT JOIN CRM_OpportunityStage S ON S.OpportunityStageId =O.Ref_OpportunityStageId
        LEFT JOIN ORG_Employee E ON E.EmployeeId =O.Ref_OwnerEmployeeId
        WHERE O.Ref_LeadId =@JourneyLeadId
            AND O.IsActive = 1
            AND(@JourneyOpportunityId IS NULL OR O.OpportunityId =@JourneyOpportunityId)
        ORDER BY O.CreatedOn DESC

        ---- RESULT SET 6
        ---- REQUIREMENTS
        --SELECT 'LEAD' AS RequirementType,R.LeadRequirementId AS RequirementId,R.Ref_LeadId AS LeadId,NULL AS OpportunityId,NULL AS Ref_LeadRequirementId,R.Ref_ProductId AS ProductId,P.ProductCode,P.ProductName,P.Ref_ProductCategoryId,PC.CategoryCode,PC.CategoryName,R.Quantity,R.RequirementRemark,R.Ref_UnitId AS UnitId,R.IsActive,CAST(0 AS bit) AS IsDeleted,R.CreatedOn,R.CreatedBy,R.UpdatedOn,R.UpdatedBy
        --FROM CRM_LeadRequirement R
        --LEFT JOIN MST_Offering P ON P.ProductId =R.Ref_ProductId
        --LEFT JOIN MST_ProductCategory PC ON PC.ProductCategoryId =P.Ref_ProductCategoryId
        --WHERE R.Ref_LeadId =@JourneyLeadId
        --    AND R.IsActive =1
        --UNION ALL
        --SELECT 'OPPORTUNITY' AS RequirementType,R.OpportunityRequirementId AS RequirementId,O.Ref_LeadId AS LeadId,R.Ref_OpportunityId AS OpportunityId,R.Ref_LeadRequirementId,R.Ref_ProductId AS ProductId,P.ProductCode,P.ProductName,P.Ref_ProductCategoryId,PC.CategoryCode,PC.CategoryName,R.Quantity,R.RequirementRemark,R.Ref_UnitId AS UnitId,R.IsActive,R.IsDeleted,R.CreatedOn,R.CreatedBy,R.UpdatedOn,R.UpdatedBy
        --FROM CRM_OpportunityRequirement R
        --INNER JOIN CRM_Opportunity O ON O.OpportunityId =R.Ref_OpportunityId
        --LEFT JOIN MST_Offering P ON P.ProductId =R.Ref_ProductId
        --LEFT JOIN MST_ProductCategory PC ON PC.ProductCategoryId =P.Ref_ProductCategoryId
        --WHERE O.Ref_LeadId =@JourneyLeadId
        --    AND(@JourneyOpportunityId IS NULL OR R.Ref_OpportunityId =@JourneyOpportunityId)
        --    AND R.IsDeleted =0
        --ORDER BY CreatedOn ASC;        

        -- RESULT SET 6
        -- REQUIREMENTS
        --SELECT 'LEAD' AS RequirementType,R.LeadRequirementId AS RequirementId,R.Ref_LeadId AS LeadId,NULL AS OpportunityId,NULL AS Ref_LeadRequirementId,R.Ref_ProductId AS ProductId,P.ProductCode,P.ProductName,P.Ref_ProductCategoryId,PC.CategoryCode,PC.CategoryName,R.Quantity,R.RequirementRemark,R.Ref_UnitId AS UnitId,R.IsActive,CAST(0 AS bit) AS IsDeleted,R.CreatedOn,R.CreatedBy,R.UpdatedOn,R.UpdatedBy
        --FROM CRM_LeadRequirement R
        --LEFT JOIN MST_Offering P ON P.ProductId =R.Ref_ProductId
        --LEFT JOIN MST_ProductCategory PC ON PC.ProductCategoryId =P.Ref_ProductCategoryId
        --WHERE R.Ref_LeadId =@JourneyLeadId
        --    AND R.IsActive =1
        --    AND NOT EXISTS(
        --        SELECT 1
        --        FROM CRM_OpportunityRequirement ORQ
        --        INNER JOIN CRM_Opportunity OQ ON OQ.OpportunityId =ORQ.Ref_OpportunityId
        --        WHERE ORQ.Ref_LeadRequirementId =R.LeadRequirementId
        --            AND ORQ.IsDeleted =0
        --            AND OQ.Ref_LeadId =@JourneyLeadId
        --            AND(@JourneyOpportunityId IS NULL OR ORQ.Ref_OpportunityId =@JourneyOpportunityId)
        --    )
        --UNION ALL
        --SELECT 'OPPORTUNITY' AS RequirementType,R.OpportunityRequirementId AS RequirementId,O.Ref_LeadId AS LeadId,R.Ref_OpportunityId AS OpportunityId,R.Ref_LeadRequirementId,R.Ref_ProductId AS ProductId,P.ProductCode,P.ProductName,P.Ref_ProductCategoryId,PC.CategoryCode,PC.CategoryName,R.Quantity,R.RequirementRemark,R.Ref_UnitId AS UnitId,R.IsActive,R.IsDeleted,R.CreatedOn,R.CreatedBy,R.UpdatedOn,R.UpdatedBy
        --FROM CRM_OpportunityRequirement R
        --INNER JOIN CRM_Opportunity O ON O.OpportunityId =R.Ref_OpportunityId
        --LEFT JOIN MST_Offering P ON P.ProductId =R.Ref_ProductId
        --LEFT JOIN MST_ProductCategory PC ON PC.ProductCategoryId =P.Ref_ProductCategoryId
        --WHERE O.Ref_LeadId =@JourneyLeadId
        --    AND(@JourneyOpportunityId IS NULL OR R.Ref_OpportunityId =@JourneyOpportunityId)
        --    AND R.IsDeleted =0
        --ORDER BY CreatedOn DESC;

        -- RESULT SET 6
        -- REQUIREMENTS
        SELECT 'LEAD' AS RequirementType,
               R.LeadRequirementId AS RequirementId,
               R.Ref_LeadId AS LeadId,
               NULL AS OpportunityId,
               NULL AS Ref_LeadRequirementId,
               R.Ref_ProductId AS ProductId,
               P.ItemCode AS ProductCode,
               P.ItemName AS ProductName,
               R.Ref_ProductCategoryId,
               PC.CategoryName,
               R.Ref_OfferingId AS OfferingId,
               O.ProductName AS OfferingName,
               R.Quantity,
               R.RequirementRemark,
               R.Ref_UnitId AS UnitId,
               R.IsActive,
               CAST(0 AS bit) AS IsDeleted,
               R.CreatedOn,
               R.CreatedBy,
               R.UpdatedOn,
               R.UpdatedBy
        FROM CRM_LeadRequirement R
        LEFT JOIN MST_ProductCategory PC ON PC.ProductCategoryId=R.Ref_ProductCategoryId
        LEFT JOIN MST_Offering O ON O.ProductId=R.Ref_OfferingId
        LEFT JOIN MST_Product P ON P.ProductId=R.Ref_ProductId
        WHERE R.Ref_LeadId=@JourneyLeadId
        AND R.IsActive=1
        AND NOT EXISTS(
            SELECT 1
            FROM CRM_OpportunityRequirement ORQ
            INNER JOIN CRM_Opportunity OQ ON OQ.OpportunityId=ORQ.Ref_OpportunityId
            WHERE ORQ.Ref_LeadRequirementId=R.LeadRequirementId
              AND ORQ.IsDeleted=0
              AND OQ.Ref_LeadId=@JourneyLeadId
              AND(@JourneyOpportunityId IS NULL OR ORQ.Ref_OpportunityId=@JourneyOpportunityId)
        )
        UNION ALL
        SELECT 'OPPORTUNITY' AS RequirementType,
               R.OpportunityRequirementId AS RequirementId,
               O.Ref_LeadId AS LeadId,
               R.Ref_OpportunityId AS OpportunityId,
               R.Ref_LeadRequirementId,
               R.Ref_ProductId AS ProductId,
               P.ItemCode AS ProductCode,
               P.ItemName AS ProductName,
               R.Ref_ProductCategoryId,
               PC.CategoryName,
               R.Ref_OfferingId AS OfferingId,
               OFR.ProductName AS OfferingName,
               R.Quantity,
               R.RequirementRemark,
               R.Ref_UnitId AS UnitId,
               R.IsActive,
               R.IsDeleted,
               R.CreatedOn,
               R.CreatedBy,
               R.UpdatedOn,
               R.UpdatedBy
        FROM CRM_OpportunityRequirement R
        INNER JOIN CRM_Opportunity O ON O.OpportunityId=R.Ref_OpportunityId
        LEFT JOIN MST_ProductCategory PC ON PC.ProductCategoryId=R.Ref_ProductCategoryId
        LEFT JOIN MST_Offering OFR ON OFR.ProductId=R.Ref_OfferingId
        LEFT JOIN MST_Product P ON P.ProductId=R.Ref_ProductId
        WHERE O.Ref_LeadId=@JourneyLeadId
        AND(@JourneyOpportunityId IS NULL OR R.Ref_OpportunityId=@JourneyOpportunityId)
        AND R.IsDeleted=0
        ORDER BY CreatedOn DESC;

        -- RESULT SET 7
        -- QUOTATIONS
        SELECT Q.OpportunityQuotationId,Q.Ref_OpportunityId,Q.QuotationNo,Q.VersionNumber,Q.RevisionNumber,Q.RootQuotationNo,Q.Ref_PreviousQuotationId,Q.IsCurrent,Q.QuotationDate,
            Q.ValidUntil,Q.SubTotal,Q.FreightCharge,Q.FreightPercent,Q.TaxPercent,Q.TaxAmount,Q.TotalAmount,Q.TermsRemark,Q.SentByEmployeeId,
            NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(SE.FirstName,''),' ',ISNULL(SE.LastName,'')))),'') AS SentByEmployeeName,Q.SentOn,
            Q.CustomerResponseCode,Q.CustomerResponseByEmployeeId,
            NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(RE.FirstName,''),' ',ISNULL(RE.LastName,'')))),'') AS CustomerResponseByEmployeeName,
            Q.CustomerResponseOn,Q.CustomerResponseRemark,Q.IsActive,Q.CreatedOn,Q.CreatedBy,Q.UpdatedOn,Q.UpdatedBy,
            (
                SELECT QA.AttachmentId,QA.Ref_OpportunityQuotationId,QA.FileName,QA.FilePath,QA.FileType,QA.FileSize,QA.CreatedOn,QA.CreatedBy
                FROM CRM_OpportunityQuotationAttachment QA
                WHERE QA.Ref_OpportunityQuotationId =Q.OpportunityQuotationId
                FOR JSON PATH
            ) AS AttachmentsJson
        FROM CRM_OpportunityQuotation Q
        INNER JOIN CRM_Opportunity O ON O.OpportunityId =Q.Ref_OpportunityId
        LEFT JOIN ORG_Employee SE ON SE.EmployeeId =Q.SentByEmployeeId
        LEFT JOIN ORG_Employee RE ON RE.EmployeeId =Q.CustomerResponseByEmployeeId
        WHERE O.Ref_LeadId =@JourneyLeadId
            AND(@JourneyOpportunityId IS NULL OR Q.Ref_OpportunityId =@JourneyOpportunityId)
            AND Q.IsActive =1
        ORDER BY Q.QuotationDate DESC,Q.VersionNumber DESC;
            
        -- RESULT SET 8
        -- QUOTATION ITEMS
        SELECT QI.OpportunityQuotationItemId,QI.Ref_OpportunityQuotationId,QI.Ref_OpportunityRequirementId,QI.Quantity,QI.Rate,QI.Amount,R.Ref_ProductId,P.ProductCode,P.ProductName
        FROM CRM_OpportunityQuotationItem QI
        INNER JOIN CRM_OpportunityQuotation Q ON Q.OpportunityQuotationId =QI.Ref_OpportunityQuotationId
        INNER JOIN CRM_Opportunity O ON O.OpportunityId =Q.Ref_OpportunityId
        LEFT JOIN CRM_OpportunityRequirement R ON R.OpportunityRequirementId =QI.Ref_OpportunityRequirementId
        LEFT JOIN MST_Offering P ON P.ProductId = R.Ref_ProductId
        WHERE O.Ref_LeadId =@JourneyLeadId
            AND (@JourneyOpportunityId IS NULL OR Q.Ref_OpportunityId =@JourneyOpportunityId)
        ORDER BY
            QI.Ref_OpportunityQuotationId,
            QI.OpportunityQuotationItemId;
            
        -- RESULT SET 9
        -- NEGOTIATIONS
        SELECT N.OpportunityNegotiationId,N.Ref_OpportunityId,N.Ref_OpportunityQuotationId, N.RoundNumber,N.OurOfferAmount,N.CustomerCounterAmount,
            CASE
                WHEN N.CustomerCounterAmount IS NULL
                THEN NULL
                ELSE
                    N.OurOfferAmount - N.CustomerCounterAmount
            END AS OfferDifference,
            N.Remarks,N.RoundDate,N.NextFollowUpDate,N.StatusCode,N.IsActive,N.CreatedOn,N.CreatedBy,N.UpdatedOn,N.UpdatedBy
        FROM CRM_OpportunityNegotiation N

        INNER JOIN CRM_Opportunity O ON O.OpportunityId =N.Ref_OpportunityId
        WHERE O.Ref_LeadId =@JourneyLeadId
            AND (@JourneyOpportunityId IS NULL OR N.Ref_OpportunityId =@JourneyOpportunityId)
            AND N.IsActive = 1
        ORDER BY N.RoundNumber DESC;
        
        -- RESULT SET 10
        -- MANAGEMENT REVIEWS   
        SELECT MR.ManagementReviewId,MR.Ref_OpportunityId,MR.Ref_OpportunityQuotationId,MR.Ref_OpportunityNegotiationId,MR.Margin,MR.Remarks,MR.DecisionCode,MR.ReviewedByEmployeeId,
            NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(E.FirstName, ''),' ',ISNULL(E.LastName, '')))),'') AS ReviewedByEmployeeName,
            MR.ReviewedOn,MR.IsActive,MR.CreatedOn,MR.CreatedBy
        FROM CRM_OpportunityManagementReview MR

        INNER JOIN CRM_Opportunity O ON O.OpportunityId =MR.Ref_OpportunityId
        LEFT JOIN ORG_Employee E  ON E.EmployeeId = O.Ref_OwnerEmployeeId
        WHERE O.Ref_LeadId =@JourneyLeadId
            AND(@JourneyOpportunityId IS NULL OR MR.Ref_OpportunityId =@JourneyOpportunityId)
            AND MR.IsActive = 1
        ORDER BY MR.ReviewedOn DESC;            
       
        -- RESULT SET 11
        -- OPPORTUNITY ACTIVITIES
        SELECT A.ActivityId,A.Ref_OpportunityId,A.Ref_OpportunityAssignmentId,A.Ref_ActivityTypeId,A.Ref_EmployeeId,
            NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(E.FirstName,''),' ',ISNULL(E.LastName,'')))),'') AS EmployeeName,
            A.ActivityDateTime,A.Subject,A.Description,A.Outcome,A.NextFollowUpDate,A.DurationInMinutes,A.Latitude,A.Longitude,A.LocationAddress,
            A.Ref_ActivityStatusId,A.Ref_OpportunityStageId,A.Ref_PreviousOpportunityStageId,A.Ref_NewOpportunityStageId,A.Ref_SourceLeadActivityId,
            A.CreatedOn,A.CreatedBy,A.UpdatedOn,A.UpdatedBy,
            (
                SELECT AA.AttachmentId,AA.Ref_OpportunityActivityId,AA.Ref_ActivityExpenseId,AA.FileName,AA.FilePath,AA.FileType,AA.FileSize,AA.CreatedBy,AA.CreatedOn
                FROM CRM_OpportunityActivityAttachment AA
                WHERE AA.Ref_OpportunityActivityId =A.ActivityId
                FOR JSON PATH
            ) AS AttachmentsJson
        FROM CRM_OpportunityActivity A
        INNER JOIN CRM_Opportunity O ON O.OpportunityId =A.Ref_OpportunityId
        LEFT JOIN ORG_Employee E ON E.EmployeeId =A.Ref_EmployeeId
        WHERE O.Ref_LeadId =@JourneyLeadId
            AND(@JourneyOpportunityId IS NULL OR A.Ref_OpportunityId =@JourneyOpportunityId)
            AND A.IsActive =1
        ORDER BY A.ActivityDateTime DESC;
        
        -- RESULT SET 12
        -- OPPORTUNITY FOLLOW-UPS
        SELECT  F.OpportunityFollowUpId,F.Ref_OpportunityId,F.Ref_OpportunityActivityId,F.Ref_AssignedEmployeeId,
            NULLIF(LTRIM(RTRIM(CONCAT( ISNULL(AE.FirstName, ''),' ',ISNULL(AE.LastName, '')))), '') AS AssignedEmployeeName,
            F.ScheduledOn, F.Priority, F.Remarks,F.IsCompleted, F.CompletedOn,F.Ref_CompletedByEmployeeId,
            NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(CE.FirstName, ''),' ',ISNULL(CE.LastName, '')))), '' ) AS CompletedByEmployeeName,
            F.Ref_ActivityStatusId,F.Ref_SourceLeadFollowUpId, F.CreatedOn,F.CreatedBy,F.UpdatedOn, F.UpdatedBy
        FROM CRM_OpportunityFollowUp F
        INNER JOIN CRM_Opportunity O ON O.OpportunityId =F.Ref_OpportunityId
        LEFT JOIN ORG_Employee AE ON AE.EmployeeId =F.Ref_AssignedEmployeeId
        LEFT JOIN ORG_Employee CE ON CE.EmployeeId =F.Ref_CompletedByEmployeeId
        WHERE O.Ref_LeadId =@JourneyLeadId
            AND(@JourneyOpportunityId IS NULL OR F.Ref_OpportunityId =@JourneyOpportunityId)
        ORDER BY F.ScheduledOn DESC;
        
        -- RESULT SET 13
        -- OPPORTUNITY STATUS HISTORY 
        SELECT H.OpportunityStatusHistoryId,H.Ref_OpportunityId,H.Ref_OpportunityStageId,S.StageCode,S.StageName,S.StageOrder,H.Ref_PreviousOpportunityStageId,PS.StageCode AS PreviousStageCode,
             PS.StageName AS PreviousStageName,H.Ref_ChangedByEmployeeId,
            NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(E.FirstName, ''),' ',ISNULL(E.LastName, '')))),'') AS ChangedByEmployeeName,H.StatusRemark,H.ChangedOn,H.CreatedOn
        FROM CRM_OpportunityStatusHistory H
        INNER JOIN CRM_Opportunity O ON O.OpportunityId = H.Ref_OpportunityId
        LEFT JOIN CRM_OpportunityStage S ON S.OpportunityStageId = H.Ref_OpportunityStageId
        LEFT JOIN CRM_OpportunityStage PS ON PS.OpportunityStageId = H.Ref_PreviousOpportunityStageId
        LEFT JOIN ORG_Employee E ON E.EmployeeId =H.Ref_ChangedByEmployeeId
        WHERE O.Ref_LeadId = @JourneyLeadId
            AND (@JourneyOpportunityId IS NULL OR H.Ref_OpportunityId = @JourneyOpportunityId)
        ORDER BY H.ChangedOn DESC;
        
        -- RESULT SET 14
        -- OPPORTUNITY ASSIGNMENT / CONVERSION
        SELECT A.OpportunityAssignmentId,A.Ref_OpportunityId, A.Ref_FromEmployeeId,
            NULLIF( LTRIM(RTRIM(CONCAT(ISNULL(FE.FirstName, ''),' ',ISNULL(FE.LastName, '')))),'') AS FromEmployeeName,
            A.Ref_ToEmployeeId,
            NULLIF(LTRIM(RTRIM(CONCAT( ISNULL(TE.FirstName, ''),' ',ISNULL(TE.LastName, '')))),'' ) AS ToEmployeeName,
            A.Ref_AssignedByEmployeeId,
            NULLIF( LTRIM(RTRIM(CONCAT(ISNULL(ABE.FirstName, ''),' ',ISNULL(ABE.LastName, '')))),'') AS AssignedByEmployeeName,
            A.AssignmentType,A.AssignmentRemark,A.AssignedOn,A.IsCurrent, A.IsActive, A.CreatedOn
        FROM CRM_OpportunityAssignment A
        INNER JOIN CRM_Opportunity O  ON O.OpportunityId = A.Ref_OpportunityId
        LEFT JOIN ORG_Employee FE ON FE.EmployeeId = A.Ref_FromEmployeeId
        LEFT JOIN ORG_Employee TE ON TE.EmployeeId = A.Ref_ToEmployeeId
        LEFT JOIN ORG_Employee ABE ON ABE.EmployeeId = A.Ref_AssignedByEmployeeId
        WHERE  O.Ref_LeadId = @JourneyLeadId
            AND( @JourneyOpportunityId IS NULL   OR A.Ref_OpportunityId = @JourneyOpportunityId )
            AND A.IsActive = 1
        ORDER BY A.AssignedOn DESC;           

       -- RESULT SET 15
        -- EXPENSES
        SELECT AE.ActivityExpenseId,CASE WHEN AE.Ref_LeadId IS NOT NULL THEN 'LEAD' ELSE 'OPPORTUNITY' END AS ExpenseType,AE.Ref_LeadId,AE.Ref_LeadActivityId,AE.Ref_OpportunityId,AE.Ref_OpportunityActivityId,AE.Ref_ExpenseCategoryId,EC.CategoryCode,EC.CategoryName,AE.Ref_TransportModeId,TM.ModeCode,TM.ModeName,AE.ExpenseDate,AE.Amount,AE.Description,AE.FromLocation,AE.ToLocation,AE.CreatedOn,AE.CreatedBy,AE.UpdatedOn,AE.UpdatedBy,
        JSON_QUERY(ISNULL((SELECT AA.AttachmentId,AA.Ref_ActivityId,NULL AS Ref_OpportunityActivityId,AA.FileName,AA.FilePath,AA.FileType,AA.FileSize,AA.CreatedOn,AA.CreatedBy,AA.Ref_ActivityExpenseId
        FROM CRM_LeadActivityAttachment AA
        WHERE AA.Ref_ActivityExpenseId =AE.ActivityExpenseId
        FOR JSON PATH),'[]')) AS LeadAttachmentsJson,
        JSON_QUERY(ISNULL((SELECT AA.AttachmentId,NULL AS Ref_ActivityId,AA.Ref_OpportunityActivityId,AA.FileName,AA.FilePath,AA.FileType,AA.FileSize,AA.CreatedOn,AA.CreatedBy,AA.Ref_ActivityExpenseId
        FROM CRM_OpportunityActivityAttachment AA
        WHERE AA.Ref_ActivityExpenseId =AE.ActivityExpenseId
        FOR JSON PATH),'[]')) AS OpportunityAttachmentsJson
        FROM CRM_ActivityExpense AE
        LEFT JOIN CRM_ActivityExpenseCategory EC ON EC.ExpenseCategoryId =AE.Ref_ExpenseCategoryId
        LEFT JOIN CRM_ActivityTransportMode TM ON TM.TransportModeId =AE.Ref_TransportModeId
        WHERE AE.IsActive =1
            AND(AE.Ref_LeadId =@JourneyLeadId
                OR(AE.Ref_OpportunityId IS NOT NULL
                    AND EXISTS(
                        SELECT 1
                        FROM CRM_Opportunity O
                        WHERE O.OpportunityId =AE.Ref_OpportunityId
                            AND O.Ref_LeadId =@JourneyLeadId
                            AND(@JourneyOpportunityId IS NULL OR O.OpportunityId =@JourneyOpportunityId)
                    )
                )
            )
        ORDER BY AE.ExpenseDate ASC,AE.ActivityExpenseId DESC;
        RETURN;
    END;

    RAISERROR('Invalid report status.',16,1);
END;
GO
