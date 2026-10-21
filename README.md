# CRMWebServices

Backend REST API for the **Rajkamal Business Management Platform**. It combines a **CRM** (leads → opportunities → quotations → tickets) with an **HRMS** (employees, attendance, leave, probation, announcements) and is consumed by the Android/Flutter apps.

Built with **ASP.NET Core (.NET 8)** and **SQL Server** (stored-procedure driven), secured with **JWT**, and backed by a background scheduler that sends **Firebase push** and **email** notifications.

---

## Table of Contents

- [Features](#features)
- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Authentication and Authorization](#authentication-and-authorization)
- [API Modules](#api-modules)
- [Scheduler and Notifications](#scheduler-and-notifications)
- [File Uploads](#file-uploads)
- [Logging](#logging)
- [Coding Conventions](#coding-conventions)
- [Deployment](#deployment)
- [Known Notes / TODO](#known-notes--todo)

---

## Features

### CRM
- **Leads**: create, assign, transfer, close, activities, follow-ups, expenses, requirements, attachments
- **Existing customer detection** by GST / PAN / name, with branch and contact reuse
- **Opportunities**: convert won leads, stages, requirements, activities, follow-ups, expenses, close (won/lost)
- **Quotations**: raise, revise (versioned), customer response, negotiation rounds, management review
- **Freight policy**: versioned slab-based freight, **ODA** (Out of Delivery Area) charges and approval workflow with email
- **Tickets**: raised against opportunity requirements, assigned to employees, status and activity tracking
- **Call logs** for leads and opportunities
- **Dashboards and reports**: KPI, funnel, pipeline, employee performance, full lead-to-quotation journey

### HRMS
- **Employee management**: registration with education, bank, family and document details
- **Probation**: extend, confirm (with letter PDF and email), cancel, history
- **Attendance**: manual punch with geo-location, scoped reports (ALL / MAPPED / TEAM / SELF)
- **Leave**: balance, apply, cancel, approve and reject, team leaves
- **Leave policies, shifts and holiday calendars**
- **Announcements** targeted by company, region, state, office or department (push and/or email)
- **HRMS dashboard** (org-wide, team or self scope)

### Platform
- Role and permission engine with **Super Modules → Modules → Permissions**
- **One-user-one-device** login with force-logout over Firebase
- Forgot-password flow with **OTP** (BCrypt-hashed, attempt-limited)
- Configurable **scheduler** (jobs, frequency, retry, expiry)
- Templated HTML emails with inline company logo

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core Web API, **.NET 8** |
| Database | SQL Server (ADO.NET + stored procedures) |
| Auth | JWT Bearer, BCrypt.Net-Next |
| Push | FirebaseAdmin (FCM) |
| Email | MailKit / MimeKit (SMTP config stored in DB) |
| Logging | Serilog (Console + rolling File) |
| Docs | Swagger / Swashbuckle |
| JSON | Newtonsoft.Json and System.Text.Json |
| Tooling | dotnet-ef (tool manifest present) |

---

## Architecture

```
 Android / Flutter App
         │  HTTPS + JWT
         ▼
 ┌─────────────────────┐
 │     Controllers     │  routing, permission checks, token → EmployeeId
 └─────────┬───────────┘
           ▼
 ┌─────────────────────┐
 │    Repositories     │  business flow, mapping DataTable → models, notifications
 └─────────┬───────────┘
           ▼
 ┌─────────────────────┐
 │   IDatabaseService  │  ExecuteDataTable / DataSet / Scalar / NonQuery
 └─────────┬───────────┘
           ▼
   SQL Server stored procedures  (USP_CRM_*, USP_ORG_*)
```

**Key design points**

- **Stored-procedure pattern**: each module has one SP (e.g. `USP_CRM_LeadManagement`) that takes an `@Status` parameter acting as the action switch (`CREATE_LEAD`, `GET_LEAD_LIST_FULL`, ...).
- **Uniform response envelope** for all APIs:
```json
  { "status": true, "message": "…", "data": { } }
```
- **Notifications are best-effort**: failures are logged and never roll back the business transaction.
- **Permissions are cached** in memory for 15 minutes per employee.
- **Global exception middleware** returns a safe 500 payload and logs the details.

---

## Project Structure

```
CRMWebServices/
├── Controllers/        # API endpoints (Auth, Lead, Opportunity, Quotation, Ticket, Leave, ...)
├── Repository/         # Data access + business flow per module
├── Interfaces/         # Repository and service contracts
├── Services/           # DatabaseService, PermissionService, AuthorizationService,
│                       # EmailService, FirebaseNotificationService,
│                       # SchedulerWorker / SchedulerExecutionService,
│                       # EmailTemplateRenderer / SchedulerEmailComposer
├── Models/             # Request/response models per module
├── Helpers/            # JwtHelper, ApiResponse, ClaimsPrincipalExtension,
│                       # GlobalExceptionMiddleware, RoleIds, DataTableExtensions
├── Data/               # DBConnection (SqlConnection factory)
├── Templates/Email/    # HTML email templates (_Layout + per-event templates)
├── Uploads/            # Runtime file storage (git-ignored in production)
├── Firebase/           # firebase-service-account.json (NOT committed)
├── Logs/               # Serilog rolling logs
├── Properties/         # launchSettings, publish profiles
├── appsettings*.json   # Per-environment config
└── Program.cs          # DI, auth, CORS, Swagger, middleware pipeline
```

---

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- SQL Server access to the `Rkl_EnterpriseSuiteDB` database (all `USP_*` procedures deployed)
- Firebase service account JSON (for push notifications)
- Visual Studio 2022+ or VS Code

### Run locally

```bash
git clone https://gitlab.com/rkl_products/rkl-businessmgmtplatform/backend.git
cd backend

dotnet restore
dotnet run --launch-profile "https"
```

Swagger UI: `https://localhost:7022/swagger`

### Launch profiles

| Profile | `ASPNETCORE_ENVIRONMENT` | Use |
|---|---|---|
| `https` / `http` | `Development` | Local development |
| `Internal Network` | `InternalNetwork` | Office LAN database |
| `Outside Network` | `OutsideNetwork` | Public/remote database |
| `IIS Express` | `Development` | IIS Express |

Swagger is enabled for `Development`, `InternalNetwork` and `OutsideNetwork`.

---

## Configuration

Config is layered: `appsettings.json` → `appsettings.{Environment}.json` → environment variables / user-secrets.

```jsonc
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=<host>,<port>;Database=Rkl_EnterpriseSuiteDB;User Id=<user>;Password=<password>;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "<long-random-secret>",
    "Issuer": "CRMAPI",
    "Audience": "CRMUsers",
    "ExpiryMinutes": 1440
  },
  "EmailSettings": {
    "CompanyLogoPath": "wwwroot/assets/companylogo.jpg"
  }
}
```

> ⚠️ **Never commit real credentials or the JWT key.** Use environment variables, `dotnet user-secrets`, or your deployment secret store.

**Other required files**

| File | Purpose |
|---|---|
| `Firebase/firebase-service-account.json` | FCM push notifications (git-ignored) |
| `wwwroot/assets/companylogo.jpg` | Inline logo in emails |
| SMTP settings | Stored in DB, read through `USP_CRM_EmailConfiguration` (`GET_ACTIVE`) |

---

## Authentication and Authorization

### Login flow
1. `POST /api/auth/login` validates credentials (BCrypt), the active/locked status and the **device check**.
2. If the user is already logged in on another device, the API returns `ALREADY_LOGGED_IN`. The app can then call `POST /api/auth/ForceLogout`, which pushes a `FORCE_LOGOUT` message to the old device.
3. On success a **JWT** is issued with claims `EmployeeId`, `LoginId`, `EmployeeCode`, `RoleIds`. The response also includes the user's **SuperModules → Modules → Permissions** tree for building the app menu.

### Password reset
`forgot_password` (OTP emailed) → `verify_otp` (max 5 attempts, 10-minute expiry) → `Reset_password` (reset token + password policy).

### Permission model
Permission codes follow `{Resource}_{Action}`, e.g. `Lead_View`, `Opp_RaiseQuotation`.

`ICRMAuthorizationService.CanPerformAction(user, resource, action, resourceId)` evaluates in this order:

1. `{Resource}_ViewAll` → allowed
2. Has the specific `{Resource}_{Action}` permission? If not → denied
3. `Create` → allowed (no ownership check)
4. Ownership check: creator or assigned employee, with special rules for `Transfer` and `Assign`, plus ticket-assignee access to opportunities

Admin-only areas (Permission Manager, Scheduler) check for **RoleId 1**.

---

## API Modules

All routes require `Authorization: Bearer <token>` unless noted.

| Module | Base route | Highlights |
|---|---|---|
| Auth | `/api/auth` | login, logout, force-logout, forgot/verify/reset password *(anonymous)* |
| Lead | `/api/Lead` | Register, Transfer, Assign, UpdateStatus, CreateActivity, CreateFollowUp, CloseLead, Details, requirements, expenses, customer search |
| Opportunity | `/api/opportunity` | wonleads, convert, list, details, transfer, requirements, activity, follow-up, close |
| Quotation | `/api/OpportunityQuotation` | raise-data, raise, revise, list, details, customer-response, negotiation-round, review-decision, check-oda, oda-approvals |
| Freight Policy | `/api/FreightPolicy` | Create / Update / Deactivate policy, active policy, applicable freight, delivery partners |
| Ticket | `/api/ticket` | create, my-tickets, detail, UpdateStatus, UpdateActivity, statuses |
| Call Log | `/api/CallLog` | Create, by Lead, by Opportunity |
| Dashboard | `/api/dashboard` | Details, my-dashboard, employee-performance, hrms-details |
| Reports | `/api/Reports` | dashboard, journey, filters |
| Employee | `/api/Employee` | Register, NewRegister, GetEmployeeMaster, GetEmployeeDetails, UpdateEmployee, hr-managers |
| Probation | `/api/EmployeeProbation` | employees, detail, history, extend, confirm, cancel |
| Attendance | `/api/attendance` | GetAttendance, GetPunches, ManualPunch, GetDashboardCount, reports |
| Leave | `/api/leave` | balance, apply, cancel, team leaves, approve, reject |
| Leave Policy | `/api/LeavePolicy` | Save, Update, List, Details, Leave types |
| Shift / Holiday | `/api/shift`, `/api/holiday` | CRUD for shifts, holiday calendars |
| Announcement | `/api/Announcement` | create, update, targets, recipients, send |
| Master Data | `/api/master` | dropdowns (companies, offices, roles, products, units, …) |
| Permission Manager | `/api/PermissionManager` | modules, super modules, permissions, role access *(admin only)* |
| Scheduler | `/api/Scheduler` | job CRUD, enable/disable, entity types, scopes *(admin only)* |

> Full request/response schemas are available in **Swagger**.

---

## Scheduler and Notifications

`SchedulerWorker` (a `BackgroundService`) polls every **10 seconds**, fetches due items (`GET_DUE_ITEMS`) and claims each one atomically (`CLAIM_JOB_ITEM`) so multiple instances never double-process.

| Entity type | Behaviour |
|---|---|
| `LEAD` | Reminds creator and manager while a lead is unassigned, rescheduling until assigned |
| `EMPLOYEE_PROBATION` | Probation-ending reminder to manager and HR |
| `EMPLOYEE_BIRTHDAY` | Birthday reminder to manager/HR and a wish email to the employee |

**Event-driven notifications** (lead/opportunity assignment, ticket updates, quotations, leave, announcements) go through the same `SchedulerExecutionService`:

- **Push**: Firebase FCM to the employee's active device
- **Email**: rendered from `Templates/Email/*.html` wrapped in `_Layout.html`

**Email templating:** `{{Token}}` is HTML-encoded, `{{{Token}}}` is raw HTML, and `{{#if Key}}…{{/if}}` renders conditionally. `SchedulerEmailComposer` maps action types (e.g. `PROBATION_EXPIRY`, `BIRTHDAY_WISH`, `ANNOUNCEMENT`) to templates.

---

## File Uploads

Attachments are sent as **Base64** and saved under `Uploads/<Folder>/`:

| Folder | Content |
|---|---|
| `LeadAttachments` | Lead documents |
| `LeadActivityAttachments` | Activity and expense receipts |
| `OpportunityActivityAttachments` / `OpportunityActivityExpenseAttachments` | Opportunity activity files |
| `OpportunityQuotationAttachments` | Quotation PDFs |
| `EmployeeDocuments` | Employee docs and probation confirmation letters |

- Allowed types: `.pdf .doc .docx .xls .xlsx .jpg .jpeg .png`, max **10 MB**
- Files are served statically at `/Uploads/<folder>/<file>`

---

## Logging

Serilog writes to the console and to `Logs/crm-log-YYYYMMDD.txt` (daily rolling, 30 files retained). HTTP requests are logged with status and duration, and unhandled exceptions are captured by `GlobalExceptionMiddleware`.

---

## Coding Conventions

- **Pattern**: `Controller → IRepository → IDatabaseService → Stored Procedure`
- Every repository method returns `ApiResponse` (or a typed response) and catches `SqlException` and `Exception` separately
- Always get the acting user from the token (`User.EmployeeId()`) and **never trust `CreatedBy` from the request body**
- Check permissions in the controller before calling the repository
- Wrap notification calls in their own `try/catch`
- Use `#region` blocks per operation inside repositories
- DB columns use the `Ref_` prefix for foreign keys; SPs return `Status` and `Message` for validation results

### Adding a new endpoint

1. Add the `@Status` branch to the stored procedure
2. Add request/response models under `Models/`
3. Add the method to the `I…Repository` interface and its implementation
4. Add the controller action with `[Authorize]` and a permission check
5. Register the repository in `Program.cs` if it is new (`AddScoped`)

---

## Deployment

Visual Studio publish profiles live in `Properties/PublishProfiles/` (framework-dependent, `net8.0`, FileSystem target).

```bash
dotnet publish -c Release -o ./publish
```

On the server:
- Set `ASPNETCORE_ENVIRONMENT` (`InternalNetwork` or `OutsideNetwork`)
- Provide `Firebase/firebase-service-account.json` and the logo asset
- Ensure `Uploads/` and `Logs/` are writable
- Inject the connection string and JWT key through environment variables

---

## Known Notes / TODO

- Rotate and move all credentials (DB password, JWT key) out of source control
- `AnnouncementController` is currently missing `[Authorize]`
- Employee details response should be reviewed for fields that should not be exposed
- Invalidate the permission cache when role permissions change (see `PermissionManagerController` TODOs)
- HR Manager notification on leave application is pending
- Align `dotnet-ef` tool version (`10.x`) with the EF Core packages (`9.0.0`)

---

## Maintainers

Rajkamal Barscan Systems Pvt. Ltd. – Product Development Team
