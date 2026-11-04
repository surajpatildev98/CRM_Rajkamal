# Project Status

Snapshot taken on 2026-11-04 from branch `feature/schedular`, based on a read-only study. The confirmed facts below come from the source code and the Live SQL script. Everything under "Observations" is interpretation and has **not** been fixed.

## 1. Module implementation status (observed)

| Module | Status | Notes |
|---|---|---|
| Auth (login, one device per user, OTP reset) | Implemented | No lockout on failed attempts (TODO). No refresh token. No JWT revocation. |
| Permission engine / Permission manager | Implemented | Admin gate is a hard-coded RoleId 1 (marked TEMP). The cache is never invalidated. |
| Master data | Implemented | `GET_TEAMS` is not handled by the Live SP. |
| Lead | Implemented | Most complete module. |
| Opportunity | Implemented | – |
| Quotation / ODA | Implemented | `LOG_NEGOTIATION_ROUND` is not handled by the Live SP. `review-decision` has no permission check. |
| Freight policy | Implemented | No permission checks. |
| Ticket | Implemented | No permission checks. Ignores the SP Status column. |
| Call log | Implemented | No permission checks. |
| Dashboard | Implemented | `GET_HRMS_DASHBOARD_FULL` (`hrms-details`) is not handled by the Live SP. |
| Reports | Implemented | No checks. EmployeeId comes from the request body. |
| Employee | Implemented | Two registration paths (legacy `Register` and `NewRegister`). |
| Attendance | Implemented with a defect | `GetRoleScope()` always returns ALL. `GET_IS_MANAGER` is not handled by the Live SP. MAPPED scope is not implemented. |
| Leave | Partially implemented | Push notification goes to the manager on apply only. HR notification is a TODO. No notification on approve, reject or cancel. No document upload. |
| Leave policy | Implemented | – |
| Probation | Implemented | Confirm runs the database step before validating the PDF. The `fail` endpoint is marked "not used" but is exposed. |
| Shift, Holiday | Implemented | Little validation. |
| Announcement | Implemented | **No `[Authorize]`.** ScheduledOn is not acted on. Attachments are stored as path strings only. |
| Scheduler (LEAD / PROBATION / BIRTHDAY jobs) | Implemented | See the issues listed below. |
| Resignation / notice period | **Schema only** | Tables exist, but no SP or C# code uses them. |
| Customer (CUS_*) | Partial | Written or linked through opportunity close (Requires Verification). No API of its own. |

## 2. Confirmed defects or mismatches

1. The `if` in `AttendanceController.cs:51` is swallowed by the comment on that line, so every user gets ALL attendance scope.
2. C# sends four `@Status` codes that the Live SPs do not handle: `GET_HRMS_DASHBOARD_FULL`, `GET_IS_MANAGER`, `GET_TEAMS` and `LOG_NEGOTIATION_ROUND`. Verify them against the DEV/UAT database.
3. `ICRMAuthorizationService` is registered twice in DI (`Program.cs`). This is harmless.
4. `PermissionService.InvalidateCache` is never called, so permission changes can lag by up to 15 minutes.
5. In the birthday job, the birthday-wish email to the employee is nested inside the HR-manager branch (`SchedulerExecutionService.cs:~594-640`).
6. `CreateTicket` and `UpdateTicketStatus` ignore the SP `Status` column. Probation Extend, Fail and Cancel ignore it as well.
7. `ConfirmProbation` confirms in the database before it validates the PDF.

## 3. Observations and risks (not fixed)

### Security

- **Secrets in tracked files.** `appsettings*.json` files and some `.pubxml` files are tracked in git. `appsettings.json` contains the JWT key and connection strings, including a commented-out plaintext credentialed connection string.
- **Anonymous endpoints.** `AuthController` ForceLogout and Logout, and the whole `AnnouncementController`, can be called without logging in.
- **IDOR exposure.** Several endpoints take the employee ID from the request body or query string instead of the token: Leave, Attendance ManualPunch, GetEmployeeDetails (which returns bank data), Holiday, Reports, Ticket detail, CallLog and Quotation details.
- **Missing permission checks.** No checks on FreightPolicy, Ticket, CallLog, Reports, review-decision, the ODA endpoints, or HRMS create/update/delete operations.
- **Uploads are public.** `/Uploads` is served before authentication. The Opportunity, Quotation and Probation upload helpers have no extension or size whitelist and use the raw file name, which allows path traversal (Requires Verification).
- **Raw errors returned.** `ex.Message`, including SQL errors, is returned to clients.
- **Open email relay.** `EmailTestController` lets any authenticated user send arbitrary email.
- **Account probing.** Login ID enumeration is possible through forgot_password. There is no brute-force protection on login.
- **Device rule bypass.** The one-device rule can be bypassed by omitting `DeviceId`.
- **Shared database login.** The DB script grants `db_owner` to the `WmsSql` login.
- **Plaintext SMTP password.** `MST_EmailConfiguration.Password` may be stored in plaintext (Requires Verification).
- **Data-wipe script.** `../CRM_SQL_FILES/DELETE.sql` contains unguarded `DELETE FROM` statements for all CRM data.

### Data integrity

- **Missing foreign keys.** `CRM_Opportunity` has no FKs, and many Opportunity, Customer, Leave and Attendance links are undeclared.
- **Missing indexes.** The Live script contains no secondary indexes, while the ad-hoc scripts define them (Requires Verification).
- **No transactions in C#.** Files are written before the SP runs, so a failure leaves orphan files behind.
- **No SP transactions.** `USP_ORG_Attendance` and `USP_ORG_SHIFTMANAGEMENT` write data without a transaction.
- **Client-computed leave days.** Leave `TotalDays` is calculated by the client and not checked in C#.

### Code quality / duplication

- **Duplicated upload helper.** `SaveBase64Attachment` and `BuildFileUrl` are copied in four or more repositories, each with different rules.
- **Repeated notification code.** The notification and manager-lookup code is repeated in many places.
- **Duplicated SPs and endpoints.** Two employee registration SPs and endpoints, and two overlapping employee SPs (`EmployeeManagement` and `NewEmployeeManagement`).
- **Inconsistent HTTP results.** Some endpoints return 200 with `Status=false`, others return 400. `SAVE_ROLE_ACCESS` returns a `Success` column instead of `Status`.
- **Mixed route naming.** PascalCase, kebab-case and snake_case are all used. `POST api/Opportunity/ActivityExpenseMasters` actually adds an expense.
- **Dead or commented-out code.** Old ConfirmProbation, DecideManagementReview, ReviseQuotation, GetEmployeeShift, the old GetRoleScope and the `_Old` SP branches.
- **Unused packages.** AutoMapper and EF Core are referenced but never used.
- **Misplaced or unscoped files.** `AuthorizationService.cs` lives in `Repository/` but uses the Services namespace. `LeavePolicyController` has no namespace.
- **Cross-module dependency.** HRMS (Leave) depends on CRM (`ILeadRepository`) for the manager lookup.
- **Path base mismatch.** The Firebase path uses `Directory.GetCurrentDirectory()`, while templates use `AppContext.BaseDirectory`.

## 4. Pending verification / further investigation

- Logic inside the SPs:
  - Lead and opportunity status transitions, won-lead eligibility, and the effect of CLOSE_OPPORTUNITY on `CustomerId`.
  - What triggers a management review, quotation version numbering, and whether a revision re-evaluates ODA.
  - Leave balance, overlap and sandwich rules, and the leave status codes.
  - The attendance geo-fence and late-arrival rules.
  - Probation status codes.
  - How the scheduler computes NextRunOn and how job items are created.
- Whether the four unhandled `@Status` codes exist in the DEV/UAT database.
- Whether indexes exist on the real database.
- Runtime behaviour of `/Uploads` access without a token.
- The `../Frontend` project (Vite/TypeScript), which was not analyzed.
- Whether AutoMapper and the EF Core packages are needed at all.
- Code paths that call SP codes through variables instead of string literals.
