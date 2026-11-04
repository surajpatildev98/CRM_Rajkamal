# CLAUDE.md

Follow [AGENTS.md](AGENTS.md) and read [BRAIN.md](BRAIN.md) before any task. These rules add to AGENTS.md and never override it.

## Ponytail mode (always on, level: FULL)
Use the installed Ponytail skill at **full** intensity for every coding task.

1. Understand the existing code and the complete workflow (controller → repository → SP → callers, notifications) before changing anything.
2. Prefer, in order: existing code/helpers/services/SPs in this repo → .NET / SQL Server built-in features → already-installed NuGet packages. Never add a dependency for a few lines.
3. No unnecessary abstractions, wrappers, duplicate code, extra files, options or "for later" code. No over-engineering.
4. Make the smallest reliable change that fully solves the actual problem, including every caller it affects.
5. Never remove required validation, security (auth, ownership/permission checks), error handling, or existing business logic just to reduce code.
6. Keep the existing CRM architecture and conventions (`Controller → I<Module>Repository → IDatabaseService → USP_* @Status → ApiResponse`).
