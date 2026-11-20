# VAULTLY

# Structure
Mono repo:
- `./web` web application
  - `./web/src/web` React SPA — Feature-Sliced Design with a `domain` layer instead of `entities`
  - `./web/src/api` `Vaultly.Web.Api` (C#) — backend-for-frontend: session cookie for the browser, tokens in Redis, calls auth/backend
- `./backend` backend with all domain micro services — tactical DDD + CQRS (C#)
- `./auth` authentication/identity service, completely separate from `./backend` — tactical DDD + CQRS (C#)

### General Instructions
- When doing any changes in web app read `./web/AGENTS.md` and then `./web/src/web/AGENTS.md` (SPA) or `./web/src/api/AGENTS.md` (web API)
- When doing any changes in backend read `./backend/src/AGENTS.md`
- When doing any changes in auth service read `./auth/AGENTS.md`
- When preparing a commit, read `./.agents/skills/proper-commit/SKILL.md`
- CI: one workflow per project in `.github/workflows/` (web-spa, web-api, auth, backend), each triggered only by changes in its paths; they run in parallel
