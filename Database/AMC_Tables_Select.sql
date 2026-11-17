/* AMC module - quick SELECTs for all created tables */
SELECT * FROM dbo.MST_AmcDuration        ORDER BY SortOrder;
SELECT * FROM dbo.MST_AmcCoverageService ORDER BY SortOrder;
SELECT * FROM dbo.AMC_NumberCounter;
SELECT * FROM dbo.AMC_Contract           ORDER BY AmcContractId DESC;
SELECT * FROM dbo.AMC_ContractItem       ORDER BY AmcContractItemId DESC;
SELECT * FROM dbo.AMC_ContractCoverage   ORDER BY AmcContractCoverageId DESC;
SELECT * FROM dbo.AMC_ContractLog        ORDER BY AmcContractLogId DESC;
SELECT TicketId, TicketNo, CustomerName, ServiceMode, Status, Ref_AmcContractId, AmcCoverageStatus
FROM dbo.TKT_Ticket ORDER BY TicketId DESC;   -- new AMC link columns
