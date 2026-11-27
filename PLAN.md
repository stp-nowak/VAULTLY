# 💰 VAULTLY
*Know where every złoty goes.*

---

## 🏦 Banking & Account Connection
- Connect bank accounts via Plaid / TrueLayer (OAuth-based, no credentials stored)
- Support multiple accounts per user (checking, savings, credit cards)
- Real-time transaction sync via webhooks
- Manual account option (for cash or unsupported banks)
- Account balance history over time
- Net worth calculation (assets minus liabilities)
- Disconnect / reconnect accounts
- Account health indicators (low balance warnings)

---

## 💳 Transactions
- Full transaction history across all accounts in one feed
- Auto-categorization engine (Food, Transport, Subscriptions, Entertainment, etc.)
- Manual category override + custom categories
- Merchant logo + name enrichment
- Split transaction across multiple categories
- Tag transactions (e.g. "vacation", "work expense")
- Search + filter (date range, category, account, amount range, merchant)
- Recurring transaction detection (subscriptions, rent, salary)
- Export to CSV / PDF
- Duplicate transaction detection

---

## 📊 Budgets
- Create monthly budgets per category
- Budget rollover option (unspent carries to next month)
- Real-time budget consumption as transactions come in
- Over-budget alerts (push + email)
- Sub-budgets (e.g. Food → Groceries, Restaurants, Takeaway)
- Budget templates (starter templates like "Student", "Family", "Minimalist")
- Historical budget performance per month
- Projected end-of-month spend based on current pace

---

## 🎯 Financial Goals
- Create savings goals (e.g. "New laptop — €1200 by December")
- Link a specific account or pot to a goal
- Auto-contribute rules (e.g. "save €200/month toward this goal")
- Goal progress visualization
- Multiple concurrent goals with priority ranking
- Goal achieved celebration + history
- Emergency fund goal type (target = N months of expenses)

---

## 📈 Analytics & Insights
- Monthly spending breakdown by category (visual, not tables)
- Spending trends over time (3/6/12 month views)
- Income vs expenses cashflow chart
- Biggest spending days / merchants
- Month-over-month comparison
- Subscription tracker (all recurring charges in one view, total monthly cost)
- Unusual spending detection ("You spent 3x more on food this week")
- Net worth timeline chart
- Savings rate percentage (income minus spend / income)

---

## 🔔 Alerts & Notifications
- Large transaction alert (above custom threshold)
- Low balance warning
- Budget category approaching limit (e.g. at 80%)
- Budget exceeded
- New recurring charge detected
- Unusual transaction / potential fraud flag
- Weekly summary digest (email)
- Monthly report (email — full breakdown)
- Salary/income detected notification

---

## 👤 Auth & Users
- Email + password registration with email verification
- OAuth (Google)
- JWT + refresh token rotation
- 2FA via authenticator app
- Password reset
- Session management (see active sessions, revoke)
- Account deletion with data wipe (GDPR)

---

## 👨‍👩‍👧 Shared Finance (Households)
- Create a household group
- Invite partner / family members
- Roles — Owner, Member, Viewer
- Shared budget view across all members' accounts (with permission)
- Shared goals (e.g. "Family holiday fund")
- Each member keeps private accounts if desired
- Household net worth view
- Split expense tracking between members

---

## 💎 Subscription Tiers (Stripe)
- **Free** — 1 bank connection, basic budgets, 3 months history
- **Pro (€7.99/mo)** — unlimited connections, full history, goals, insights, alerts, household
- **Annual discount** — 2 months free
- Stripe billing portal (upgrade, downgrade, cancel)
- Payment history
- Grace period on failed payments before downgrade

---

## 🛡️ Admin Panel
- User management (view, suspend, delete)
- Subscription overview (MRR, churn, active subscribers)
- Platform health (bank connection error rates, webhook delivery stats)
- Feature flag management
- Manual transaction category correction tools
- Support ticket queue

---

## 🔐 Security & Privacy
- All bank tokens encrypted at rest
- No bank credentials ever stored (Plaid/TrueLayer handles it)
- Audit log of all data access
- GDPR data export
- Read-only bank access (can never move money)
- Rate limiting on all endpoints
- IP-based suspicious login detection

---

---

## 🏗️ Microservices Architecture

```
            ┌──────────────────────────────┐
            │ ./web/src/web  React SPA     │   browser: HttpOnly session cookie only
            └──────────────┬───────────────┘
                           │ same-origin /api/* + X-CSRF
            ┌──────────────▼───────────────┐
            │ ./web/src/api  Web API (BFF) │── Redis: tokens per session (encrypted)
            └──────┬────────────────┬──────┘
                   │ OAuth code /   │ Bearer access token (Identity JWT)
                   │ refresh        │
┌──────────────────▼───┐   ┌────────▼────────────────────────────────────┐
│ ./auth               │   │ ./gateway  API Gateway (YARP in C#)         │── Redis (planned): revoked
│ Identity Service     │◀──│ validates access token (JWKS), routes,      │   sessions, user plans
│ (standalone, own     │   │ issues an internal token per service        │
│  solution, ./core)   │   └───┬──────┬──────┬──────┬──────┬──────┬──────┘
│ JWKS / OIDC discovery│       │      │      │      │      │      │   Bearer internal token
│ Vaultly.Identity.    │     Bank  Trans- Budgets Goals Alerts Billing  (aud = that service)
│ PublicTypes          │     Sync  actions
└──────────┬───────────┘       │      │      │      │      │      │
           │                  PG     PG     PG     PG   Redis   PG
          PG                                             +PG   +Stripe
           └──────────────── RabbitMQ (integration events) ─────────┘
           every domain service builds on ./core (Vaultly.Core, .Persistence, .Authentication)
```

**Repository layout**
- `./web` — web app. `src/web`: React SPA, Feature-Sliced Design with a `domain` layer instead of `entities`
  (one `domain/<context>` slice per service's bounded context, e.g. `domain/identity` ↔ `./auth`).
  `src/api`: C# web API (backend-for-frontend) — browser gets only an HttpOnly session cookie, tokens live in Redis,
  `/api/*` is forwarded to the gateway with the session's bearer token.
- `./gateway` — API gateway (YARP): the only entry to the domain services. Validates Identity's access tokens, applies
  route policies (signed in / admin / anonymous for webhooks), forwards each request with a short-lived internal token
  whose audience is the called service. Planned: instant session revocation and plan-based feature gates/rate limits.
- `./core` — `Vaultly.Core` (DDD building blocks) and `Vaultly.Core.Authentication` (internal token validation),
  referenced by every domain service.
- Domain services — one top-level folder each, named after what it does (`./accounts`, `./bank-sync`, `./transactions`,
  `./budgets`, `./goals`, `./alerts`, `./billing`, `./admin`); each is a bounded context in **tactical DDD + CQRS**
  (`Domain` / `Application` with `Features/<Area>/<UseCase>` commands and queries / `Infrastructure` / `Api`), with its own solution, compose
  file, CI workflow and database. Never a generic `./backend`.
- `./auth` — Identity service, **completely separate** (own solution and DB; DDD building blocks from `./core`), same tactical DDD + CQRS
  rules. Owns `Vaultly.Identity.PublicTypes` (claims, `UserRole`, later its integration events) and
  `Vaultly.Identity.ApiContracts` (its HTTP API models).
- Code shared between services lives in one project owned by one service (`PublicTypes`, `ApiContracts`) and is
  referenced, never copied; it would become NuGet packages only if the repository were split.

| Service | Responsibility | DB |
|---|---|---|
| **Identity** (`./auth`) | Auth, users, sessions, 2FA | Postgres |
| **Gateway** (`./gateway`) | Single entry, token validation, internal tokens, routing; later revocation, plan gates, rate limits | Redis (planned) |
| **Accounts** (`./accounts`) | The user's financial accounts (manual first), balances, net worth per currency; balances change through Transactions' events | Postgres |
| **Bank Sync** (`./bank-sync`) | Plaid/TrueLayer connection, webhooks; feeds linked accounts and their balances to Accounts | Postgres |
| **Transactions** (`./transactions`) | Transaction store, categorization, search, enrichment | Postgres |
| **Budgets** (`./budgets`) | Budget rules, consumption tracking, rollover | Postgres |
| **Goals** (`./goals`) | Savings goals, contributions, progress | Postgres |
| **Alerts** (`./alerts`) | Rules engine, notification delivery, digests | Postgres + Redis |
| **Billing** (`./billing`) | Stripe subscriptions, plans, webhooks | Postgres |
| **Admin** (`./admin`) | Platform oversight, reads via events | Postgres |

---

## 🗺️ Phased Development Plan

---

**Important**: Domain services and auth (`./auth`) follow tactical DDD with CQRS. Frontend follows Feature-Sliced Design with a `domain` layer instead of `entities`.

### 🔴 Phase 1 — Foundation (Weeks 1–3)
*Goal: Docker running, auth solid, user can register*

**Infrastructure**
- Docker Compose — all service containers, Postgres instances, RabbitMQ, Redis
- YARP API Gateway (`./gateway`): access token validation, routing config, internal tokens per service ✅
- One solution per service (`auth/src/Vaultly.Auth.slnx`, `gateway/src/Vaultly.Gateway.slnx`, `core/src/Vaultly.Core.slnx`, one per domain service)
- Shared projects: `Vaultly.Identity.PublicTypes` ✅, `Vaultly.Identity.ApiContracts` ✅, `Vaultly.Core` (+ CQRS messaging) + `Vaultly.Core.Persistence` + `Vaultly.Core.Authentication` + `Vaultly.Core.Testing` ✅, later RabbitMQ base, middleware
- CI pipeline (GitHub Actions — build + test on PR)

**Identity Service**
- .Net 10
- Register, email verify, login, logout
- JWT access token + refresh token rotation
- Password reset via email
- Google OAuth
- 2FA with authenticator app
- Session management endpoints

**Frontend**
- React + TypeScript + Vite setup
- TanStack Router for routing
- Auth context + protected routes
- Register / Login / Verify / Reset pages
- App shell — sidebar nav, topbar, responsive layout
- Dark mode from day one (finance apps look better dark)

**Deliverable:** User can sign up, verify, log in, see empty dashboard.

---

### 🟠 Phase 2 — Bank Connection & Transactions (Weeks 4–7)
*Goal: Real (sandbox) bank connected, transactions flowing in*

**Accounts Service** (`./accounts`)
- Manual accounts: open with type, currency and opening balance, rename, close, list ✅
- Net worth per currency ✅
- Balance changes through manual transactions (Transactions events over RabbitMQ), balance history

**Bank Sync Service**
- Plaid sandbox setup (or TrueLayer for EU)
- Connect bank account flow (OAuth handshake)
- Store encrypted access tokens
- Fetch accounts + balances on connect
- Initial transaction backfill (last 90 days)
- Webhook receiver — real-time transaction updates
- Disconnect account flow

**Transaction Service**
- Manual transactions on manual accounts; Accounts updates the balance from their events
- Transaction ingestion from Bank Sync (via RabbitMQ)
- Auto-categorization engine (rule-based first, then improvable)
- Merchant name enrichment
- Recurring transaction detection algorithm
- Search + filter API
- Manual category override
- Tag system
- CSV export

**Frontend**
- Bank connection flow (Plaid Link widget embedded)
- Connected accounts list with balances
- Transaction feed — unified across all accounts
- Category badges, merchant logos, search + filters
- Transaction detail drawer (edit category, add tags, split)
- Net worth widget on dashboard

**Deliverable:** Sandbox bank connected, transactions visible, categorized, searchable.

---

### 🟡 Phase 3 — Budgets (Weeks 8–10)
*Goal: User can set budgets and see them update in real time*

**Budget Service**
- Create / edit / delete budgets per category
- Real-time consumption updates (listens to Transaction events via RabbitMQ)
- Rollover logic (month end processing)
- Sub-budget support
- Projected end-of-month spend calculation
- Budget template presets

**Alert Service (partial)**
- Budget at 80% alert
- Budget exceeded alert
- Deliver via in-app notification + email

**Frontend**
- Budget creation flow with category picker
- Budget dashboard — ring/arc progress per category
- Live consumption update when new transaction arrives (WebSocket)
- Over-budget visual state (color shift, warning)
- Monthly budget history view
- Budget template selector for new users

**Deliverable:** Budgets set, update live as transactions come in, alerts fire.

---

### 🟢 Phase 4 — Goals & Analytics (Weeks 11–13)
*Goal: User can plan their financial future, see meaningful insights*

**Goal Service**
- Create / edit savings goals
- Link account to goal
- Auto-contribute rules
- Progress calculation
- Goal completion flow

**Analytics (within Transaction + Budget services)**
- Monthly spending breakdown endpoint
- Cashflow (income vs expense) over time
- Spending trends (3/6/12 months)
- Subscription tracker (all recurring charges)
- Unusual spending detection
- Savings rate calculation

**Frontend**
- Goals page — cards with progress arcs, timeline to completion
- Add contribution flow
- Analytics dashboard — cashflow chart, category breakdown, trend lines
- Subscription tracker view (all recurring charges, monthly total)
- Spending heatmap (day of week / time of month)
- Month comparison view

**Deliverable:** Goals trackable, analytics dashboard telling a real story about spending.

---

### 🔵 Phase 5 — Billing & Subscription Tiers (Weeks 14–15)
*Goal: Monetization working, free vs pro feature gating*

**Billing Service**
- Stripe subscription setup (Free / Pro / Annual)
- Stripe billing portal integration
- Webhook handler (subscription created, cancelled, payment failed)
- Feature flag system tied to subscription tier (gateway: user plan cached from Billing events, routes gated by plan)
- Grace period logic on failed payment
- Downgrade flow (data retained, features locked)

**Frontend**
- Pricing page
- Upgrade flow with Stripe Checkout
- Billing portal link (manage / cancel)
- Feature gate UI (blurred/locked sections with upgrade prompt)
- Payment history page
- Trial / grace period banner

**Deliverable:** Stripe subscriptions working, free users hit limits, pro unlocks everything.

---

### 🟣 Phase 6 — Households & Sharing (Weeks 16–18)
*Goal: Couples and families can manage finances together*

**Identity Service (extension)**
- Household creation
- Member invite via email
- Role management (Owner / Member / Viewer)
- Permission system for shared vs private accounts

**Budget + Goal Services (extension)**
- Shared budget creation across household
- Shared goal contributions from multiple members
- Household net worth aggregation

**Frontend**
- Household setup + invite flow
- Shared dashboard view
- Member permission management
- Toggle between personal and household view
- Shared goal contribution UI

**Deliverable:** Two users can share a financial view and collaborate on goals.

---

### ⚫ Phase 7 — Alerts, Polish & Hardening (Weeks 19–21)
*Goal: Production-ready, feels like a real product*

**Alert Service (complete)**
- Large transaction alerts
- Low balance warnings
- Unusual spending detection alerts
- Weekly summary email digest
- Monthly report email (full PDF breakdown)
- Salary detected notification
- User alert preferences (which alerts, which channels)

**Cross-cutting**
- Serilog + Seq for centralized logging
- Health check endpoints per service
- Rate limiting (gateway, per user and plan)
- Instant session revocation at the gateway (Identity session events → Redis)
- GDPR data export endpoint
- Full audit log
- Admin panel — users, subscriptions, platform health
- IP-based suspicious login detection

**Frontend**
- Notification center (in-app bell)
- Alert preferences settings page
- Monthly report PDF download
- Onboarding checklist for new users (connect bank → set budget → create goal)
- Empty states everywhere (not blank, guided)
- Full mobile responsiveness

**Deliverable:** Something you'd genuinely be proud to put in a portfolio or ship.

---

## 📋 Summary

| Phase | Focus | Weeks |
|---|---|---|
| 1 | Foundation + Auth | 1–3 |
| 2 | Bank Connection + Transactions | 4–7 |
| 3 | Budgets + Live Updates | 8–10 |
| 4 | Goals + Analytics | 11–13 |
| 5 | Stripe Billing + Feature Gating | 14–15 |
| 6 | Households + Sharing | 16–18 |
| 7 | Alerts + Polish + Hardening | 19–21 |

---