# VAULTLY

Personal Finance &amp; Budget Intelligence Platform

## Repository layout

- `web/` - web app: `src/web` React SPA (Feature-Sliced Design with a `domain` layer) + `src/api` C# web API (backend-for-frontend, session cookie, tokens in Redis)
- `backend/` - domain microservices, tactical DDD + CQRS
- `auth/` - standalone identity/auth service (OAuth 2.0/OIDC, SSO), tactical DDD + CQRS, with its own login UI in `auth/web`

## Run with Docker

```
docker compose up --build
```

starts everything; open http://localhost:5173. To sign in, first copy `auth/.env.example` to `auth/.env` and fill in
the Google OAuth client (redirect URI `http://localhost:5096/google/callback`).

Each service has its own compose file with only what that service alone needs, so it can be started on its own:

| Command | Starts | Host ports |
|---|---|---|
| `docker compose -f auth/docker-compose.yml up --build` | Identity API, login UI, Identity Postgres | 5096, 5174, 5432 |
| `docker compose -f web/docker-compose.yml up --build` | web SPA, web API, its Redis | 5173, 5180, 6379 |
| `docker compose -f backend/docker-compose.yml up` | backend Postgres (no backend service has an API yet) | 5433 |
| `docker compose -f infra/docker-compose.yml up` | shared infrastructure: RabbitMQ (`vaultly` / `vaultly`) | 5672, 15672 (management UI) |

- All files belong to one Compose project (`vaultly`), so whatever combination runs shares a network. Starting one
  file while others run prints a harmless "orphan containers" warning — do not pass `--remove-orphans`, it would
  remove the other services' containers.
- Ports equal the local development ports and bind to `127.0.0.1` only, so services run from the IDE can use the
  databases, Redis and RabbitMQ from Docker.
- The app containers call each other by container name, so an app run from the IDE is invisible to them: its
  callers have to run locally too (stop their containers first). Identity API from the IDE → also run the login UI
  and the web API locally; web API from the IDE → also run the SPA locally.
- The SPA containers are nginx: they serve the built app and proxy the API paths to the API container, the same
  job the Vite dev server does in development.
- `docker compose down` stops everything; add `--volumes` to also drop the databases, Redis data and the
  generated JWT signing key.

## Documentation

- [Authentication & authorization flow](docs/architecture/authentication.md) — end-to-end sign-in, SSO, token refresh and sign-out with diagrams

## Auth service configuration

The identity API supports both `appsettings*.json` and a local `.env` file.

Use `auth\src\Vaultly.Identity.Api\.env.example` as the starting point for a local `.env` file. The `.env` file is optional, but when the same key exists in both places, the `.env` value overrides `appsettings.json`.

Use native .NET hierarchical keys in `.env`, such as `Identity__Issuer` and `ConnectionStrings__IdentityDb`.
