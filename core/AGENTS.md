# Vaultly Core

## Scope
- `./core` holds what every domain service (Accounts, Transactions, Budgets, Goals, Alerts, Billing, Admin) and the
  identity service (`./auth`) build on, referenced with a relative `ProjectReference`:
  - `Vaultly.Core` — tactical DDD building blocks: `AggregateRoot`, `DomainEvent`, `DomainException`, `IUnitOfWork`.
    Pure C# (MediatR only), so a `Domain` project may reference it.
  - `Vaultly.Core.Authentication` — `AddInternalTokenAuthentication`: a service accepts only the internal tokens the
    gateway (`./gateway`) issued for it (issuer = gateway, audience = the service's cluster id, keys from the
    gateway's `/.well-known/jwks.json`); every endpoint requires one unless it is `AllowAnonymous`. Claims and roles
    are read with `Vaultly.Identity.PublicTypes` (`IdentityClaims`, `UserRole`). Only `Api` projects reference it.
- No business logic here. A type belongs here only when several services need the very same thing.
- Domain services live in their own top-level folders named after what they do (e.g. `./accounts`), never under
  `./core`. `Vaultly.Core*` references no service project except `Vaultly.Identity.PublicTypes`, which itself
  references nothing, so any service (Identity included, it uses `Vaultly.Core`) may use core without a cycle.
- Identity is a separate service (`./auth`); services never reference its projects, only
  `Vaultly.Identity.PublicTypes` (and `Vaultly.Identity.ApiContracts` when they call its API).

## Commands
- from `core/src`: `dotnet build Vaultly.Core.slnx` — build
- from `core/src`: `dotnet test Vaultly.Core.slnx` — run tests
- A change here changes every domain service: build and test them in the same change.

## Architecture of a domain service: tactical DDD + CQRS
- Every service is a bounded context split into `Domain`, `Application`, `Infrastructure`, `Api` projects
  (`<service>/src/Vaultly.<Name>.<Layer>`), plus `*.Domain.Tests` and `*.Application.Tests`.
- **Domain** (tactical DDD): aggregates, entities, value objects, domain events, domain services, domain exceptions.
  Pure C#, no framework dependencies except `Vaultly.Core`.
- **Application** (CQRS):
  - `Messaging/` — `ICommand`, `ICommand<T>`, `IQuery<T>`, `ICommandHandler<..>`, `IQueryHandler<..>` (MediatR underneath).
  - `Commands/<UseCase>/` — `<UseCase>Command.cs` + `<UseCase>Handler.cs`; write side, uses repositories + `IUnitOfWork`.
  - `Queries/<UseCase>/` — `<UseCase>Query.cs` + `<UseCase>Handler.cs`; read side, uses `Interfaces/ReadServices`, returns DTOs.
  - `Interfaces/Repositories` (write ports), `Interfaces/ReadServices` (read ports), `Interfaces/Services` (other ports).
- **Infrastructure**: EF Core repositories (tracked aggregates), read services (`AsNoTracking` projections), messaging, external clients.
- **Api**: minimal API endpoints send commands/queries through `ISender`; composition root only; authentication via
  `Vaultly.Core.Authentication`. The service is reached only through the gateway (add its route and cluster to the
  gateway's `ReverseProxy` section; the cluster id is the service's `InternalTokens:Audience`).
- Never use raw `IRequest`/`IRequestHandler` in Application code, never let a query write state,
  never let a query handler depend on a repository or `IUnitOfWork`. Add a `CqrsConventionTests` fixture to each
  new service (reference implementation: `auth/src/Vaultly.Identity.Application.Tests/CqrsConventionTests.cs`).
- Full rules and templates: `ddd` skill (`/.claude/skills/ddd`).

## C# Instructions
- Latest C# (currently C# 14), `net10.0`, nullable reference types enabled, primary constructors.
- Comments: follow the root `AGENTS.md` rule — minimal; `<summary>` only on public API types/members where it adds
  information; inline comments only for genuinely non-obvious reasons.
- Test-driven development; `test-writer` skill (NUnit).
- Always build and run tests after changing code here or in a domain service.

## Logging and Security
- Log with Serilog.
- Never log raw tokens, cookie values, secrets or private keys from configuration.
