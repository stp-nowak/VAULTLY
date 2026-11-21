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

### General Instructions
- Authentication/authorization across web, web API and auth service is described in `./docs/architecture/authentication.md`; keep it in sync when changing the flow
- **Comments: as few as possible.** Do not write comments that explain what the code does — make the code
  (names, small functions, types) say it. Add a comment only for something genuinely non-obvious: a protocol or
  security constraint, a workaround, a surprising "why". The only routine exception: a short `<summary>`/JSDoc on
  public types and members of a project's public API (public classes, interfaces, exported package APIs) when it
  adds information beyond the name. This rule overrides any older "comment every function" guidance.
- When doing any changes in web app read `./web/AGENTS.md` and then `./web/src/web/AGENTS.md` (SPA) or `./web/src/api/AGENTS.md` (web API)
- When doing any changes in the shared UI package read `./packages/ui/AGENTS.md`
- When doing any changes in backend read `./backend/src/AGENTS.md`
- When doing any changes in auth service read `./auth/AGENTS.md` (login UI: `./auth/web/AGENTS.md`)
- When preparing a commit, read `./.agents/skills/proper-commit/SKILL.md`
- CI: one workflow per project in `.github/workflows/` (ui, web-spa, web-api, auth, auth-web, backend), each triggered only by changes in its paths; they run in parallel
