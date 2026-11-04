# BRAIN_PROGRESS.md — Analysis tracker for BRAIN.md

**Last run:** 2026-11-03 · **Branch:** `feature/newFeature` (working tree includes uncommitted Payslip/Resignation/Regularization files)

## Method
- Static source analysis of every controller, repository, interface, service, helper and model, done in 5 module batches (Lead+CallLog; Opportunity; Quotation+Freight; Auth+Permission+MasterData+Dashboard+Reports+Ticket+EmailTest; HRMS+Scheduler+Email+Firebase), plus a direct review of Program.cs, Helpers, Data, Services/DatabaseService and config.
- **DB access:** a read-only catalog query (sys.objects / sql_modules) was attempted and was **denied by the session's permission policy**. No SP bodies or table DDL were read, except `Database/AttendanceRegularization.sql`.

## Analyzed folders
| Folder | Status |
|---|---|
| Controllers/ (21 files) | ✅ all |
| Repository/ (23 files) | ✅ all |
| Interfaces/ (29 files) | ✅ module interfaces read alongside their repositories |
| Services/ (8 files) | ✅ all |
| Helpers/ (6 + UserContext) | ✅ all |
| Data/ | ✅ |
| Models/ (all subfolders) | ✅ request/response models for every module |
| Database/ | ✅ AttendanceRegularization.sql (the only SQL file) |
| Templates/Email | ✅ listed and template mapping traced |
| Properties/, appsettings*.json, .gitignore, csproj | ✅ (secrets intentionally not recorded) |
| Uploads/, Logs/, bin/, obj/, wwwroot/ | ⏭ runtime and data only; not analyzed |
| Frontend | ❌ not present in this repo |

## Analyzed modules
✅ Auth · Permission engine · PermissionManager · Lead · CallLog · Opportunity · Opportunity stages · Quotation · Negotiation · Management review · ODA · Freight policy · Ticket · Dashboard · Reports · MasterData · Employee · Probation · Attendance + Regularization · Leave · LeavePolicy · Shift · Holiday · Resignation · Payslip · Announcement · Scheduler · Email · Firebase

## Counts
| Item | Count |
|---|---|
| Controllers | 21 |
| HTTP endpoints (Http* attributes) | ~207 |
| C# files analyzed | ~110 (≈30.5k lines incl. SQL) |
| Distinct stored procedures referenced | 25 (24 `USP_*` + `usp_Resignation_Manage`) |
| SPs with source in repo | 1 (`USP_ORG_AttendanceRegularization`) |
| Tables verified from DDL | 1 created (`ORG_AttendanceRegularization`) + 6 referenced ORG tables + 1 view |
| CRM tables | ~20 entities INFERRED from columns; 0 physical names verified |
| Major workflows documented | 8 (login/JWT, lead lifecycle, lead→opportunity, opportunity→won/lost, quotation/negotiation, management review/ODA, generic API→DB, scheduler/notifications) + HRMS flows |

## Completed workflows
- [x] Login, device check, force logout, OTP reset
- [x] Permission check (`CanPerformAction`) and access filter
- [x] Lead create → assign/transfer → activities → status → close
- [x] Won lead → convert → opportunity (C# side)
- [x] Opportunity requirements → ticket → quotation → ODA → review → customer response → negotiation → revise → close
- [x] Scheduler polling, claiming and dispatch; email templating; FCM
- [x] Attendance regularization (full SQL)

## Pending analysis / NEEDS INVESTIGATION
1. **SP bodies.** Highest priority: `USP_CRM_OpportunityManagement` (`CONVERT_LEAD_TO_OPPORTUNITY`, stage logic, list scoping), `USP_CRM_OpportunityQuotationManagement` (stage moves, totals, ODA rule), `USP_CRM_LeadManagement` (list scoping, customer matching, close), and `USP_CRM_PermissionManagement` (`GET_RESOURCE_OWNER` column mapping).
2. Physical CRM table names, PKs, FKs, indexes, triggers, views and functions (`CRM_Lead*`, `CRM_Opportunity*`).
3. Actual opportunity stage codes and order (the brief's NEW→QUOTE→PROP→NEGOTIATION→FINAL DECISION is unconfirmed).
4. Role names and permission codes stored in the DB; which roles hold which `Lead_*` / `Opp_*` codes.
5. Ticket status codes and the `QuotationEligibility` rule.
6. Whether the SPs validate parent/child ownership (follow-up, requirement, activity, quotation ids).
7. Frontend (Flutter) repo: page → API mapping.
8. `MAPPED` attendance scope (mentioned in the README, absent in C#).

## Unverified information (summary)
- All DB-side business rules (labelled INFERRED / NOT VERIFIED in BRAIN.md §5, §6 and §11).
- Role ids 6, 8, 9, 11, 15, 18, 20 (known only from code comments and checks).
- That `CRM_LeadOrganization`, `CRM_OpportunityNegotiation`, etc. are the real table names.

## Next steps for a future batch
- With DB access: run `SELECT name, type_desc FROM sys.objects WHERE is_ms_shipped=0` and `sp_helptext` on the SPs above. Then update BRAIN.md §5, §6, §8 and §11, and flip the labels.

## Update 2026-11-04 (live code, commit a91c245)
Added BRAIN.md §12a: Support Ticket module (`USP_TKT_TicketManagement`), CRM+WMS customer search (`USP_CRM_CustomerManagement`, `CUS_Customer.Ref_WmsCustomerMasterId`), dashboard `@LoggedInEmployeeId`, scheduler `DisableJobRequest`. NOT VERIFIED: live DB has these SQL scripts applied.
