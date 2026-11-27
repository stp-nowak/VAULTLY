# Vaultly Web

The web application = SPA + its own server-side API (backend-for-frontend pattern).

```
web/
└── src/
    ├── web/   # React SPA (Vite, TanStack Router/Query) — Feature-Sliced Design with `domain` layer → ./src/web/AGENTS.md
    └── api/   # Vaultly.Web.Api (C#, ASP.NET Core) — serves the SPA's needs on the same origin → ./src/api/AGENTS.md
```

## Security model (non-negotiable)
Full flow with diagrams: `../docs/architecture/authentication.md`.

- The browser holds **only an HttpOnly session cookie** (`Secure`, `SameSite=Strict`, opaque random id).
  No access/refresh tokens, no PKCE, no token storage in JavaScript.
- `web/src/api` is a confidential OIDC client of the Identity service (`./auth`): it redirects to `/authorize`
  (PKCE), exchanges the code with its client secret, stores the tokens **in Redis**
  per session (key = SHA-256 of the session id, payload encrypted with Data Protection) and refreshes them.
- The SPA calls **only same-origin `/api/*`** with `X-CSRF: 1` (`fetchJson` in `src/web/src/shared/api` does it).
  `/api/auth/*` is handled by the Auth feature, every other `/api/*` is forwarded to the API gateway (`./gateway`) with
  `Authorization: Bearer <token from Redis>`; cookies are stripped before forwarding.
- The SPA has **no sign-in screen** and never starts sign-in by itself. It only checks for a session
  (`GET /api/auth/user`): with one, `/` opens `/app`; without one, `/` shows the public landing page and `/app`
  redirects to `/?returnUrl=…`. Sign-in starts when the user clicks "Log in": a full-page navigation to
  `/api/auth/login?returnUrl=…`, which leads to the Identity service's own login UI (`auth/web`). Failures come back
  as `/login?error=<code>`.
- Logout: `POST /api/auth/logout` revokes the tokens and returns `logoutUrl`; the SPA navigates there so the Identity
  service ends its SSO session, then the browser returns to `/`.

## Docker
- `docker compose -f web/docker-compose.yml up --build` (from the repo root) starts the SPA (http://localhost:5173),
  the web API (5180) and its Redis (6379); signing in also needs `auth/docker-compose.yml`. Root `README.md` has the
  whole picture.
- Two images: `src/api/Dockerfile` (web API) and `src/web/Dockerfile` (nginx serving the SPA and proxying `/api` to
  `API_PROXY_TARGET`, see `src/web/nginx.conf.template`).
- In a container the web API calls Identity through `Auth:IdentityInternalBaseUrl`, while browser redirects keep
  using `Auth:IdentityBaseUrl`.

## Local development
1. Redis: `docker compose -f web/docker-compose.yml up -d web-redis` (or leave `ConnectionStrings:Redis` empty in Development for an in-memory fallback).
2. Identity service: `dotnet run --project auth/src/Vaultly.Identity.Api` (http://localhost:5096) and its login UI
   `pnpm --filter @vaultly/auth-web dev` (http://localhost:5174).
3. Web API: `dotnet run --project web/src/api/Vaultly.Web.Api` (http://localhost:5180).
4. SPA: `pnpm install` (repo root, once), then `pnpm --filter @vaultly/web dev` (http://localhost:5173, proxies `/api` → 5180).

Open the app on http://localhost:5173 — the cookie is first-party there thanks to the Vite proxy.

## Instructions
- Changes in the SPA: read `./src/web/AGENTS.md`.
- Changes in the web API: read `./src/api/AGENTS.md`.
- A change of an `/api/auth/*` contract must update both sides in the same PR.
