# Vaultly.Web.Api

Server-side part of the web app (backend-for-frontend pattern): owns the browser session and talks to the Identity
service and the backend on the user's behalf. See `../../AGENTS.md` for the security model.

## Commands
- from `web/src/api`: `dotnet build Vaultly.Web.Api.slnx` — build
- from `web/src/api`: `dotnet test Vaultly.Web.Api.slnx` — run tests
- `dotnet run --project Vaultly.Web.Api` — http://localhost:5180 (needs Redis or empty `ConnectionStrings:Redis` in Development)

## Structure: vertical slices per feature

```
Vaultly.Web.Api/
├── Program.cs                   # Composition only: AddPlatform → AddAuthFeature → AddWebFeature, middleware, Map*Feature
├── Common/                      # Cross-feature, business-agnostic
│   ├── Csrf/                    #   X-CSRF header middleware (+ AllowWithoutCsrfHeader opt-out)
│   └── Hosting/                 #   Redis cache, Data Protection key ring in Redis, TimeProvider
└── Features/
    ├── Auth/                    # OIDC confidential client (authorize + PKCE, callback, logout), session cookie, tokens in Redis
    │   ├── AuthFeature.cs       #   DI + endpoint registration of the feature
    │   ├── AuthEndpoints.cs     #   /api/auth/{login,callback,user,sessions,logout}
    │   ├── AuthOptions.cs       #   section "Auth"
    │   ├── IdentityService/     #   HTTP client of ./auth (/token, /revoke with client secret, /sessions)
    │   └── Sessions/            #   WebSession, IWebSessionStore (Redis), cookies, UserSessionService, refresh lock
    └── Web/                     # Everything else the SPA needs
        ├── WebFeature.cs        #   DI + static SPA hosting + endpoint registration
        ├── WebOptions.cs        #   section "Web"
        └── ApiProxy/            #   /api/{**path} → backend gateway via YARP, bearer from the session
```

## Rules
- One folder per feature under `Features/`; each exposes `Add<Feature>Feature` and `Map<Feature>Feature`.
- Features talk to each other only through small public interfaces. Today: `Web` → `Auth.Sessions.IAccessTokenProvider`.
  `Auth` must never reference `Web`.
- Only `Common/` may be shared by several features, and it must not contain feature logic.
- No DDD layering here — this is an application edge, not a bounded context. Domain rules live in `./backend`/`./auth`.
- Tokens never leave the server: never return them in responses, never put them in cookies, never log them.
  Session ids are bearer credentials too — never log them (log `AuthSessionId`/`UserId` instead).
- Every `/api` endpoint requires `X-CSRF: 1`; opt out only for top-level GET navigations (`AllowWithoutCsrfHeader`).
- New config goes into the owning feature's options class with data-annotation validation (`ValidateOnStart`).
- C#: latest language version, `net10.0`, nullable enabled, primary constructors, minimal comments (root rule),
  Serilog for logging, NUnit + Moq tests (integration tests via `WebApiFactory`, Redis replaced by memory cache).
- Known limitation: token refresh is serialised per session **within one instance** (`SessionRefreshLock`).
  Run one instance or use sticky sessions until a Redis-based lock is added.
