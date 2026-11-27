# Vaultly Core

## Scope
- `./core` holds what every domain service (Accounts, Transactions, Budgets, Goals, Alerts, Billing, Admin) and the
  identity service (`./auth`) build on, referenced with a relative `ProjectReference`:
  - `Vaultly.Core` — tactical DDD building blocks: `AggregateRoot`, `DomainEvent`, `DomainException`, `IUnitOfWork`;
    CQRS markers in `Vaultly.Core.Messaging` (`ICommand`, `ICommand<T>`, `IQuery<T>`, `ICommandHandler<..>`,
    `IQueryHandler<..>`, MediatR underneath); `WireNames` (the `snake_case` spelling of enums outside the code).
    Pure C# (MediatR only), so a `Domain` project may reference it.
  - `Vaultly.Core.Persistence` — EF Core: `UnitOfWork<TDbContext>` (saves, then publishes the saved aggregates'
    domain events), `MigrateDatabaseAsync<TDbContext>()`, `WireNameConverter<TEnum>` (enum columns as `WireNames`).
    Only `Infrastructure` projects reference it.
  - `Vaultly.Core.Authentication` — `AddInternalTokenAuthentication`: a service accepts only the internal tokens the
    gateway (`./gateway`) issued for it (issuer = gateway, audience = the service's cluster id, keys from the
    gateway's `/.well-known/jwks.json`); every endpoint requires one unless it is `AllowAnonymous`. Claims and roles
    are read with `Vaultly.Identity.PublicTypes` (`IdentityClaims`, `UserRole`). Only `Api` projects reference it.
  - `Vaultly.Core.Testing` — for test projects only: `CqrsConventions` (derive a `[TestFixture]` from it in each
    service's application tests) and `TestGateway` (serves a signing key as JWKS and issues internal tokens, so API
    tests run the real authentication).
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
  Pure C#, no framework dependencies except `Vaultly.Core`. Grouped by domain concept, never by kind: one folder per
  aggregate, named in the plural (`Accounts/`, `Users/`; a folder named like its class would clash with it), holding
  the aggregate, its entities, value objects, enums, events, exceptions and its repository port
  (`I<Aggregate>Repository`), one type per file. A type several concepts use sits with the concept that owns it;
  only domain-wide basics (e.g. `InvalidValueException`) sit in the project root. No `Aggregates/`, `ValueObjects/`,
  `Events/`, `Exceptions/` folders. When an aggregate folder grows, its parts get sub-folders by sub-concept — an
  entity with its id, value objects, exceptions and events (`Sessions/RefreshTokens/`, `Users/Passwords/`) — while
  the root, its id and repository stay at the top. A separate aggregate is never nested. Inside any concept folder,
  two or more domain events go to `Events/` and two or more domain exceptions to `Exceptions/` (a single one stays
  beside the type that raises it).
- **Application** (CQRS):
  - Commands and queries implement the markers from `Vaultly.Core.Messaging`.
  - `Features/<Area>/<UseCase>/` — `Handler.cs` plus `Command.cs` (write side: repositories + `IUnitOfWork`) or
    `Query.cs` (read side: a read service, returns DTOs), never both, plus DTOs only that use case returns.
    File names do not repeat the use case; class names do (`RevokeSessionCommand`, `RevokeSessionHandler`).
  - `Models/` — only types used by several areas.
  - No catch-all folders (`Interfaces/`, `ReadServices/`, `Services/`, `Common/`): every port lives next to the code
    that uses it — in the use case folder when one use case uses it, in the area folder when several use cases of
    the area do, in the area that owns the concept when other areas use it too, and in a folder named after what it
    does when no area owns it (e.g. `Security/`). Read ports are `I<Thing>ReadService`; write ports are the
    domain's repositories.
- **Infrastructure**: grouped by the same domain concepts as the Domain — `Accounts/` holds `AccountRepository`,
  `AccountReadService` (`AsNoTracking` projections) and `AccountConfiguration` (its EF mapping,
  `IEntityTypeConfiguration<Account>`); an external client sits with the concept it serves (`ExternalLogin/`).
  `Persistence/` holds only what spans concepts: the DbContext (`ApplyConfigurationsFromAssembly`), its design-time
  factory and the migrations. Never `Repositories/`, `ReadServices/`, `ExternalServices/`, `Options/` folders.
  `UnitOfWork<TDbContext>` and `WireNameConverter` come from `Vaultly.Core.Persistence`.
- **Api**: minimal API endpoints send commands/queries through `ISender`; composition root only; authentication via
  `Vaultly.Core.Authentication`. The service is reached only through the gateway (add its route and cluster to the
  gateway's `ReverseProxy` section; the cluster id is the service's `InternalTokens:Audience`).
- Never use raw `IRequest`/`IRequestHandler` in Application code, never let a query write state,
  never let a query handler depend on a repository or `IUnitOfWork`. Each service's application tests have a
  `CqrsConventionTests : CqrsConventions` fixture (`Vaultly.Core.Testing`; example:
  `accounts/src/Vaultly.Accounts.Application.Tests/CqrsConventionTests.cs`).
- Reference service: `./accounts`.
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
