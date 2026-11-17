/* Rollback for AMC_Tables_01_UAT.sql - drops all AMC tables and the TKT_Ticket AMC columns. */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRAN;

IF COL_LENGTH('dbo.TKT_Ticket', 'Ref_AmcContractId') IS NOT NULL
BEGIN
    DROP INDEX IF EXISTS IX_TKT_Ticket_AmcContract ON dbo.TKT_Ticket;
    ALTER TABLE dbo.TKT_Ticket DROP CONSTRAINT IF EXISTS CK_TKT_Ticket_AmcCoverageStatus, FK_TKT_Ticket_AmcContract;
    ALTER TABLE dbo.TKT_Ticket DROP COLUMN AmcCoverageStatus, Ref_AmcContractId;
END

DROP TABLE IF EXISTS dbo.AMC_ContractLog;
DROP TABLE IF EXISTS dbo.AMC_ContractCoverage;
DROP TABLE IF EXISTS dbo.AMC_ContractItem;
DROP TABLE IF EXISTS dbo.AMC_Contract;
DROP TABLE IF EXISTS dbo.AMC_NumberCounter;
DROP TABLE IF EXISTS dbo.MST_AmcCoverageService;
DROP TABLE IF EXISTS dbo.MST_AmcDuration;

COMMIT;
