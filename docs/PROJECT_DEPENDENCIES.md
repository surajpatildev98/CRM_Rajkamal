# Project Dependencies

## Runtime / framework

| Item | Value | Source |
|---|---|---|
| SDK | `Microsoft.NET.Sdk.Web` | `CRMWebServices.csproj` |
| Target | **net8.0** | csproj |
| Nullable | disabled; warning 8632 suppressed | csproj |
| ImplicitUsings | enabled | csproj |
| Solution | `CRMWebServices.slnx` (single project) | root |
| Local tool | `dotnet-ef` (tool manifest in `dotnet-tools.json` and `.config/dotnet-tools.json`) | root |

## NuGet packages (csproj)

| Package | Version | Used for |
|---|---|---|
| AutoMapper | 16.2.0 | Referenced. **No usage verified** (Requires Verification) |
| BCrypt.Net-Next | 3.1.0 | Hashing passwords and OTPs |
| FirebaseAdmin | 3.6.0 | FCM push (`Services/FirebaseNotificationService.cs`) |
| Google.Apis.Auth | 1.75.0 | Firebase credential loading |
| MailKit | 4.17.0 | SMTP email (`Services/EmailService.cs`) |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.0 | JWT authentication |
| Microsoft.Data.SqlClient | 7.0.2 | ADO.NET SQL Server access |
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.0 | Referenced. **No DbContext found**; data access is ADO.NET only |
| Microsoft.EntityFrameworkCore.Tools | 9.0.0 | Design-time only |
| Newtonsoft.Json | 13.0.4 | JSON serialization of child collections sent to SPs; dynamic parsing at login |
| Serilog.AspNetCore / Exceptions / Sinks.Console / Sinks.File | 8.0.0 / 8.4.0 / 6.1.1 / 7.0.0 | Logging |
| Swashbuckle.AspNetCore | 6.6.2 | Swagger UI with a Bearer security definition |

Note: EF Core 9 packages sit on a net8.0 target. EF Core is not used at runtime as far as the code shows.

## External services

| Service | How it is configured | Code |
|---|---|---|
| SQL Server (DB `Rkl_EnterpriseSuiteDB`, see DATABASE_REFERENCE) | `ConnectionStrings:DefaultConnection` | `Data/DBConnection.cs`, `Services/DatabaseService.cs` |
| SMTP | **Stored in the database**: `USP_CRM_EmailConfiguration` `GET_ACTIVE` (Host, Port, SSL→StartTls, Email, Password, FromEmail, FromName) | `Repository/EmailConfigurationRepository.cs`, `Services/EmailService.cs` |
| Firebase Cloud Messaging | `Firebase/firebase-service-account.json`, loaded from `Directory.GetCurrentDirectory()`; the constructor throws if it is missing | `Services/FirebaseNotificationService.cs` (singleton) |
| Mobile deep link | `rajkamalcrm://oda-approval?quotationId=` inside ODA emails | `Repository/OpportunityQuotationRepository.cs` |

## Configuration keys (names only, values not reproduced)

| Key | Used by |
|---|---|
| `ConnectionStrings:DefaultConnection` | DBConnection |
| `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes` (1440) | Program.cs, JwtHelper |
| `EmailSettings:CompanyLogoPath` | EmailService (inline `cid:companylogo`) |
| `EmailSettings:PublicBaseUrl` | read in SchedulerEmailComposer but unused |
| `CRMSettings:BaseUrl` | appears only in commented-out code |
| `Logging:*`, `AllowedHosts` | framework |

## Environments

| Environment | File | Notes |
|---|---|---|
| Development | `appsettings.Development.json` | logging only |
| InternalNetwork | `appsettings.InternalNetwork.json` | connection string override |
| OutsideNetwork | `appsettings.OutsideNetwork.json` | connection string override |

- Swagger is enabled in Development, InternalNetwork and OutsideNetwork (`Program.cs`).
- Launch profiles (`Properties/launchSettings.json`) use https://localhost:7022 and http://localhost:5163, plus IIS Express on port 33163.
- Publish profiles: `Properties/PublishProfiles/FolderProfile*.pubxml`. These are FileSystem, Release publishes to local `D:\` paths, and one points at another developer's path.

## Build and output requirements

- `Templates\**\*.html` are copied to the output (PreserveNewest) and loaded from `AppContext.BaseDirectory/Templates/Email`.
- `Firebase\firebase-service-account.json` is copied to the output and publish folders.
- `wwwroot\assets\companylogo.jpg` is always copied.
- The `Uploads/` folder is created at startup under the ContentRoot and served at `/Uploads`.
- `Logs/crm-log-.txt` is a daily rolling log with 30 files retained.

## Related projects (outside this repository)

| Path | Content |
|---|---|
| `../CRM_SQL_FILES` | SQL scripts, including `Live/Rkl_EnterpriseSuiteDB_Live.sql` (schema script), ad-hoc migration scripts and a UAT `.bak` |
| `../Frontend` | Vite + TypeScript web front end. **Not analyzed in this study.** |
| `../CRMWebServices_Publish_1`, `../CRMWebServices_120826-W.zip` | publish output and archive (not analyzed) |

The README states that the API is consumed by the Android/Flutter apps. CORS policy `AllowFlutterWeb` allows any origin, header and method.
