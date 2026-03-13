# Vaultly frontend

Initial React authentication shell for Vaultly Phase 1.

## Stack

- React 19
- TypeScript with `strict: true`
- Vite
- TanStack Router
- TanStack Query
- Sass
- Vitest + React Testing Library + MSW

## Local configuration

Copy `.env.example` to `.env.local` and adjust the values for your environment:

```powershell
Copy-Item .env.example .env.local
```

Environment variables:

- `IDENTITY_BASE_URL`: public base URL of the identity service
- `IDENTITY_CLIENT_ID`: OAuth client id used by the frontend during the PKCE flow
- `IDENTITY_SCOPES`: scopes requested during login
- `AUTH_CALLBACK_PATH`: frontend callback path, relative to the app origin

The identity base URL is local developer configuration only. It is not edited in the runtime UI.

## Backend alignment

The frontend follows `backend\docs\auth-flow.md`:

- discover auth endpoints from `/.well-known/openid-configuration`
- keep the access token in short-lived client state
- rely on the `vaultly_refresh` cookie for refresh
- include credentials for refresh/logout behavior
- treat refresh `401` responses as the end of the session

Make sure the frontend callback URL matches the identity-service frontend callback configuration.

## Commands

```powershell
pnpm install
pnpm dev
pnpm lint
pnpm test
pnpm build
```
