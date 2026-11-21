# VAULTLY

Personal Finance &amp; Budget Intelligence Platform

## Repository layout

- `web/` - web app: `src/web` React SPA (Feature-Sliced Design with a `domain` layer) + `src/api` C# web API (backend-for-frontend, session cookie, tokens in Redis)
- `backend/` - domain microservices, tactical DDD + CQRS
- `auth/` - standalone identity/auth service (OAuth 2.0/OIDC, SSO), tactical DDD + CQRS, with its own login UI in `auth/web`

## Documentation

- [Authentication & authorization flow](docs/architecture/authentication.md) — end-to-end sign-in, SSO, token refresh and sign-out with diagrams

## Auth service configuration

The identity API supports both `appsettings*.json` and a local `.env` file.

Use `auth\src\Vaultly.Identity.Api\.env.example` as the starting point for a local `.env` file. The `.env` file is optional, but when the same key exists in both places, the `.env` value overrides `appsettings.json`.

Use native .NET hierarchical keys in `.env`, such as `Identity__Issuer` and `ConnectionStrings__IdentityDb`.
