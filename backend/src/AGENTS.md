## Scope
 - `./backend` hosts the domain microservices (Bank Sync, Transaction, Budget, Goal, Alert, Billing, Admin) and their `Vaultly.SharedKernel`.
 - Authentication/identity is **not** here — it is a fully separate service in `./auth` (see `./auth/AGENTS.md`).
   Backend services never reference `./auth` projects; they only validate its JWTs via
   `/.well-known/openid-configuration` + `/.well-known/jwks.json` and consume its integration events.

## Commands
 - from backend/src directory `dotnet build` - build solution
 - from backend/src directory `dotnet test` - run tests for solution

## Skills
 - all skills are in `./.agents/skills` directory

## Architecture: tactical DDD + CQRS
 - Every service is a bounded context split into `Domain`, `Application`, `Infrastructure`, `Api` projects
   (`services/<name>/Vaultly.<Name>.<Layer>`), plus `*.Domain.Tests` and `*.Application.Tests`.
 - **Domain** (tactical DDD): aggregates, entities, value objects, domain events, domain services, domain exceptions.
   Pure C#, no framework dependencies except `Vaultly.SharedKernel`.
 - **Application** (CQRS):
   - `Messaging/` — `ICommand`, `ICommand<T>`, `IQuery<T>`, `ICommandHandler<..>`, `IQueryHandler<..>` (MediatR underneath).
   - `Commands/<UseCase>/` — `<UseCase>Command.cs` + `<UseCase>Handler.cs`; write side, uses repositories + `IUnitOfWork`.
   - `Queries/<UseCase>/` — `<UseCase>Query.cs` + `<UseCase>Handler.cs`; read side, uses `Interfaces/ReadServices`, returns DTOs.
   - `Interfaces/Repositories` (write ports), `Interfaces/ReadServices` (read ports), `Interfaces/Services` (other ports).
 - **Infrastructure**: EF Core repositories (tracked aggregates), read services (`AsNoTracking` projections), messaging, external clients.
 - **Api**: minimal API endpoints send commands/queries through `ISender`; composition root only.
 - Never use raw `IRequest`/`IRequestHandler` in Application code, never let a query write state,
   never let a query handler depend on a repository or `IUnitOfWork`. Add a `CqrsConventionTests` fixture to each
   new service (reference implementation: `auth/src/Vaultly.Identity.Application.Tests/CqrsConventionTests.cs`).
 - Full rules and templates: `ddd` skill.

## C# Instructions
- Always use the latest version C#, currently C# 14 features.
- Keep all projects targeting `net10.0` with nullable reference types enabled, matching the existing service and test projects.
- Prefer primary constructors
- Write clear and concise comments for each function.
- Use test-driven Development
- Use ddd skill when creating new C# files or refactoring existing ones

## General Instructions
- Make only high confidence suggestions when reviewing code changes.
- Write code with good maintainability practices, including comments on why certain design decisions were made.
- Handle edge cases and write clear exception handling.
- For libraries or external dependencies, mention their usage and purpose in comments.
- Always build and run tests after changes to backend functionality. Do not run build or tests if changes made do not effect backend functionality.

## Logging and Monitoring
- Write usefull logs using Serilog
- Never log raw tokens, cookie values, secrets or private keys from configuration.

## Testing
- Use `test-writer` skill when creating tests.
