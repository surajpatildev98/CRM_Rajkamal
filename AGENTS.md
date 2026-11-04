# AGENTS.md — Instructions for Codex / Claude Code

This repo is **CRMWebServices**: an ASP.NET Core (.NET 8) Web API for a CRM and HRMS, backed by SQL Server stored procedures. There is no frontend in this repo.

## Before any task
1. **Read [BRAIN.md](BRAIN.md) first.** It holds the architecture, module index, auth model, SP catalogue, workflows, business rules and known risks.
2. Find your module in BRAIN.md §3. Open **only** that module's controller, repository, interface and models, plus any shared code they call.
3. Do not run repository-wide exploration unless BRAIN.md is missing the information you need. If it is, add that information to BRAIN.md afterwards.

## Architecture to follow
`Controller → I<Module>Repository → IDatabaseService → USP_<AREA>_<Module>` (switched on `@Status`) → `ApiResponse {status, message, data}`.
- Add behaviour as a new `@Status` branch, repository method, interface method, controller action and model. Register new repositories in `Program.cs`.
- Send all notifications through `ISchedulerExecutionService`, wrapped in try/catch (best-effort).
- Accept files as Base64, validated like `LeadRepository.SaveBase64Attachment` (extension allow-list, size limit, sanitized name).

## Non-negotiable rules
- The acting employee always comes from `User.EmployeeId()` (JWT claim `EmployeeId`). Never take the actor from the request body. Do not use `Helpers/UserContext.cs` or `IsInRole`.
- Every Lead or Opportunity action calls `ICRMAuthorizationService.CanPerformAction(User, "Lead"|"Opp", action, id)`. Never bypass or weaken ownership and permission checks.
- Reuse existing services, SPs, `@Status` values, DTOs and tables (BRAIN.md §8–§9) before creating new ones. Do not duplicate business logic.
- Do not change DB structure or SPs without explicit user approval. SP bodies are not in the repo, so ask for the current definition first. Never reorder SP result sets: the C# mapping is positional.
- Do not modify unrelated modules. Check all callers before changing shared code (`AuthorizationService`, `PermissionService`, `DatabaseService`, `SchedulerExecutionService`, helpers).
- Preserve the business workflows in BRAIN.md §6 and §11 unless the task explicitly changes them.
- Never print, log or commit secrets (connection strings, JWT key, Firebase JSON, SMTP credentials).

## After a change
- Run `dotnet build`. Exercise the endpoint via Swagger (Development profile) when possible.
- Update **BRAIN.md** (and BRAIN_PROGRESS.md) if you changed APIs, `@Status` values, permissions, DB relationships or workflows.
- Label facts honestly: VERIFIED, INFERRED, NOT VERIFIED or NEEDS INVESTIGATION. Never present assumptions as verified.
