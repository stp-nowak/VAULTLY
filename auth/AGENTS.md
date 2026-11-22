# Vaultly Auth (Identity service)

Standalone OAuth 2.0 / OIDC authorization server, Keycloak-style: registered clients redirect the browser to
`/authorize`, the user signs in on the service's **own login UI** (`./web`, React), an SSO session cookie lets later
sign-ins skip the login page, and clients exchange codes for tokens server-to-server. Google is the identity provider.
Client-facing flow: `./docs/auth-flow.md`. Login UI guidance: `./web/AGENTS.md`.

## Independence
- This service is **completely separate** from `./backend`: own solution (`src/Vaultly.Auth.slnx`), own
  shared kernel (`Vaultly.Identity.SharedKernel`), own database, own deployment.
- Never add a project reference, shared file or shared database to/from `./backend`.
  Integration with other services happens only through:
  - JWTs validated by consumers via `/.well-known/openid-configuration` and `/.well-known/jwks.json`,
  - integration events on the message bus (contracts are duplicated as DTOs by consumers, not shared assemblies).

- The API never serves the login UI (no static files, no `index.html` fallback). `./web` is hosted by its own web
  server (nginx image), which proxies the API paths so UI and API share one origin; the API only knows the UI's
  URL for redirects (`Identity:LoginUiBaseUrl`, empty = same origin as `Identity:PublicBaseUrl`).

## Commands
- from `auth/src`: `dotnet build Vaultly.Auth.slnx` — build
- from `auth/src`: `dotnet test Vaultly.Auth.slnx` — run tests
- Local config: `src/Vaultly.Identity.Api/.env.example` → `.env` (overrides `appsettings*.json`, keys like `Identity__Issuer`).
- Login UI: `pnpm --filter @vaultly/auth-web dev` (http://localhost:5174, proxies API paths to http://localhost:5096).
- Docker: `docker compose -f auth/docker-compose.yml up --build` (from the repo root) — API (5096), login UI (5174)
  and Postgres (5432); only the database: add `-d identity-db`. Images: `src/Dockerfile` (API), `web/Dockerfile`
  (nginx + login UI). Google credentials: `.env.example` → `.env` next to the compose file.
- Database schema = EF Core migrations in `src/Vaultly.Identity.Infrastructure/Persistence/Migrations`. After a model
  change, from `auth/src`:
  `dotnet ef migrations add <Name> --project Vaultly.Identity.Infrastructure --startup-project Vaultly.Identity.Infrastructure --output-dir Persistence/Migrations`
  (a test fails while the model and migrations differ). `Database:MigrateOnStartup` applies them on startup — on in
  Development, off otherwise.

## Endpoints
| Endpoint | Purpose |
|---|---|
| `GET /authorize` | Authorization code + PKCE (S256 only, `state` required). Silent sign-in with an active SSO cookie, otherwise redirect to the login UI `/login?request=<id>` |
| `GET /api/authorization-requests/{id}` | Login UI data: client name, status, providers |
| `GET /{provider}/authorize?request=<id>`, `GET /{provider}/callback` | External provider login; the callback sets the `vaultly_sso` cookie and redirects to the client with `code` + `state` |
| `POST /token` | Form-encoded; `authorization_code` (code, `code_verifier`, `redirect_uri`) and `refresh_token` grants; client auth `client_secret_post` or `client_secret_basic`; refresh token in the body |
| `POST /revoke` | RFC 7009 refresh-token revocation (ends the session) |
| `GET /logout` | Ends the SSO session; redirects to a registered `post_logout_redirect_uri` or the UI `/logged-out` |
| `GET/DELETE /sessions` | User's sessions (bearer access token) |
| `/.well-known/openid-configuration`, `/.well-known/jwks.json` | Discovery and signing keys |

Clients are registered in `Identity:Clients` (id, secret, display name, redirect and post-logout URIs; exact or
`…/*` prefix). Secrets only via environment/secret store. Protocol errors never redirect to an unvalidated URI.

## Structure

```
auth/
├── docs/                                   # Client-facing documentation
├── web/                                    # Login UI (React SPA, @vaultly/auth-web) — hosted by its own web server, never by the API
└── src/
    ├── Vaultly.Auth.slnx
    ├── Vaultly.Identity.SharedKernel/      # AggregateRoot, DomainEvent, DomainException, IUnitOfWork
    ├── Vaultly.Identity.Domain/            # Tactical DDD: Aggregates (User, Session + SSO, OAuthState,
    │                                       #   AuthorizationRequest), Entities (AuthCode bound to client/PKCE),
    │                                       #   ValueObjects, Events, Exceptions, Constants
    ├── Vaultly.Identity.Application/       # CQRS
    │   ├── Messaging/                      #   ICommand, IQuery, ICommandHandler, IQueryHandler
    │   ├── Commands/<UseCase>/             #   write side: StartAuthorization, StartExternalLogin, HandleOAuthCallback,
    │   │                                   #   ExchangeAuthCode, RefreshSession, RevokeToken, EndSession, RevokeSession
    │   ├── Queries/<UseCase>/              #   read side: GetAuthorizationRequest, GetSessions
    │   ├── Interfaces/Repositories/        #   write ports (aggregates)
    │   ├── Interfaces/ReadServices/        #   read ports (DTOs)
    │   ├── Interfaces/Services/            #   other ports (clock, token hashing, JWT, OAuth providers)
    │   ├── Models/  Options/  Common/       #   Common: ClientRegistry, OAuthProtocolException (RFC 6749 errors)
    ├── Vaultly.Identity.Infrastructure/    # EF Core (Repositories, ReadServices), security, Google OAuth client
    ├── Vaultly.Identity.Api/               # Minimal API endpoints → ISender, composition root
    ├── Vaultly.Identity.Domain.Tests/
    └── Vaultly.Identity.Application.Tests/ # incl. CqrsConventionTests guarding CQRS rules
```

## Architecture rules: tactical DDD + CQRS
- Read and follow the `ddd` skill (`src/.claude/skills/ddd/SKILL.md`) for every new or refactored C# file.
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
- Comments: follow the root rule — minimal; `<summary>` only on public API types/members where it adds information;
  inline comments only for genuinely non-obvious reasons (protocol/security constraints, workarounds).
- Test-driven development; use the `test-writer` skill (NUnit + Moq).
- Always build and run tests after changes to service functionality.

## Logging and Security
- Log with Serilog.
- Never log raw auth codes, refresh tokens, cookie values, OAuth client secrets, or `Identity:JwtPrivateKeyPem` contents from configuration.
