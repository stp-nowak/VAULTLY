# Auth web (Identity sign-in UI)

> Part of the auth service (`../AGENTS.md`). React SPA `@vaultly/auth-web`: the **login UI of the Vaultly Identity
> service**, Keycloak-style. Client apps (e.g. the web API) redirect the browser to the Identity service
> (`/authorize`), which sends it to this UI to choose a sign-in provider. The UI is hosted by its own web server
> (nginx image, Vite in development) that proxies the Identity API paths, so every call is same-origin; the Identity
> API (`auth/src`) never serves it. It is also the Identity service's **admin console** (`/admin`). This SPA never
> sees tokens and holds no credentials: the console's API is authenticated by the Identity service's own HttpOnly SSO
> cookie, and every call sends `X-CSRF: 1` (`fetchJson`).

## Routes (`src/app/routes`, TanStack file routes)

| URL | Page | What it shows |
|---|---|---|
| `/login?request=<id>` | `pages/login` | Loads the authorization request; `pending` → "Sign in to {clientName}" + one "Continue with {name}" button per provider (full-page navigation to `startUrl`). Missing `request`, 404, `expired` or `completed` → "This sign-in link is no longer valid…". Loading → "Loading…"; network/5xx → "We couldn't load the sign-in page…". |
| `/register?request=<id>` | `pages/register` | "Create your {clientName} account": name, email and password (`features/register-with-password`), link back to sign-in. Notices as on `/login`; password sign-in switched off → "not available". |
| `/error?code=<code>` | `pages/sign-in-error` | "Sign-in didn't complete" + message for `login_failed`, `access_denied`, `invalid_request`, `request_expired` (anything else → `login_failed`). |
| `/logged-out` | `pages/logged-out` | "You're signed out." |
| `/` | — | Redirects to `/logged-out`. |
| `/admin` | `pages/admin-console` | Layout of the admin console: loads `GET /api/admin/me`; admin → console frame (`widgets/console-shell`) + child page; 403 → "No access" + sign out; 401 → "Signing you in…" and a full-page navigation to `/api/admin/sign-in`. `/admin/` redirects to `/admin/users`. |
| `/admin/users?search=&page=` | `pages/admin-users` | Users newest first with role select (`features/assign-user-role`), sign-in methods and join date; search by email or name; pages of 20. |
| `/admin/sign-in-methods` | `pages/admin-sign-in-methods` | Every sign-in method with a switch (`features/toggle-sign-in-method`) and whether it is offered, switched off or not set up on this server (switch disabled). |
| unknown URL / route error | `pages/route-error` | Root `notFoundComponent` / `errorComponent`. |

Search params are kept as raw strings (`shared/lib/searchParams.ts`): the router's default JSON parsing would turn
ids such as `8e10` into numbers.

## API contract (Identity API, same origin)

`GET /api/authorization-requests/{id}` (`credentials: 'same-origin'`):

```json
{ "id": "8f1c…", "clientName": "Vaultly", "status": "pending",
  "providers": [ { "id": "google", "name": "Google", "startUrl": "/google/authorize?request=8f1c…" } ] }
```

- `status`: `"pending" | "expired" | "completed"`; unknown id → HTTP 404 (treated as an invalid link).
- `passwordSignIn`: `/login` then shows the email and password form (`features/sign-in-with-password`) and a link to
  `/register`. A pending request without providers and without `passwordSignIn` shows "Signing in is not available".
- `POST /api/authorization-requests/{id}/password-sign-in` `{ email, password }` and `…/registration`
  `{ name, email, password }` → `{ redirectUrl }` (absolute http(s), checked by `parseSignInResponse`; the browser
  navigates there). Errors `{ error }` are mapped to messages per feature (`signInFailureOf`); a failed sign-in never
  says whether the account exists.
- `startUrl` must be a same-origin path (`/…`); anything else is rejected by the parser.
- Server-side sign-in failures redirect the browser to `/error?code=<code>`; sign-out ends on `/logged-out`.

Client, parser, types and `authorizationRequestQueryOptions` live in `src/domain/authorization-request`.

Admin console (`X-CSRF: 1` required; 401 = no SSO session, 403 = not an admin):

- `GET /api/admin/sign-in` — page navigation that signs the browser in and returns to `/admin`.
- `GET /api/admin/me` → `{ "id", "email", "name", "role": "admin" }` (`domain/console-admin`).
- `GET /api/admin/users?search=&page=&pageSize=` → `{ "items": [{ "id", "email", "name", "role", "emailVerified",
  "createdAt", "signInMethods": ["google"] }], "total", "page", "pageSize" }` (`domain/user`).
- `PUT /api/admin/users/{id}/role` with `{ "role": "admin" | "user" }` → 204; 404 unknown user; 409
  `{ "error": "last_admin" }` (`features/assign-user-role`).
- `GET /api/admin/sign-in-settings` → `{ "methods": [{ "id", "name", "isConfigured", "isEnabled" }] }`
  (`domain/sign-in-settings`).
- `PUT /api/admin/sign-in-settings/methods/{id}` with `{ "enabled": bool }` → 204; 404 unknown method; 409
  `{ "error": "last_sign_in_method" }` (`features/toggle-sign-in-method`).
- Sign out: navigation to `/logout`.

## Commands (from the repository root)
- `pnpm install` — once for the whole workspace
- `pnpm --filter @vaultly/auth-web dev` — http://localhost:5174, proxies `/api`, `/authorize`, `/google`, `/logout`,
  `/token`, `/revoke`, `/.well-known` to `AUTH_API_PROXY_TARGET` (default `http://localhost:5096`, see `.env.example`)
- `pnpm --filter @vaultly/auth-web lint | typecheck | test | build`
- `pnpm --filter @vaultly/auth-web deps:circular` — JSON report of import cycles
- CI: `.github/workflows/auth-web.yml` (lint, test, build, Docker image build)
- Docker: `Dockerfile` builds an nginx image that serves `dist/` and proxies the Identity API paths to
  `AUTH_API_PROXY_TARGET`. The path list in `nginx.conf.template` must match `authApiPaths` in `vite.config.ts`.

## Rules (mirrored from `web/src/web/AGENTS.md`)

- **No `if`/early-return chains that render JSX.** Derive one state value in a pure, tested function (prefer lookup
  maps such as `Record<QueryStatus, Notice>` over `if` chains), map it to a small view model at a single branch point
  (e.g. `{ heading, body }`) and render shared markup once. Never repeat the same element (heading, wrapper) per state.
- **Conditional JSX elements use `&&`:** `{condition && <X />}` instead of `{condition ? <X /> : null}`; two branches
  spanning several lines as `{condition && (…)}{!condition && (…)}` instead of `{condition ? (…) : (…)}`. A two-branch
  ternary that fits on one line may stay. Keep `condition` a boolean (`items.length > 0 &&`, not `items.length &&`,
  which renders `0`). Ternaries that compute values (text, `className`, props, view models) are not affected.
- **Feature-Sliced Design with `domain` instead of `entities`**: `app → pages → widgets → features → domain → shared`.
  Import only from lower layers, never between slices of one layer, and only through a slice's `index.ts`;
  relative imports inside a slice, `@/` across slices. Enforced by `no-restricted-imports` in `eslint.config.js`.
  - `app/routes/*` stay thin (search validation, loader prefetch) and render a page.
  - `domain/authorization-request` ↔ the AuthorizationRequest aggregate of the Identity service.
  - `domain/console-admin` — who may use the admin console; `domain/user` ↔ the User aggregate (read side);
    `domain/sign-in-settings` ↔ the SignInSettings aggregate.
  - `features/toggle-sign-in-method` — the sign-in method switch.
  - `features/sign-in-with-provider` — the provider buttons (user action).
  - `features/sign-in-with-password`, `features/register-with-password` — the email and password forms.
  - `features/assign-user-role` — the role select; writes are mutations in `features/*/api`.
  - `widgets/console-shell` — frame of the admin console pages.
- **Styling**: Tailwind CSS v4 + `@vaultly/ui` only (`Button`, `Card`, `CenteredScreen`, `Eyebrow`, `StatusNotice`, …,
  tokens such as `text-muted`). No hex colors, no Sass/CSS modules; global CSS is only `app/styles/index.css`.
- **No circular dependencies**: `src/app/circularDependencies/circularDependencies.test.ts` (madge from
  `app/App.tsx`) must stay green; fix cycles by moving code down a layer.
- **User-facing copy only**: never mention tokens, cookies, OAuth, APIs or issue numbers in the UI. Route errors
  show generic messages, never raw `Error.message`.
- **i18n**: same rules as the "i18n" section of `web/src/web/AGENTS.md`. Every UI text is a translation:
  `<slice>/i18n/en.ts` per slice, generic texts in `shared/i18n/en.ts` (`common`), `document.title` in
  `app/providers/i18n/appTranslations.ts` (`app`), all combined in `app/providers/i18n/resources.ts`. The "This sign-in link is
  no longer valid" text lives in `domain/authorization-request`; signing out is `features/sign-out` (`SignOutLink`).
  `pnpm --filter @vaultly/auth-web i18n:where <ns:key>` shows where a key is used.
  **Every change that touches texts** follows the checklist in that section: `i18n:where` before editing an existing
  text (other screens using it → new key in your slice instead of editing the shared one), remove keys you stop using
  (`translationUsage.test.ts`), and run `pnpm --filter @vaultly/auth-web test` before finishing.
- Tests: Vitest + React Testing Library + MSW (`shared/test`); pages that read search params are rendered with
  `renderRoute`. Leaving the SPA goes through `navigateToUrl` (`shared/lib`) so tests can mock it.
- `src/app/routeTree.gen.ts` is generated by the router plugin (dev/build) and committed; never edit it.
