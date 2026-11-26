# Authentication & authorization flow

How a user signs in to Vaultly, how requests are authorized, and how sign-out works — end to end across the web
app, its backend-for-frontend, the auth service and Google. Client-integration details of the auth service live in
[`auth/docs/auth-flow.md`](../../auth/docs/auth-flow.md).

**In one sentence:** the browser only ever holds HttpOnly cookies; the web API (BFF) is a confidential OIDC client
that keeps the tokens in Redis; the auth service is a Keycloak-style authorization server with its own login UI and
an SSO session.

## Participants

```mermaid
flowchart LR
    subgraph Browser
        SPA["Web SPA<br/>web/src/web"]
        AUI["Auth UI<br/>auth/web"]
    end

    subgraph WebApp["Web app origin (app.vaultly.local)"]
        BFF["Web API (BFF)<br/>web/src/api"]
        Redis[("Redis<br/>tokens per session")]
    end

    subgraph AuthOrigin["Auth origin (identity.vaultly.local)"]
        AUTH["Auth service<br/>auth/src"]
        ADB[("Identity DB")]
    end

    Google["Google<br/>(identity provider)"]
    Backend["Backend gateway<br/>backend/"]

    SPA -- "same-origin /api/* + X-CSRF<br/>cookie: vaultly-session" --> BFF
    BFF --> Redis
    BFF -- "/token, /revoke (client secret)<br/>/sessions (bearer)" --> AUTH
    BFF -- "Bearer access token" --> Backend
    AUI -- "/api/authorization-requests" --> AUTH
    AUTH --> ADB
    AUTH <-- "OAuth 2.0 + PKCE" --> Google
    Backend -. "validates JWT via JWKS" .-> AUTH
```

| Participant | Where | Responsibility |
|---|---|---|
| Web SPA | `web/src/web` | The app and its public landing page. Calls only same-origin `/api/*`. No sign-in screen, no tokens; sign-in starts only when the user clicks "Log in". |
| Web API (BFF) | `web/src/api` | Confidential OIDC client `vaultly-web`. Owns the browser session, stores tokens in Redis, forwards API calls to the backend with a bearer token. |
| Auth service | `auth/src` | OAuth 2.0 / OIDC authorization server: `/authorize`, `/token`, `/revoke`, `/logout`, SSO session, JWT signing. |
| Auth UI | `auth/web` | The auth service's own login pages (`/login`, `/error`, `/logged-out`) and admin console (`/admin`), hosted by their own web server, which proxies the auth API paths. |
| Google | – | Identity provider used by the auth service. |
| Backend gateway | `backend/` | Business APIs; trusts JWTs issued by the auth service. |

## What is stored where

| Item | Lives in | Properties |
|---|---|---|
| `vaultly-session` cookie | Browser, web origin | HttpOnly, Secure, `SameSite=Strict`, random 256-bit id (`__Host-` prefix outside dev) |
| `vaultly-login` cookie | Browser, web origin | HttpOnly, Secure, `SameSite=Lax`, encrypted: `state`, PKCE `code_verifier`, `returnUrl`; 10 min |
| Access + refresh token | Redis (BFF) | Key = SHA-256 of the session id, value encrypted with ASP.NET Data Protection; expires with the refresh token |
| `vaultly_sso` cookie | Browser, auth origin | HttpOnly, Secure, `SameSite=Lax`, random SSO token; only its hash is stored |
| `AuthorizationRequest` | Identity DB | Client, redirect URI, client `state`, PKCE challenge; 10 min |
| `Session` | Identity DB | User sign-in = SSO session (12 h), owns auth codes and refresh tokens |
| `AuthCode` | Identity DB | Hashed, single use, 2 min, bound to client + redirect URI + PKCE challenge |
| `RefreshToken` | Identity DB | Hashed, rotated on every use, 30 days |
| Access token | JWT (RS256) | 15 min, claims `sub` (user), `sid` (session), `role` (`user` or `admin`); validated with `/.well-known/jwks.json` |
| User role | Identity DB | `user` (default) or `admin`; admins use the admin console. At least one admin always remains |
| Sign-in settings | Identity DB | One row listing the sign-in methods admins switched off; every other method is on |

## 1. First sign-in

```mermaid
sequenceDiagram
    autonumber
    actor U as User
    participant SPA as Web SPA
    participant BFF as Web API (BFF)
    participant R as Redis
    participant A as Auth service
    participant UI as Auth UI
    participant G as Google

    U->>SPA: open /
    SPA->>BFF: GET /api/auth/user
    BFF-->>SPA: 401 (no session)
    SPA-->>U: landing page with "Log in"
    U->>SPA: click "Log in"
    SPA->>BFF: full-page navigation GET /api/auth/login?returnUrl=/app
    Note over BFF: create state + PKCE code_verifier<br/>store them encrypted in cookie vaultly-login
    BFF-->>U: 302 to /authorize
    U->>A: GET /authorize?client_id=vaultly-web&redirect_uri&state&code_challenge (S256)
    Note over A: validate client and redirect_uri first<br/>no SSO cookie, so store AuthorizationRequest
    A-->>U: 302 to Auth UI /login?request=id
    U->>UI: GET /login?request=id
    UI->>A: GET /api/authorization-requests/id
    A-->>UI: client name, status pending, providers
    UI-->>U: "Sign in to Vaultly" + "Continue with Google"
    U->>A: GET /google/authorize?request=id
    Note over A: AuthorizationRequest must be pending<br/>create OAuthState (own PKCE + nonce for Google)
    A-->>U: 302 to Google
    U->>G: sign in and consent
    G-->>U: 302 to /google/callback?code&state
    U->>A: GET /google/callback
    A->>G: exchange code (PKCE)
    G-->>A: verified identity + nonce
    Note over A: find or create User, start Session with SSO token<br/>issue AuthCode bound to client, redirect_uri, challenge<br/>complete AuthorizationRequest
    A-->>U: Set-Cookie vaultly_sso, 302 to /api/auth/callback?code&state
    U->>BFF: GET /api/auth/callback?code&state
    Note over BFF: state must equal the one in vaultly-login<br/>(constant-time compare, cookie is consumed)
    BFF->>A: POST /token grant_type=authorization_code, code,<br/>code_verifier, redirect_uri, client_id, client_secret
    Note over A: authenticate client, consume code,<br/>check client + redirect_uri + PKCE
    A-->>BFF: access_token, refresh_token, expires_in, refresh_expires_in, user
    BFF->>R: store tokens under hash(new session id)
    BFF-->>U: Set-Cookie vaultly-session, 302 to /app
    U->>SPA: /app
    SPA->>BFF: GET /api/auth/user
    BFF-->>SPA: 200 user (no tokens)
```

The SPA never starts sign-in by itself. With a session, `/` opens `/app` straight away. Without one, `/` shows the
landing page and `/app` redirects to `/?returnUrl=<where the user was going>`; "Log in" then sends that local path as
`returnUrl`, so the user lands where they wanted to be.

## 2. Silent sign-in with an active SSO session

When the web session is gone (cookie expired, new tab after clearing it, another app) but the auth service still
has an active SSO session, the user sees the landing page, and after clicking "Log in" never sees a login page.

```mermaid
sequenceDiagram
    autonumber
    actor U as User
    participant BFF as Web API (BFF)
    participant A as Auth service

    U->>BFF: GET /api/auth/login?returnUrl=/app
    BFF-->>U: 302 to /authorize (new state + PKCE)
    U->>A: GET /authorize (+ cookie vaultly_sso)
    Note over A: Session found by hash(SSO token)<br/>not revoked and before SsoExpiresAt<br/>issue AuthCode directly, no AuthorizationRequest
    A-->>U: 302 to /api/auth/callback?code&state
    U->>BFF: GET /api/auth/callback
    BFF->>A: POST /token (authorization_code)
    A-->>BFF: tokens
    BFF-->>U: Set-Cookie vaultly-session, 302 to /app
```

## 3. Calling APIs and refreshing tokens

```mermaid
sequenceDiagram
    autonumber
    participant SPA as Web SPA
    participant BFF as Web API (BFF)
    participant R as Redis
    participant A as Auth service
    participant B as Backend gateway

    SPA->>BFF: GET /api/budgets (cookie vaultly-session, X-CSRF: 1)
    Note over BFF: missing X-CSRF header gives 400
    BFF->>R: load session by hash(session id)
    R-->>BFF: tokens
    alt access token valid for more than 60 s
        Note over BFF: use it as is
    else about to expire, refresh accepted
        Note over BFF: per-session lock, re-read session
        BFF->>A: POST /token grant_type=refresh_token + client secret
        A-->>BFF: new access + rotated refresh token
        BFF->>R: save rotated tokens
    else about to expire, refresh rejected
        BFF->>A: POST /token grant_type=refresh_token + client secret
        A-->>BFF: 400 invalid_grant (expired, revoked or reused)
        BFF->>R: delete session
        BFF-->>SPA: 401 + clear cookie
        Note over SPA: route guard returns to the landing page<br/>("Log in" is silent if SSO is still active)
    end
    opt session still active
        BFF->>B: GET /budgets, Authorization: Bearer access token, no cookies
        B-->>BFF: 200
        BFF-->>SPA: 200 (streamed through YARP)
    end
```

Reusing an already rotated refresh token is treated as theft: the auth service revokes the whole session.

## 4. Sign-out

```mermaid
sequenceDiagram
    autonumber
    actor U as User
    participant SPA as Web SPA
    participant BFF as Web API (BFF)
    participant R as Redis
    participant A as Auth service
    participant UI as Auth UI

    U->>SPA: Sign out
    SPA->>BFF: POST /api/auth/logout (X-CSRF: 1)
    BFF->>A: POST /revoke token=refresh_token + client secret
    Note over A: revoke Session, its refresh tokens and SSO
    A-->>BFF: 200
    BFF->>R: delete session
    BFF-->>SPA: 200 logoutUrl, clear vaultly-session
    SPA->>A: full-page navigation GET /logout?client_id&post_logout_redirect_uri
    Note over A: end SSO session (idempotent), delete vaultly_sso<br/>post_logout_redirect_uri must be registered
    alt registered post-logout URI
        A-->>U: 302 to web app /
        U->>SPA: open the app with no session, so the landing page shows
    else not registered
        A-->>U: 302 to Auth UI /logged-out
        U->>UI: "You're signed out."
    end
```

Only the browser can send and clear the auth-domain SSO cookie, which is why sign-out ends with a navigation to the
auth service rather than a server-to-server call alone.

## 5. Admin console

The auth UI's `/admin` is the Identity service's own admin console (users and their roles). It is not an OAuth
client and holds no tokens: its API (`/api/admin/*`) is authenticated by the `vaultly_sso` cookie, and the user's role
is read from the database on every call, so revoking the session or the role takes effect at once.

```mermaid
sequenceDiagram
    autonumber
    actor U as Admin
    participant UI as Auth UI
    participant A as Auth service
    participant G as Google

    U->>UI: open /admin
    UI->>A: GET /api/admin/me (X-CSRF: 1)
    A-->>UI: 401 (no SSO session)
    UI->>A: full-page navigation GET /api/admin/sign-in
    alt active SSO session
        A-->>U: 302 to /admin
    else no SSO session
        Note over A: store an AuthorizationRequest for the console<br/>(no client state, no PKCE challenge)
        A-->>U: 302 to /login?request=id ("Sign in to Vaultly Admin")
        U->>A: GET /google/authorize?request=id
        A-->>U: 302 to Google, back to /google/callback
        U->>G: sign in
        Note over A: find or create User, start Session with SSO token<br/>complete the request without issuing a code
        A-->>U: Set-Cookie vaultly_sso, 302 to /admin
    end
    UI->>A: GET /api/admin/me (cookie vaultly_sso, X-CSRF: 1)
    alt role admin
        A-->>UI: 200 admin
        UI->>A: GET /api/admin/users, PUT /api/admin/users/{id}/role
    else other role
        A-->>UI: 403, the console shows "No access"
    end
```

The first admin comes from configuration: while no admin exists, startup makes the user with
`Identity:BootstrapAdmin:Email` an admin (creating the user when needed); that person then signs in with Google,
which must have verified the address. The last admin can never lose the role (`409 last_admin`). Signing out of the
console is the usual `GET /logout`.

### Sign-in methods

Admins switch sign-in methods on and off at runtime on the console's `/admin/sign-in-methods`
(`GET /api/admin/sign-in-settings`, `PUT /api/admin/sign-in-settings/methods/{method}`). A method can be used only when
the deployment is configured for it (Google: `Google:Enabled` and a client id) **and** admins have not switched it off.
A switched-off provider disappears from the login page, and `/{provider}/authorize` and `/{provider}/callback` refuse
it, so a sign-in started before the switch cannot finish. Existing sessions are not ended. The last configured,
enabled method cannot be switched off (`409 last_sign_in_method`), so nobody can be locked out.

## Lifecycles

```mermaid
stateDiagram-v2
    direction LR
    [*] --> Pending: GET /authorize (no SSO)
    Pending --> Completed: Google callback issues an auth code
    Pending --> Expired: 10 min without sign-in
    Completed --> [*]
    Expired --> [*]
    note right of Pending
        Only a pending request can start a provider login
        and complete; the login UI shows other states as
        "This sign-in link is no longer valid".
    end note
```

```mermaid
stateDiagram-v2
    direction LR
    [*] --> Issued: callback or SSO in /authorize
    Issued --> Redeemed: POST /token with matching client, redirect_uri and PKCE verifier
    Issued --> Burned: POST /token with a wrong client, redirect_uri or verifier
    Issued --> Expired: 2 min
    Redeemed --> [*]
    Burned --> [*]
    Expired --> [*]
```

## Security controls

| Threat | Control | Where |
|---|---|---|
| Token theft from the browser (XSS) | Tokens never reach the browser; only HttpOnly cookies | BFF + Redis |
| CSRF against cookie-authenticated API | `X-CSRF: 1` required on every `/api/*` call; `SameSite=Strict` session cookie | `CsrfHeaderMiddleware` |
| CSRF against the admin console API | `X-CSRF: 1` required on every `/api/admin/*` call except the sign-in navigation; responses are `no-store` | `AdminEndpoints.RequireCsrfHeader` |
| Admin console used without the role, or after losing it | Role read from the database on every admin call, not from a token | `AdminEndpoints.RequireAdminAsync`, `GetConsoleUser` |
| Locking everyone out of the console | The last admin cannot be demoted | `RoleAssignment` |
| Locking everyone out of signing in | The last configured, enabled sign-in method cannot be switched off | `SignInSettings.Disable` |
| Signing in with a switched-off method | Checked when the provider login starts and again on its callback | `SignInMethodGuard` |
| Login CSRF / forged callback | `state` bound to the encrypted `vaultly-login` cookie, compared in constant time | `AuthEndpoints.CallbackAsync` |
| Intercepted authorization code | PKCE S256, code bound to client + redirect URI, client secret at `/token`, single use, burned on failure | `Session.ConsumeAuthCode`, `CodeChallenge` |
| Open redirect | Clients and redirect/post-logout URIs registered per client; invalid ones are never redirected; `returnUrl` must be a local path in the SPA and in the BFF | `ClientRegistry`, `StartAuthorizationHandler`, `SanitizeReturnUrl`, `readLocalPath` |
| Session fixation | New random session id on every sign-in, previous session deleted | `UserSessionService.SignInAsync` |
| Refresh token replay | Rotation on every use; reuse revokes the whole session | `RefreshSessionHandler` |
| Redis dump | Keys are hashed session ids, payloads encrypted | `DistributedWebSessionStore` |
| DB leak | Only hashes of codes, refresh tokens and SSO tokens are stored | Identity domain |
| Token leakage to the backend | BFF strips cookies and the CSRF header before forwarding | `BackendApiTransformer` |

## What the user sees when something fails

| Situation | Page |
|---|---|
| Sign-in link expired or already used | Auth UI `/login`: "This sign-in link is no longer valid." |
| Google denied or failed, callback error | Auth UI `/error?code=access_denied`, `login_failed` or `request_expired` |
| Unknown client or unregistered `redirect_uri` at `/authorize` | Auth UI `/error?code=invalid_request` |
| `state` mismatch or failed code exchange in the BFF | Web `/login?error=invalid_state` or `login_failed` with "Try again" |
| Session ended (refresh rejected) | Web landing page; "Log in" is silent if the SSO session is still active |
| `/app` opened without a session | Web landing page; "Log in" returns to the page the user asked for |

## Configuration

| Setting | Service | Default | Notes |
|---|---|---|---|
| `Identity:Clients[]` | auth | `vaultly-web` | Client id, secret, display name, redirect URIs, post-logout URIs (exact or `…/*`) |
| `Identity:LoginUiBaseUrl` | auth | empty (same origin as `Identity:PublicBaseUrl`) | Browser-facing URL of the separately hosted login UI. Dev and Docker: `http://localhost:5174` |
| `Identity:AccessTokenTtlMinutes` | auth | 15 | |
| `Identity:RefreshTokenTtlDays` | auth | 30 | |
| `Identity:AuthCodeTtlMinutes` | auth | 2 | |
| `Identity:AuthorizationRequestTtlMinutes` | auth | 10 | |
| `Identity:SsoSessionTtlHours` | auth | 12 | |
| `Identity:BootstrapAdmin:Email` | auth | empty | First admin, applied at startup only while no admin exists. Docker: `BOOTSTRAP_ADMIN_EMAIL` in `auth/.env` |
| `Auth:IdentityBaseUrl`, `Auth:ClientId`, `Auth:ClientSecret` | web API | – | Secret only via environment (`Auth__ClientSecret`) |
| `Auth:IdentityInternalBaseUrl` | web API | unset (= `Auth:IdentityBaseUrl`) | Server-to-server address of the auth service (`/token`, `/revoke`, `/sessions`) when it differs from the browser-facing one, e.g. `http://identity-api:8080` in Docker |
| `Auth:PublicOrigin` | web API | – | Callback and post-logout URLs are built from it |
| `Auth:LoginTimeoutMinutes`, `Auth:AccessTokenRefreshSkewSeconds` | web API | 10, 60 | |
| `ConnectionStrings:Redis` | web API | `localhost:6379` | Required outside Development |
| `Database:MigrateOnStartup` | auth | `false` (Development: `true`) | Applies EF Core migrations to the Identity DB on startup; single-instance setups only |

Local development ports: web SPA 5173 (proxies `/api` to 5180), web API 5180, auth service 5096, auth UI 5174
(proxies auth API paths to 5096). The Docker Compose stack (root `README.md`) uses the same ports and topology:
nginx containers take the place of the two Vite dev servers.

Neither API serves a frontend. Each SPA is hosted by its own web server, which proxies the API paths so the SPA and
its API share one origin for cookies.

## Code map

| Step | Code |
|---|---|
| SPA route guards, landing page, login and logout navigation | `web/src/web/src/app/routes/*`, `web/src/web/src/pages/landing`, `web/src/web/src/features/login`, `web/src/web/src/domain/identity` |
| BFF login, callback, logout, user, sessions | `web/src/api/Vaultly.Web.Api/Features/Auth/AuthEndpoints.cs` |
| BFF session, refresh, lock, Redis store | `web/src/api/Vaultly.Web.Api/Features/Auth/Sessions/` |
| BFF client of the auth service | `web/src/api/Vaultly.Web.Api/Features/Auth/IdentityService/IdentityServiceClient.cs` |
| API forwarding to the backend | `web/src/api/Vaultly.Web.Api/Features/Web/ApiProxy/` |
| Auth HTTP endpoints | `auth/src/Vaultly.Identity.Api/Api/OAuthEndpoints.cs`, `AdminEndpoints.cs` (admin console) |
| Auth use cases (CQRS) | `auth/src/Vaultly.Identity.Application/Commands/*`, `Queries/*` |
| Auth domain rules | `AuthorizationRequest`, `Session`, `AuthCode`, `CodeChallenge` in `auth/src/Vaultly.Identity.Domain` |
| Login UI | `auth/web/src/pages/{login,sign-in-error,logged-out}` |
| Admin console | `auth/web/src/pages/{admin-console,admin-users,admin-sign-in-methods}`, `auth/web/src/features/{assign-user-role,toggle-sign-in-method}` |
