# VAULTLY

# Structure
Mono repo:
- `./web` web application
  - `./web/src/web` React SPA — Feature-Sliced Design with a `domain` layer instead of `entities`
  - `./web/src/api` `Vaultly.Web.Api` (C#) — backend-for-frontend: session cookie for the browser, tokens in Redis, calls auth and the gateway
- `./packages/ui` `@vaultly/ui` — shared design system (Tailwind v4 tokens, base styles, React components) for every frontend; JS packages share one pnpm workspace (root `pnpm-workspace.yaml`)
- `./auth` authentication/identity service (OAuth 2.0/OIDC authorization server) — tactical DDD + CQRS (C#)
  - `./auth/web` its own login UI (React SPA, Keycloak-style) built on `@vaultly/ui`
  - owns `Vaultly.Identity.PublicTypes` (token claims, `UserRole`) and `Vaultly.Identity.ApiContracts` (its HTTP
    API models, used by the web API), the only projects other services may reference
- `./gateway` `Vaultly.Gateway` (C#, YARP) — single entry to the domain services: validates Identity access tokens,
  routes by path, gives each service its own short-lived internal token
- `./core` — projects every domain service and the identity service build on: `Vaultly.Core` (DDD building blocks,
  `Money`/`Currency`, CQRS messaging, `WireNames`), `Vaultly.Core.Persistence` (EF Core unit of work, migrations),
  `Vaultly.Core.Messaging.Rebus` (messages between services: Rebus on RabbitMQ with an outbox),
  `Vaultly.Core.Authentication` (internal token validation), `Vaultly.Core.Testing` (CQRS convention tests, test
  gateway, recording message bus)
- `./accounts` `Vaultly.Accounts` — the user's financial accounts, balances, balance history and net worth
- `./transactions` `Vaultly.Transactions` — the transactions users enter on manual accounts; a Rebus saga posts them to
  Accounts' balances (`./docs/architecture/transaction-posting.md`)
- Domain services (Accounts, Transactions, Budgets, …): one top-level folder per service, named after what it does
  (e.g. `./accounts`), never a generic `backend` — tactical DDD + CQRS (C#), rules in `./core/AGENTS.md`
- `./infra` Docker Compose for infrastructure shared by several services (RabbitMQ)

### General Instructions
- Authentication/authorization across web, web API and auth service is described in `./docs/architecture/authentication.md`; keep it in sync when changing the flow
- How transactions reach account balances (posting saga, compensation, delivery guarantees) is described in `./docs/architecture/transaction-posting.md`; keep it in sync
- **Comments: as few as possible.** Do not write comments that explain what the code does — make the code
  (names, small functions, types) say it. Add a comment only for something genuinely non-obvious: a protocol or
  security constraint, a workaround, a surprising "why". The only routine exception: a short `<summary>`/JSDoc on
  public types and members of a project's public API (public classes, interfaces, exported package APIs) when it
  adds information beyond the name. This rule overrides any older "comment every function" guidance.
- **Enums, not magic strings.** In C#, a value with a fixed set of options (roles, statuses, error codes, kinds) is
  an enum; never pass around or compare its string spellings. The wire or database form may stay a lowercase string,
  produced in one place by serialization (`WireNames`, `[JsonStringEnumMemberName]`, an EF value converter). Open
  sets (e.g. provider ids) stay strings. If there is a strong reason not to use an enum, ask first and present the
  options. **Frontends (TypeScript) never use `enum`**: always a string literal union type (`'admin' | 'user'`).
- **Name things after what they do.** Services, projects, packages and containers say their responsibility
  (`gateway`, `accounts`, `Vaultly.Core.Authentication`); never a generic `backend`/`Backend`.
- **Code shared between services lives in one project owned by one service, never in a copied file.** Other
  services use it by a relative `ProjectReference` (one repository, one commit, no package feed). A service exposes
  only these projects, split by purpose: `Vaultly.<Service>.PublicTypes` — types other services work with (enums
  such as `UserRole`, token claim names); `Vaultly.<Service>.ApiContracts` — request/response models of its HTTP API
  for its API clients (e.g. the web API calling Identity) instead of hand-written copies. Plus `./core` for every
  domain service. Never reference another service's `Domain`, `Application`, `Infrastructure` or `Api` project.
  `PublicTypes` and `ApiContracts` reference no other project and `Vaultly.Core*` only `PublicTypes`, so references
  only ever point from services to them and can never form a cycle. A consumer's CI workflow also triggers on the
  projects it references; its Docker image gets them as a named build context (`additional_contexts` in compose,
  `--build-context` in CI). If the repository is ever split, these projects become NuGet packages.
- **APIs never serve frontends.** No API hosts SPA files (no static files, no `index.html` fallback) or knows
  how a frontend is built. Every SPA is hosted by its own web server, which proxies the API paths when both must
  share an origin; an API may only know a frontend's URL for redirects.
- When doing any changes in web app read `./web/AGENTS.md` and then `./web/src/web/AGENTS.md` (SPA) or `./web/src/api/AGENTS.md` (web API)
- When doing any changes in the shared UI package read `./packages/ui/AGENTS.md`
- When doing any changes in the gateway read `./gateway/AGENTS.md`
- When doing any changes in core or a domain service read `./core/AGENTS.md` (accounts: also `./accounts/AGENTS.md`;
  transactions: also `./transactions/AGENTS.md`)
- When doing any changes in auth service read `./auth/AGENTS.md` (login UI: `./auth/web/AGENTS.md`)
- When preparing a commit, read `./.claude/skills/proper-commit/SKILL.md`
- Docker (usage in `./README.md`):
  - one `Dockerfile` per deployable, next to its code (`auth/src`, `auth/web`, `gateway/src`, `accounts/src`, `transactions/src`, `web/src/api`, `web/src/web`) — never one image for an API and its SPA
  - one `docker-compose.yml` per service (`auth/`, `gateway/`, `accounts/`, `transactions/`, `web/`, one per domain service) with only what that service alone needs (its apps, its database/cache)
  - anything used by several services goes to `infra/docker-compose.yml`; the root `docker-compose.yml` only `include`s the others
  - every compose file keeps `name: vaultly` (one shared network) and gives each service a `container_name: vaultly-<service>`
- CI: one workflow per project in `.github/workflows/` (ui, web-spa, web-api, auth, auth-web, gateway, core, accounts, transactions), each triggered only by changes in its paths; they run in parallel. A workflow whose project has a `Dockerfile` also builds that image (`image` job, no push)
