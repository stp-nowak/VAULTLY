# Vaultly Transactions

The transactions users enter on their manual accounts. A domain service: reached only through the gateway
(`../gateway`, route `/transactions`, cluster and token audience `transactions`), built on `../core`; rules for
domain services in `../core/AGENTS.md`. Accounts owns the balances: every change of a transaction that moves money is
applied there by the posting saga ([`docs/architecture/transaction-posting.md`](../docs/architecture/transaction-posting.md)).

## Responsibilities
| What | How |
|---|---|
| Transactions | Record one on an open account of the user (signed amount in the account's currency, day, description, optional note), change it, delete it, list (by account and days), show one |
| Ownership | A transaction belongs to the token's `sub`; another user's transaction or account answers 404 |
| Posting saga | Recording, changing amount or day, and deleting are steps that Accounts applies to the balance (`PostTransaction`); the transaction is `pending` until Accounts answers and takes no other step meanwhile. A declined step is undone: a new transaction becomes `rejected` (it can only be deleted), a change goes back to the posted amount and day, a deletion back to `posted`. Description and note change at once |
| Accounts | A copy of each account (owner, currency, closed) kept from Accounts' `AccountOpened` / `AccountClosed`, older versions ignored; an account Transactions has not heard of yet answers 404 |

Not here: balances and their history (`../accounts`), categories, tags, search, recurring detection, bank imports
(`./bank-sync` later), transfers between accounts.

## Endpoints
All require the gateway's internal token; the browser reaches them as `/api/transactions…` through the web API.
Enums are `snake_case` strings; errors are `application/problem+json`: 400 invalid input, 404 unknown or foreign
transaction or account, 409 closed account, a pending or rejected transaction, or a concurrent change.

| Endpoint | Purpose |
|---|---|
| `GET /transactions?accountId=&from=&to=&limit=&cursor=` | One page of the user's transactions (deleted ones left out), newest day first and, within a day, newest recorded first: `{ items, nextCursor }`. `limit` 1–200 (default 50); pass `nextCursor` back as `cursor` for the next page, `null` on the last. An invalid cursor or limit is 400 |
| `POST /transactions` `{ accountId, amount, bookedOn, description, note }` | Records one; 201 with it (`status: pending`) and `Location` |
| `GET /transactions/{id}` | One transaction: `status` (`pending` / `posted` / `rejected`), `pendingStep`, `declineReason` |
| `PUT /transactions/{id}` `{ amount, bookedOn, description, note }` | 202 when the amount or day changed (pending), 204 when only the text did |
| `DELETE /transactions/{id}` | 202 for a posted transaction (pending until Accounts takes it off), 204 for a rejected one |
| `GET /health/live`, `/health/ready` | Anonymous; ready = the database answers |

## Commands
- The API's OpenAPI contract is written to `transactions/openapi/transactions.json` on every build (`Microsoft.Extensions.ApiDescription.Server`)
  and committed: the web SPA generates its types from it, and CI fails while the committed copy differs from the code.
  JSON numbers are strict (never strings), enums are `snake_case` strings.
- from `transactions/src`: `dotnet build Vaultly.Transactions.slnx`, `dotnet test Vaultly.Transactions.slnx` (the page
  tests start Postgres with Testcontainers, so Docker must run)
- `dotnet run --project Vaultly.Transactions.Api` — http://localhost:5102; needs Postgres on 5434
  (`docker compose -f transactions/docker-compose.yml up -d transactions-db`), RabbitMQ on 5672
  (`docker compose -f infra/docker-compose.yml up -d`), Accounts to answer the saga and, to be called, the gateway
- Docker: `docker compose -f transactions/docker-compose.yml up --build` (from the repo root) — API (5102) and Postgres
  (5434). Image: `src/Dockerfile` with the named contexts `core` (= `core/src`), `accounts-public-types` and
  `identity-public-types`.
- Database schema = EF Core migrations in `src/Vaultly.Transactions.Infrastructure/Persistence/Migrations`. After a
  model change, from `transactions/src`:
  `dotnet ef migrations add <Name> --project Vaultly.Transactions.Infrastructure --startup-project Vaultly.Transactions.Infrastructure --output-dir Persistence/Migrations`
  (a test fails while the model and migrations differ). Rebus creates its own tables (`rebus_*`: outbox, sagas,
  timeouts).

## Structure
```
transactions/src/
├── Vaultly.Transactions.slnx
├── Vaultly.Transactions.Domain/
│   ├── Transactions/                   # Transaction aggregate (steps, compensation), TransactionId, OwnerId,
│   │                                   #   TransactionDescription, TransactionNote, TransactionStatus, PostingStep,
│   │                                   #   DeclineReason, PostingRequested, ITransactionRepository; Exceptions/
│   └── Accounts/                       # Account (copy of Accounts' account), AccountId, AccountClosedException,
│                                       #   IAccountRepository
├── Vaultly.Transactions.Application/
│   └── Features/
│       ├── Transactions/               # TransactionDto, ITransactionReadService, TransactionNotFoundException,
│       │                               #   PostingRequestedHandler (starts the saga); RecordTransaction,
│       │                               #   ChangeTransaction, DeleteTransaction, GetTransactions, GetTransaction
│       ├── Postings/                   # StartPosting (saga's first message); ConfirmPosting, DeclinePosting
│       └── Accounts/                   # TrackAccountOpened, TrackAccountClosed
├── Vaultly.Transactions.Infrastructure/
│   ├── Transactions/                   # TransactionRepository, TransactionReadService, TransactionConfiguration
│   ├── Accounts/                       # AccountRepository, AccountConfiguration, AccountEventsMessageHandler
│   ├── Postings/                       # TransactionPostingSaga, PostingSagaData, PostingTimedOut
│   └── Persistence/                    # TransactionsDbContext, design-time factory, migrations
├── Vaultly.Transactions.Api/           # Minimal API → ISender; Endpoints/, Http/ (JSON, problems, health), Contracts/
├── Vaultly.Transactions.Domain.Tests/
└── Vaultly.Transactions.Application.Tests/ # handlers, saga (Rebus SagaFixture), read service, migrations,
                                            #   CqrsConventionTests, API via TestGateway
```

## Rules
- Tactical DDD + CQRS as in `../core/AGENTS.md` and the `ddd` skill; the step rules and their compensation live in
  `Transaction`, the saga only carries messages, and Rebus handlers only forward to commands.
- Every query and command from the API carries the owner (`OwnerId` from `GetUserId()`); answers from Accounts find
  the transaction by id alone.
- Every message handler is idempotent: a step is identified by its `StepId`, an answer to a step that is no longer
  pending changes nothing, and an account event older than the copy is ignored.
- Amounts and currencies are core's `Money` and `Currency`; a transaction is always in its account's currency.
- API tests host the real service behind `TestGateway` on an in-memory database with messaging off; they drive the
  saga's outcome by sending `ConfirmPosting` / `DeclinePosting` directly.
- Lists page with a cursor (keyset), never with an offset: the cursor is the last row's day and its `Sequence` (a
  database identity column, shadow property of `Transaction`), so pages neither skip nor repeat rows while
  transactions are added. The in-memory database does not generate `Sequence`, so ordering within a day is tested on
  Postgres (`TransactionPagesTests`).
