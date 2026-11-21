# Vaultly web SPA

React SPA of the Vaultly web app. It talks only to the web API (`../api`) on the same origin and holds no tokens —
authentication is an HttpOnly session cookie managed by the web API. See `../../AGENTS.md`.

## Stack

- React 19, TypeScript (`strict: true`), Vite
- TanStack Router + TanStack Query
- Tailwind CSS v4 + `@vaultly/ui` (shared design system in `packages/ui`)
- Vitest + React Testing Library + MSW
- Architecture: Feature-Sliced Design with a `domain` layer instead of `entities` (see `AGENTS.md`)

## Local configuration

Optional `.env.local` (see `.env.example`):

- `API_PROXY_TARGET`: where the Vite dev server proxies `/api` (default `http://localhost:5180`, the web API)

## Commands

Dependencies are installed once for the whole pnpm workspace (repository root):

```powershell
pnpm install                       # from the repository root
pnpm --filter @vaultly/web dev     # http://localhost:5173, requires the web API running
pnpm --filter @vaultly/web lint
pnpm --filter @vaultly/web test
pnpm --filter @vaultly/web build
```
