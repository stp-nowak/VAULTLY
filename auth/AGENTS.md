# Vaultly Auth (Identity service)

Standalone OAuth 2.0 / OIDC authorization server, Keycloak-style: registered clients redirect the browser to
`/authorize`, the user signs in on the service's **own login UI** (`./web`, React), an SSO session cookie lets later
sign-ins skip the login page, and clients exchange codes for tokens server-to-server. Google is the identity provider.
Client-facing flow: `./docs/auth-flow.md`. Login UI guidance: `./web/AGENTS.md`.

## Independence
- This service is **completely separate** from the gateway and the domain services: own solution
  (`src/Vaultly.Auth.slnx`), own database, own deployment. Its DDD building blocks (`AggregateRoot`, `DomainEvent`,
  `DomainException`, `IUnitOfWork`) come from `Vaultly.Core` (`../core`), like every domain service's, together with
  its CQRS markers (`Vaultly.Core.Messaging`), `WireNames` and `Vaultly.Core.Persistence` (unit of work, migrations);
  the Docker image gets `core/src` as the named build context `core`.
- Never add a shared file or shared database, or a project reference to/from another service other than to
  `Vaultly.Core` and `PublicTypes`/`ApiContracts` below.
  Integration with other services happens only through:
  - JWTs validated by consumers via `/.well-known/jwks.json` (today only the gateway; services get its tokens),
  - the `Vaultly.Identity.PublicTypes` project: token claim names (`IdentityClaims`) and `UserRole`, referenced by
    the gateway and `./core`. The domain uses the same `UserRole`, so there is one enum. A change here (e.g. a new
    role) changes every consumer: build and test them in the same change,
  - the `Vaultly.Identity.ApiContracts` project: request/response models of `/token`, `/revoke` and `/sessions`
    (`TokenRequestFields`, `GrantType`, `TokenResponse`, `SessionsResponse`, …) for API clients such as the web API.
    The endpoints serialize these very types, so a change here is a change of the API. `PublicTypes` holds shared
    types, not HTTP payloads,
  - both reference no other project, so other services can depend on them without ever creating a cycle,
  - integration events on the message bus (planned).

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
| `GET /api/authorization-requests/{id}` | Login UI data: client name, status, providers, `passwordSignIn` |
| `POST /api/authorization-requests/{id}/password-sign-in`, `…/registration` | Email + password sign-in / account creation for a pending request (`X-CSRF: 1`, rate-limited per client address); sets the `vaultly_sso` cookie and returns `{ redirectUrl }`; errors `{ error }`: `invalid_credentials`, `email_taken` (409), `weak_password`, `invalid_input`, `request_expired`, `sign_in_method_disabled` (403), 429 |
| `GET /{provider}/authorize?request=<id>`, `GET /{provider}/callback` | External provider login; the callback sets the `vaultly_sso` cookie and redirects to the client with `code` + `state` |
| `POST /token` | Form-encoded; `authorization_code` (code, `code_verifier`, `redirect_uri`) and `refresh_token` grants; client auth `client_secret_post` or `client_secret_basic`; refresh token in the body |
| `POST /revoke` | RFC 7009 refresh-token revocation (ends the session) |
| `GET /logout` | Ends the SSO session; redirects to a registered `post_logout_redirect_uri` or the UI `/logged-out` |
| `GET/DELETE /sessions` | User's sessions (bearer access token) |
| `GET /api/admin/sign-in` | Admin console sign-in (navigation): straight to the console with an active SSO session, otherwise via the login UI |
| `GET /api/admin/me`, `GET /api/admin/users?search&page&pageSize`, `PUT /api/admin/users/{id}/role` | Admin console API: `vaultly_sso` cookie + `X-CSRF: 1`; 401 without a session, 403 without the admin role, 409 `last_admin` |
| `GET /api/admin/sign-in-settings`, `PUT /api/admin/sign-in-settings/methods/{method}` | Sign-in methods with `isConfigured` / `isEnabled`; switching one on or off (`{ "enabled": bool }`), 409 `last_sign_in_method` |
| `/.well-known/openid-configuration`, `/.well-known/jwks.json` | Discovery and signing keys |

Clients are registered in `Identity:Clients` (id, secret, display name, redirect and post-logout URIs; exact or
`…/*` prefix). Secrets only via environment/secret store. Protocol errors never redirect to an unvalidated URI.

Users have a role, `user` or `admin`. Admins use the admin console (login UI `/admin`), which is not an OAuth client:
it is signed in through an `AuthorizationRequest` for `ConsoleClient` (no state, no PKCE, no code issued) and then
uses the SSO cookie. The first admin comes from `Identity:BootstrapAdmin:Email` while no admin exists; role changes
go through `RoleAssignment`, which keeps at least one admin.

Sign-in methods (external providers, later email + password) are *configured* by the deployment
(`IExternalOAuthProvider.IsConfigured`, e.g. Google with a client id) and *enabled* by admins at runtime
(`SignInSettings`, one row, stores only the disabled methods). A method is usable only when both hold
(`SignInMethodGuard` on provider start and callback, the login UI's provider list); the last configured, enabled
method cannot be switched off. `SignInMethodCatalog` lists every known method.

Email + password is built in (always configured, admins can switch it off). Passwords (12–128 characters) are hashed
with ASP.NET Core Identity's `PasswordHasher` behind `IPasswordHasher`; never log them (`Password.ToString()` hides
the value, the commands redact it). A failed sign-in never tells whether the account exists: unknown email, account
without a password and a paused account all answer `invalid_credentials` after the same hashing work. Five wrong
passwords in a row pause password sign-in for 15 minutes. Accounts created with a password are unverified; when a
provider with a verified email later signs in to such an account, its password is removed and its sessions ended
(the registrant may not own the email).

## Structure

```
auth/
├── docs/                                   # Client-facing documentation
├── web/                                    # Login UI (React SPA, @vaultly/auth-web) — hosted by its own web server, never by the API
└── src/
    ├── Vaultly.Auth.slnx
    ├── Vaultly.Identity.PublicTypes/       # referenced by other services: IdentityClaims, UserRole
    ├── Vaultly.Identity.ApiContracts/      # referenced by API clients: token/revoke/sessions models, GrantType
    ├── Vaultly.Identity.Domain/            # Tactical DDD, one folder per aggregate (its entities, value objects,
    │   │                                   #   events, exceptions, repository port; one type per file):
    │   ├── Users/                          #   User, EmailAddress, PersonName; ExternalIdentities/, Passwords/ (incl.
    │   │                                   #   lockout), Roles/ (RoleAssignment keeps one admin)
    │   ├── Sessions/                       #   Session + SSO, token hashes; RefreshTokens/, AuthCodes/ (bound to client/PKCE)
    │   ├── AuthorizationRequests/          #   AuthorizationRequest for a client or the console; Clients/ (ClientId,
    │   │                                   #   RedirectUri, ClientState, CodeChallenge, ConsoleClient)
    │   ├── OAuthStates/                    #   OAuthState, CodeVerifier, Nonce
    │   ├── SignInMethods/                  #   SignInSettings, SignInMethod, ProviderId
    │   └── InvalidValueException.cs        #   the only domain-wide type
    ├── Vaultly.Identity.Application/       # CQRS
    │   ├── Features/<Area>/<UseCase>/      #   Handler.cs + Command.cs or Query.cs (+ its DTOs):
    │   │                                   #   Authorization: StartAuthorization, GetAuthorizationRequest, ExchangeAuthCode,
    │   │                                   #     StartConsoleSignIn; ExternalLogin: StartExternalLogin, HandleOAuthCallback;
    │   │                                   #   Passwords: SignInWithPassword, RegisterWithPassword; Sessions: GetSessions,
    │   │                                   #     RefreshSession, RevokeSession, RevokeToken, EndSession; Users: GetUsers,
    │   │                                   #     GetConsoleUser, AssignUserRole, EnsureBootstrapAdmin; SignInMethods:
    │   │                                   #     GetSignInSettings, SetSignInMethodEnabled
    │   │                                   #   Read ports sit with their queries: Features/Users/IUserReadService,
    │   │                                   #   Features/Sessions/GetSessions/ISessionReadService, …;
    │   │                                   #   Features/ExternalLogin/IExternalOAuthProvider
    │   ├── Security/                       #   ports for token generation and hashing, password hashing, JWT
    │   ├── Models/  Options/  Common/       #   Models: results shared across areas (TokenResult, AuthCallbackResult);
    │   │                                   #   Common: ClientRegistry, OAuthProtocolException (RFC 6749 errors)
    ├── Vaultly.Identity.Infrastructure/    # by the Domain's concepts: Users/, Sessions/, AuthorizationRequests/,
    │                                       #   OAuthStates/, SignInMethods/ (repository, read service, EF configuration);
    │                                       #   ExternalLogin/ (Google client + options), Security/ (tokens, JWT,
    │                                       #   passwords); Persistence/ (DbContext, migrations)
    ├── Vaultly.Identity.Api/               # Minimal API → ISender; Program.cs is only the composition root
    │   ├── Endpoints/                      #   one static class per area with Map<Area>Endpoints (OAuth, Password,
    │   │                                   #   Admin, Session, Discovery); IdentityEndpoints maps them all
    │   ├── Contracts/                      #   wire-only types of the login UI API: ApiError, SignInErrorCode
    │   ├── Http/                           #   HTTP plumbing: ApiJson, CsrfHeaderFilter, SsoCookie, claims, EndpointErrorHandler
    │   └── Configuration/                  #   .env loading, JWT signing key
    ├── Vaultly.Identity.Domain.Tests/
    └── Vaultly.Identity.Application.Tests/ # incl. CqrsConventionTests guarding CQRS rules
```

## Architecture rules: tactical DDD + CQRS
- Read and follow the `ddd` skill (`/.claude/skills/ddd/SKILL.md` at the repository root) for every new or refactored C# file.
- Dependency rule: `Domain → Vaultly.Core + PublicTypes`, `Application → Domain`, `Infrastructure → Application + Domain`,
  `Api → Application + Infrastructure` (composition root only).
- All invariants live in aggregates/value objects; handlers only orchestrate.
- Where code lives (Domain by aggregate, ports next to their users, no catch-all folders): `../core/AGENTS.md`.
  `Common/` and `Models/` here predate that rule; do not add to them, move a type out when touching it.
- **Commands** (`ICommand`/`ICommand<T>`): imperative names, load aggregates via repositories, persist via
  `IUnitOfWork`. Anything that writes state is a command.
- **Queries** (`IQuery<T>`): question names, read via read services (`AsNoTracking` projections),
  return DTOs, no side effects, never depend on repositories or `IUnitOfWork`.
- Use cases are grouped by feature: `Features/<Area>/<UseCase>/` holds `Handler.cs` and exactly one of
  `Command.cs` (write) or `Query.cs` (read), plus DTOs only that use case returns. File names do not repeat the use
  case; class names do (`RevokeSessionCommand`, `RevokeSessionHandler`), and the namespace follows the folders.
  Types several use cases of one area share sit in the area folder; types shared across areas in `Models/`.
- `CqrsConventionTests` (derived from `CqrsConventions` in `Vaultly.Core.Testing`) must stay green; extend the base
  in core rather than bypass it.
- Endpoints live in `Vaultly.Identity.Api/Endpoints/<Area>Endpoints.cs` as `internal static` handlers mapped by `Map<Area>Endpoints`
  (registered in `IdentityEndpoints`), never inline in `Program.cs`.

## C# Instructions
- Latest C# (currently C# 15), `net11.0`, nullable reference types enabled.
- Prefer primary constructors.
- Fixed option sets (roles, statuses, error codes, grant types) are enums, never strings. Their outside spelling is
  the `snake_case` member name: JSON via the global `JsonStringEnumConverter` (`ApiJson`, integers rejected), EF
  columns via `WireNameConverter`, claims and query strings via `WireNames` (both from `../core`). Renaming a member
  changes the wire contract and stored data.
- Comments: follow the root rule — minimal; `<summary>` only on public API types/members where it adds information;
  inline comments only for genuinely non-obvious reasons (protocol/security constraints, workarounds).
- Test-driven development; use the `test-writer` skill (NUnit + Moq).
- Always build and run tests after changes to service functionality.

## Logging and Security
- Log with Serilog.
- Never log raw auth codes, refresh tokens, cookie values, OAuth client secrets, or `Identity:JwtPrivateKeyPem` contents from configuration.
