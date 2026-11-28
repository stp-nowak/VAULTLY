# Vaultly Accounts

The register of each user's financial accounts and the only source of truth for which accounts a user has, how much
is on them and what the user's net worth is. A domain service: reached only through the gateway (`../gateway`,
route `/accounts`, cluster and token audience `accounts`), built on `../core`; rules for domain services in
`../core/AGENTS.md`.

## Responsibilities
| What | How |
|---|---|
| Accounts | Open a manual account (name, type `checking` / `savings` / `credit_card` / `cash`, ISO 4217 currency, signed opening balance), rename, close, list, show one |
| Ownership | An account belongs to the token's `sub`; another user's account answers 404, never 403, so nobody learns which accounts exist |
| Balance | Signed, debt is negative; starts at the opening balance and changes only through transactions (`../transactions`), never set by hand |
| Postings | Takes `PostTransaction` (record, change or delete one transaction) from Transactions' posting saga and answers `TransactionPosted`, or `TransactionDeclined` when the account is unknown or foreign, closed, or in another currency. One `Posting` per transaction remembers what it adds to the balance and the last applied step, so a repeated step only repeats the answer ([`docs/architecture/transaction-posting.md`](../docs/architecture/transaction-posting.md)) |
| Balance history | The balance at the end of each day with a transaction, from the opening balance |
| Net worth | Per currency of the open accounts: assets (positive balances), liabilities (debts as a positive amount), their difference; no exchange rates |
| Events for other services | `AccountOpened` (owner, currency), `AccountClosed`, both with the account's version, published from `Vaultly.Accounts.PublicTypes/Events` through Rebus (queue `accounts`) |

Not here: bank connections (`./bank-sync` later feeds accounts), the transactions themselves (`../transactions`),
budgets, goals, alerts, exchange rates, households.

## Endpoints
All require the gateway's internal token; the browser reaches them as `/api/accounts…` through the web API.
Enums are `snake_case` strings; errors are `application/problem+json`: 400 invalid input, 404 unknown or foreign
account, 409 closed account.

| Endpoint | Purpose |
|---|---|
| `GET /accounts?includeClosed=false` | The user's accounts, oldest first |
| `POST /accounts` `{ name, type, currency, openingBalance }` | Opens an account; 201 with the account and `Location` |
| `GET /accounts/{id}` | One account |
| `PUT /accounts/{id}/name` `{ name }` | Renames it; 204 |
| `POST /accounts/{id}/close` | Closes it; 204. A closed account cannot be changed |
| `GET /accounts/{id}/balances?from=&to=` | `[{ date, balance }]`: the balance at the end of each day with a transaction, oldest first |
| `GET /accounts/net-worth` | `[{ currency, assets, liabilities, netWorth }]`, ordered by currency |
| `GET /health/live`, `/health/ready` | Anonymous; ready = the database answers |

## Commands
- The API's OpenAPI contract is written to `accounts/openapi/accounts.json` on every build (`Microsoft.Extensions.ApiDescription.Server`)
  and committed: the web SPA generates its types from it, and CI fails while the committed copy differs from the code.
  JSON numbers are strict (never strings), enums are `snake_case` strings.
- from `accounts/src`: `dotnet build Vaultly.Accounts.slnx`, `dotnet test Vaultly.Accounts.slnx`
- `dotnet run --project Vaultly.Accounts.Api` — http://localhost:5101; needs Postgres on 5433
  (`docker compose -f accounts/docker-compose.yml up -d accounts-db`) and, to be called, the gateway on 5000
- Docker: `docker compose -f accounts/docker-compose.yml up --build` (from the repo root) — API (5101) and Postgres
  (5433); it sends messages through RabbitMQ (`infra/docker-compose.yml`, or the root compose file for everything). Image: `src/Dockerfile` with the named contexts `core` (= `core/src`) and `identity-public-types`.
- Database schema = EF Core migrations in `src/Vaultly.Accounts.Infrastructure/Persistence/Migrations`. After a model
  change, from `accounts/src`:
  `dotnet ef migrations add <Name> --project Vaultly.Accounts.Infrastructure --startup-project Vaultly.Accounts.Infrastructure --output-dir Persistence/Migrations`
  (a test fails while the model and migrations differ). `Database:MigrateOnStartup` applies them on startup — on in
  Development, off otherwise.

## Structure
```
accounts/src/
├── Vaultly.Accounts.slnx
├── Vaultly.Accounts.Domain/
│   ├── Accounts/                       # Account aggregate (with its version, ApplyPosting), AccountId, OwnerId, AccountName,
│   │                                   #   AccountType, AccountClosedException, IAccountRepository; Events/
│   └── Postings/                       # Posting (what one transaction adds to the balance), TransactionId, IPostingRepository
├── Vaultly.Accounts.PublicTypes/       # Events/ other services subscribe to: AccountOpened, AccountClosed;
│                                       #   Commands/: PostTransaction and its replies TransactionPosted, TransactionDeclined
├── Vaultly.Accounts.Application/       # CQRS on Vaultly.Core.Messaging
│   └── Features/
│       ├── Accounts/                   # AccountDto, IAccountReadService, AccountNotFoundException (shared by the area),
│       │                               #   AccountEventsPublisher; OpenAccount, RenameAccount, CloseAccount,
│       │                               #   GetAccounts, GetAccount, GetBalanceHistory
│       ├── Postings/PostTransaction/   # applies one step; returns the decline reason or null
│       └── NetWorth/GetNetWorth/       # Query, Handler, CurrencyNetWorthDto, INetWorthReadService
├── Vaultly.Accounts.Infrastructure/    # EF Core (Npgsql)
│   ├── Accounts/                       # AccountRepository, AccountReadService, AccountConfiguration
│   ├── Postings/                       # PostingRepository, PostingConfiguration, BalanceHistoryReadService,
│   │                                   #   PostTransactionMessageHandler (Rebus: command in, reply out)
│   └── Persistence/                    # AccountsDbContext, design-time factory, migrations
├── Vaultly.Accounts.Api/               # Minimal API → ISender; Endpoints/, Http/ (JSON, problems, health), Contracts/
├── Vaultly.Accounts.Domain.Tests/
└── Vaultly.Accounts.Application.Tests/ # handlers, read service, migrations, CqrsConventionTests, API via TestGateway
```

## Rules
- Tactical DDD + CQRS as in `../core/AGENTS.md` and the `ddd` skill; invariants live in `Account` and its value
  objects, handlers only orchestrate.
- Every query and command carries the owner (`OwnerId` from `GetUserId()`); repositories and read services always
  filter by it.
- Amounts are `decimal` with at most 4 decimal places (`numeric(19,4)`); currencies are validated ISO 4217 codes.
- API tests host the real service behind `TestGateway` (`Vaultly.Core.Testing`) on an in-memory database, with
  messaging off and a `RecordingMessageBus`.
- Every change to an account raises its `Version`; events carry it so copies elsewhere can ignore late ones.
