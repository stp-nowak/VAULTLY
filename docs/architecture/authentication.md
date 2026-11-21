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
| Web SPA | `web/src/web` | The app. Calls only same-origin `/api/*`. No sign-in screen, no tokens. |
| Web API (BFF) | `web/src/api` | Confidential OIDC client `vaultly-web`. Owns the browser session, stores tokens in Redis, forwards API calls to the backend with a bearer token. |
| Auth service | `auth/src` | OAuth 2.0 / OIDC authorization server: `/authorize`, `/token`, `/revoke`, `/logout`, SSO session, JWT signing. |
| Auth UI | `auth/web` | The auth service's own login pages (`/login`, `/error`, `/logged-out`), served by the auth service. |
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
| Access token | JWT (RS256) | 15 min, claims `sub` (user) and `sid` (session); validated with `/.well-known/jwks.json` |

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

    U->>SPA: open /app
    SPA->>BFF: GET /api/auth/user
    BFF-->>SPA: 401 (no session)
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

## 2. Silent sign-in with an active SSO session

When the web session is gone (cookie expired, new tab after clearing it, another app) but the auth service still
has an active SSO session, the user never sees a login page.

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
        Note over SPA: route guard sends the user to sign-in<br/>(silent if SSO is still active)
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
        U->>SPA: open the app with no session, so sign-in shows the full login page
    else not registered
        A-->>U: 302 to Auth UI /logged-out
        U->>UI: "You're signed out."
    end
```

Only the browser can send and clear the auth-domain SSO cookie, which is why sign-out ends with a navigation to the
auth service rather than a server-to-server call alone.

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
| Login CSRF / forged callback | `state` bound to the encrypted `vaultly-login` cookie, compared in constant time | `AuthEndpoints.CallbackAsync` |
| Intercepted authorization code | PKCE S256, code bound to client + redirect URI, client secret at `/token`, single use, burned on failure | `Session.ConsumeAuthCode`, `CodeChallenge` |
| Open redirect | Clients and redirect/post-logout URIs registered per client; invalid ones are never redirected; BFF `returnUrl` must be a local path | `ClientRegistry`, `StartAuthorizationHandler`, `SanitizeReturnUrl` |
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
| Session ended (refresh rejected) | Back to sign-in; silent if the SSO session is still active |

## Configuration

| Setting | Service | Default | Notes |
|---|---|---|---|
| `Identity:Clients[]` | auth | `vaultly-web` | Client id, secret, display name, redirect URIs, post-logout URIs (exact or `…/*`) |
| `Identity:LoginUiBaseUrl` | auth | empty (same origin) | Dev: `http://localhost:5174` (Vite) |
| `Identity:AccessTokenTtlMinutes` | auth | 15 | |
| `Identity:RefreshTokenTtlDays` | auth | 30 | |
| `Identity:AuthCodeTtlMinutes` | auth | 2 | |
| `Identity:AuthorizationRequestTtlMinutes` | auth | 10 | |
| `Identity:SsoSessionTtlHours` | auth | 12 | |
| `LoginUi:ServeSpa`, `LoginUi:SpaRootPath` | auth | `true`, `wwwroot` | Off in development |
| `Auth:IdentityBaseUrl`, `Auth:ClientId`, `Auth:ClientSecret` | web API | – | Secret only via environment (`Auth__ClientSecret`) |
| `Auth:PublicOrigin` | web API | – | Callback and post-logout URLs are built from it |
| `Auth:LoginTimeoutMinutes`, `Auth:AccessTokenRefreshSkewSeconds` | web API | 10, 60 | |
| `ConnectionStrings:Redis` | web API | `localhost:6379` | Required outside Development |

Local development ports: web SPA 5173 (proxies `/api` to 5180), web API 5180, auth service 5096, auth UI 5174
(proxies auth API paths to 5096).

## Code map

| Step | Code |
|---|---|
| SPA route guards, login redirect, logout navigation | `web/src/web/src/app/routes/*`, `web/src/web/src/domain/identity` |
| BFF login, callback, logout, user, sessions | `web/src/api/Vaultly.Web.Api/Features/Auth/AuthEndpoints.cs` |
| BFF session, refresh, lock, Redis store | `web/src/api/Vaultly.Web.Api/Features/Auth/Sessions/` |
| BFF client of the auth service | `web/src/api/Vaultly.Web.Api/Features/Auth/IdentityService/IdentityServiceClient.cs` |
| API forwarding to the backend | `web/src/api/Vaultly.Web.Api/Features/Web/ApiProxy/` |
| Auth HTTP endpoints | `auth/src/Vaultly.Identity.Api/Api/OAuthEndpoints.cs` |
| Auth use cases (CQRS) | `auth/src/Vaultly.Identity.Application/Commands/*`, `Queries/GetAuthorizationRequest` |
| Auth domain rules | `AuthorizationRequest`, `Session`, `AuthCode`, `CodeChallenge` in `auth/src/Vaultly.Identity.Domain` |
| Login UI | `auth/web/src/pages/{login,sign-in-error,logged-out}` |
