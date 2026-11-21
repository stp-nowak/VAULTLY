# Vaultly auth web (sign-in UI)

React SPA with the sign-in pages of the Vaultly Identity service (`../src`): provider choice (`/login`), sign-in
errors (`/error`) and the signed-out page (`/logged-out`). Client apps never link here directly — the Identity
service redirects the browser to it during authorization. See `AGENTS.md` for routes, the API contract and rules.

## Stack

- React 19, TypeScript (`strict: true`), Vite
- TanStack Router + TanStack Query
- Tailwind CSS v4 + `@vaultly/ui` (shared design system in `packages/ui`)
- Vitest + React Testing Library + MSW
- Architecture: Feature-Sliced Design with a `domain` layer instead of `entities`

## Local configuration

Optional `.env.local` (see `.env.example`):

- `AUTH_API_PROXY_TARGET`: where the Vite dev server proxies the Identity API paths (default `http://localhost:5096`)

## Commands

Dependencies are installed once for the whole pnpm workspace (repository root):

```powershell
pnpm install                            # from the repository root
pnpm --filter @vaultly/auth-web dev     # http://localhost:5174, requires the Identity API running
pnpm --filter @vaultly/auth-web lint
pnpm --filter @vaultly/auth-web test
pnpm --filter @vaultly/auth-web build   # dist/ is served by the Identity API in production
```
