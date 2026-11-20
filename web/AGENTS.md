# Vaultly Web

The web application = SPA + its own server-side API (backend-for-frontend pattern).

```
web/
└── src/
    ├── web/   # React SPA (Vite, TanStack Router/Query) — Feature-Sliced Design with `domain` layer → ./src/web/AGENTS.md
    └── api/   # Vaultly.Web.Api (C#, ASP.NET Core) — serves the SPA's needs on the same origin → ./src/api/AGENTS.md
```

## Security model (non-negotiable)
- The browser holds **only an HttpOnly session cookie** (`Secure`, `SameSite=Strict`, opaque random id).
  No access/refresh tokens, no PKCE, no token storage in JavaScript.
- `web/src/api` performs the OAuth login against the Identity service (`./auth`), stores the tokens **in Redis**
  per session (key = SHA-256 of the session id, payload encrypted with Data Protection) and refreshes them.
- The SPA calls **only same-origin `/api/*`** with `X-CSRF: 1` (`fetchJson` in `src/web/src/shared/api` does it).
  `/api/auth/*` is handled by the Auth feature, every other `/api/*` is forwarded to the backend gateway with
  `Authorization: Bearer <token from Redis>`; cookies are stripped before forwarding.
- Login is a full-page navigation to `/api/auth/login?returnUrl=/path`; failures come back as `/login?error=<code>`.

## Local development
1. Redis: `docker run -p 6379:6379 redis:7` (or leave `ConnectionStrings:Redis` empty in Development for an in-memory fallback).
2. Identity service: `dotnet run --project auth/src/Vaultly.Identity.Api` (http://localhost:5096).
3. Web API: `dotnet run --project web/src/api/Vaultly.Web.Api` (http://localhost:5180).
4. SPA: `cd web/src/web && pnpm dev` (http://localhost:5173, proxies `/api` → 5180).

Open the app on http://localhost:5173 — the cookie is first-party there thanks to the Vite proxy.

## Instructions
- Changes in the SPA: read `./src/web/AGENTS.md`.
- Changes in the web API: read `./src/api/AGENTS.md`.
- A change of an `/api/auth/*` contract must update both sides in the same PR.
