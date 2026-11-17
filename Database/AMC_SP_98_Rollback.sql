/* Rollback for AMC_SP_02_UAT.sql - drops the SP and the AMC module permissions. Run before AMC_Tables_99_Rollback.sql. */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ModuleId INT = (SELECT ModuleId FROM dbo.ORG_Module WHERE ModuleCode = 'AMC');
DELETE o  FROM dbo.ORG_EmployeePermissionOverride o JOIN dbo.ORG_Permission p ON p.PermissionId = o.Ref_PermissionId WHERE p.Ref_ModuleId = @ModuleId;
DELETE rp FROM dbo.ORG_RolePermission rp JOIN dbo.ORG_Permission p ON p.PermissionId = rp.Ref_PermissionId WHERE p.Ref_ModuleId = @ModuleId;
DELETE FROM dbo.ORG_Permission        WHERE Ref_ModuleId = @ModuleId;
DELETE FROM dbo.ORG_RoleModuleAccess  WHERE Ref_ModuleId = @ModuleId;
DELETE FROM dbo.ORG_SuperModuleMapping WHERE ModuleId = @ModuleId;
DELETE FROM dbo.ORG_Module            WHERE ModuleId = @ModuleId;
COMMIT;
DROP PROCEDURE IF EXISTS dbo.USP_AMC_ContractManagement;
