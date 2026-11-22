# VAULTLY

# Structure
Mono repo:
- `./web` web application
  - `./web/src/web` React SPA — Feature-Sliced Design with a `domain` layer instead of `entities`
  - `./web/src/api` `Vaultly.Web.Api` (C#) — backend-for-frontend: session cookie for the browser, tokens in Redis, calls auth/backend
- `./packages/ui` `@vaultly/ui` — shared design system (Tailwind v4 tokens, base styles, React components) for every frontend; JS packages share one pnpm workspace (root `pnpm-workspace.yaml`)
- `./backend` backend with all domain micro services — tactical DDD + CQRS (C#)
- `./auth` authentication/identity service (OAuth 2.0/OIDC authorization server), completely separate from `./backend` — tactical DDD + CQRS (C#)
  - `./auth/web` its own login UI (React SPA, Keycloak-style) built on `@vaultly/ui`
- `./infra` Docker Compose for infrastructure shared by several services (RabbitMQ)

### General Instructions
- Authentication/authorization across web, web API and auth service is described in `./docs/architecture/authentication.md`; keep it in sync when changing the flow
- **Comments: as few as possible.** Do not write comments that explain what the code does — make the code
  (names, small functions, types) say it. Add a comment only for something genuinely non-obvious: a protocol or
  security constraint, a workaround, a surprising "why". The only routine exception: a short `<summary>`/JSDoc on
  public types and members of a project's public API (public classes, interfaces, exported package APIs) when it
  adds information beyond the name. This rule overrides any older "comment every function" guidance.
- **Backends never serve frontends.** No API hosts SPA files (no static files, no `index.html` fallback) or knows
  how a frontend is built. Every SPA is hosted by its own web server, which proxies the API paths when both must
  share an origin; an API may only know a frontend's URL for redirects.
- When doing any changes in web app read `./web/AGENTS.md` and then `./web/src/web/AGENTS.md` (SPA) or `./web/src/api/AGENTS.md` (web API)
- When doing any changes in the shared UI package read `./packages/ui/AGENTS.md`
- When doing any changes in backend read `./backend/src/AGENTS.md`
- When doing any changes in auth service read `./auth/AGENTS.md` (login UI: `./auth/web/AGENTS.md`)
- When preparing a commit, read `./.claude/skills/proper-commit/SKILL.md`
- Docker (usage in `./README.md`):
  - one `Dockerfile` per deployable, next to its code (`auth/src`, `auth/web`, `web/src/api`, `web/src/web`) — never one image for an API and its SPA
  - one `docker-compose.yml` per service (`auth/`, `web/`, `backend/`) with only what that service alone needs (its apps, its database/cache)
  - anything used by several services goes to `infra/docker-compose.yml`; the root `docker-compose.yml` only `include`s the others
  - every compose file keeps `name: vaultly` (one shared network) and gives each service a `container_name: vaultly-<service>`
- CI: one workflow per project in `.github/workflows/` (ui, web-spa, web-api, auth, auth-web, backend), each triggered only by changes in its paths; they run in parallel. A workflow whose project has a `Dockerfile` also builds that image (`image` job, no push)
