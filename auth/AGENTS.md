# Vaultly Auth (Identity service)

Standalone authentication service: OAuth 2.0 Authorization Code + PKCE, Google login, JWT access tokens,
refresh-token rotation (cookie `vaultly_refresh`), session listing/revocation, OIDC discovery + JWKS.
Client-facing flow: `./docs/auth-flow.md`.

## Independence
- This service is **completely separate** from `./backend`: own solution (`src/Vaultly.Auth.slnx`), own
  shared kernel (`Vaultly.Identity.SharedKernel`), own database, own deployment.
- Never add a project reference, shared file or shared database to/from `./backend`.
  Integration with other services happens only through:
  - JWTs validated by consumers via `/.well-known/openid-configuration` and `/.well-known/jwks.json`,
  - integration events on the message bus (contracts are duplicated as DTOs by consumers, not shared assemblies).

## Commands
- from `auth/src`: `dotnet build Vaultly.Auth.slnx` — build
- from `auth/src`: `dotnet test Vaultly.Auth.slnx` — run tests
- Local config: `src/Vaultly.Identity.Api/.env.example` → `.env` (overrides `appsettings*.json`, keys like `Identity__Issuer`).

## Structure

```
auth/
├── docs/                                   # Client-facing documentation
└── src/
    ├── Vaultly.Auth.slnx
    ├── Vaultly.Identity.SharedKernel/      # AggregateRoot, DomainEvent, DomainException, IUnitOfWork
    ├── Vaultly.Identity.Domain/            # Tactical DDD: Aggregates (User, Session, OAuthState), Entities,
    │                                       #   ValueObjects, Events, Exceptions, Constants
    ├── Vaultly.Identity.Application/       # CQRS
    │   ├── Messaging/                      #   ICommand, IQuery, ICommandHandler, IQueryHandler
    │   ├── Commands/<UseCase>/             #   write side: StartExternalLogin, HandleOAuthCallback,
    │   │                                   #   ExchangeAuthCode, RefreshSession, Logout, RevokeSession
    │   ├── Queries/<UseCase>/              #   read side: GetEnabledProviders, GetSessions
    │   ├── Interfaces/Repositories/        #   write ports (aggregates)
    │   ├── Interfaces/ReadServices/        #   read ports (DTOs)
    │   ├── Interfaces/Services/            #   other ports (clock, token hashing, JWT, OAuth providers)
    │   ├── Models/  Options/  Common/
    ├── Vaultly.Identity.Infrastructure/    # EF Core (Repositories, ReadServices), security, Google OAuth client
    ├── Vaultly.Identity.Api/               # Minimal API endpoints → ISender, composition root
    ├── Vaultly.Identity.Domain.Tests/
    └── Vaultly.Identity.Application.Tests/ # incl. CqrsConventionTests guarding CQRS rules
```

## Architecture rules: tactical DDD + CQRS
- Read and follow the `ddd` skill (`src/.agents/skills/ddd/SKILL.md`) for every new or refactored C# file.
- Dependency rule: `Domain → SharedKernel`, `Application → Domain`, `Infrastructure → Application + Domain`,
  `Api → Application + Infrastructure` (composition root only).
- All invariants live in aggregates/value objects; handlers only orchestrate.
- **Commands** (`ICommand`/`ICommand<T>`): imperative names, load aggregates via repositories, persist via
  `IUnitOfWork`. Anything that writes state is a command.
- **Queries** (`IQuery<T>`): question names, read via `Interfaces/ReadServices` (`AsNoTracking` projections),
  return DTOs, no side effects, never depend on repositories or `IUnitOfWork`.
- One use case = one folder with `<UseCase>Command.cs|Query.cs` and `<UseCase>Handler.cs`.
- `CqrsConventionTests` must stay green; extend it rather than bypass it.

## C# Instructions
- Latest C# (currently C# 14), `net10.0`, nullable reference types enabled.
- Prefer primary constructors.
- Write clear and concise XML comments for each function; explain *why* for non-obvious design decisions.
- Test-driven development; use the `test-writer` skill (NUnit + Moq).
- Always build and run tests after changes to service functionality.

## Logging and Security
- Log with Serilog.
- Never log raw auth codes, refresh tokens, cookie values, OAuth client secrets, or `Identity:JwtPrivateKeyPem` contents from configuration.
