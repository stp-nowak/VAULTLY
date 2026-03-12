---
name: ddd
description: >
  Apply Domain-Driven Design (DDD) principles when designing or implementing software systems.
  Use this skill whenever the user asks to scaffold a service, design a domain model, create aggregates,
  define value objects, plan bounded contexts, implement domain events, structure a C# project,
  or asks anything about DDD architecture. Also trigger when the user says things like
  "create a service", "design the domain", "model this feature", "how should I structure this",
  "add a new feature to an existing DDD project", or "implement X using DDD". When in doubt, use this skill.
---

# Domain-Driven Design (DDD) Skill

This skill ensures all code and architecture follows proper DDD principles. Read this fully before
generating any domain model, service scaffold, aggregate, or bounded context design.

---

## Core Philosophy

DDD is NOT about folder structure. It is about:

1. **Modelling the business problem** in code — the code should read like the business speaks
2. **Protecting invariants** — illegal states must be unrepresentable
3. **Isolating business logic** from infrastructure, frameworks, and databases
4. **Making boundaries explicit** — each context owns its data and decisions

The domain layer must have **zero dependencies** on infrastructure. No EF Core, no HTTP clients,
no RabbitMQ, no logging frameworks inside the domain. Pure C# only.

---

## Bounded Contexts

Each bounded context is an autonomous service or module with:
- Its own domain model
- Its own database (database-per-service in microservices)
- Its own definition of shared concepts (e.g. `User` means something different in each context)

### Rules
- Never share a domain model across bounded contexts
- Never query another context's database directly
- Each context stores only the minimal slice of foreign data it needs
- Cross-context communication uses **events** (async) or **HTTP** (sync, rare)

### Cross-context data (local cache pattern)
When a context needs data from another context, it stores a minimal local projection updated by events:

```csharp
// Budget context owns this table — populated from Billing events, never queried from Billing DB
public class UserPlanProjection
{
    public Guid UserId { get; private set; }
    public PlanTier Plan { get; private set; }        // only what Budget needs
    public DateTime LastSyncedAt { get; private set; }
}
```

---

## Project Structure (per service / bounded context)

```
ServiceName/
├── Domain/                         # Zero dependencies. Pure business logic only.
│   ├── Aggregates/
│   ├── Entities/
│   ├── ValueObjects/
│   ├── Events/                     # Domain events (raised inside aggregates)
│   ├── Services/                   # Domain services (stateless logic spanning aggregates)
│   └── Exceptions/                 # Domain-specific exceptions
│
├── Application/                    # Orchestration layer. Depends on Domain only.
│   ├── UseCases/
│   │   └── FeatureName/
│   │       ├── FeatureNameCommand.cs   (or Query)
│   │       └── FeatureNameHandler.cs
│   ├── EventHandlers/              # Handles domain or integration events
│   └── Interfaces/                 # ← Repository interfaces AND external service abstractions live here
│       ├── Repositories/           #   e.g. IOrderRepository, ICustomerRepository
│       └── Services/               #   e.g. IEmailService, IStorageService
│
├── Infrastructure/                 # Implements Application interfaces. Depends on Domain + Application.
│   ├── Persistence/
│   │   ├── DbContext.cs
│   │   ├── Repositories/           # Implements IRepository interfaces from Application
│   │   └── Migrations/
│   ├── Messaging/                  # RabbitMQ, Kafka publishers/consumers
│   └── ExternalServices/           # Third-party API clients
│
└── API/                            # Entry point. Wires everything together via DI.
    ├── Controllers/
    └── Program.cs                  # References Application + Infrastructure for DI registration
```

**Dependency rule (strict):**
```
Domain     → nothing
Application → Domain
Infrastructure → Domain + Application   (implements Application interfaces)
API        → Application + Infrastructure   (API must reference Infrastructure to register implementations in DI)
```

> **Why does API reference Infrastructure?**
> `Program.cs` must call `services.AddScoped<IOrderRepository, OrderRepository>()` and similar
> registrations. Without a project reference to Infrastructure, the concrete types are invisible to
> the DI container. This is the *composition root* — the one place in the system where abstractions
> are bound to implementations. Referencing Infrastructure from API does **not** mean controllers
> use it directly; they only talk to Application (via MediatR commands/queries).

> **Why do repository interfaces live in Application, not Domain?**
> The Domain layer should contain nothing but pure business concepts: aggregates, value objects,
> domain events, domain services, and domain exceptions. It must not know that persistence even
> exists. It is the **Application layer** (use cases) that needs to load and save aggregates, so
> it is the Application layer that defines the repository contracts (ports). Infrastructure then
> provides the implementations (adapters). This follows the Ports & Adapters / Hexagonal Architecture
> principle and is the approach used in Clean Architecture templates for .NET.

---

## Aggregates

An aggregate is a cluster of objects that must stay consistent together. It has one **Aggregate Root**
which is the only public entry point — all state changes go through it.

### Rules
- All state changes go through the aggregate root — never mutate child entities directly
- Aggregates should be small — if you have more than 3-4 entities, consider splitting
- Load the full aggregate, mutate it, save it — never partial updates
- Raise domain events inside the aggregate when something meaningful happens
- Use private setters on all properties — enforce change through methods

### Template (C#)

```csharp
public class Order : AggregateRoot   // AggregateRoot from SharedKernel
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money Total { get; private set; }

    private readonly List<OrderLine> _lines = new();
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    private Order() { }  // Required by EF Core — keep private

    // Factory method — enforces creation rules
    public static Order Create(Guid customerId, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(customerId);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = OrderStatus.Draft,
            Total = Money.Zero(currency)
        };

        order.RaiseDomainEvent(new OrderCreated(order.Id, customerId));
        return order;
    }

    public void AddLine(ProductId productId, Money price, int quantity)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOrderStateException("Cannot add lines to a non-draft order");

        if (quantity <= 0)
            throw new InvalidQuantityException(quantity);

        _lines.Add(OrderLine.Create(productId, price, quantity));
        RecalculateTotal();
    }

    public void Submit()
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOrderStateException("Only draft orders can be submitted");

        if (!_lines.Any())
            throw new EmptyOrderException("Cannot submit an order with no lines");

        Status = OrderStatus.Submitted;
        RaiseDomainEvent(new OrderSubmitted(Id, CustomerId, Total));
    }

    private void RecalculateTotal() =>
        Total = _lines.Aggregate(Money.Zero(Total.Currency), (acc, l) => acc.Add(l.Subtotal));
}
```

---

## Entities

Entities have identity (an ID) and can change over time. They live inside an aggregate and are
only accessible through the aggregate root.

```csharp
public class OrderLine     // Entity — no public constructor, created via factory
{
    public Guid Id { get; private set; }
    public ProductId ProductId { get; private set; }
    public Money UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public Money Subtotal => UnitPrice.Multiply(Quantity);

    private OrderLine() { }

    internal static OrderLine Create(ProductId productId, Money price, int quantity)
    {
        return new OrderLine
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            UnitPrice = price,
            Quantity = quantity
        };
    }
}
```

---

## Value Objects

Value objects have **no identity**. Two value objects with the same values are equal. They are
**immutable** — never change them, create new ones.

Use C# `record` types. Always validate in the constructor.

### Rules
- Immutable — no setters, no mutation methods
- Self-validating — throw domain exceptions in constructor if invalid
- Replace ALL primitives in the domain (no raw `string email`, `decimal amount`)
- Equality is by value, not reference

```csharp
// Money — the most important value object in any financial system
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0) throw new InvalidMoneyException($"Amount cannot be negative: {amount}");
        if (string.IsNullOrWhiteSpace(currency)) throw new InvalidMoneyException("Currency is required");
        Amount = Math.Round(amount, 2);
        Currency = currency.ToUpperInvariant();
    }

    public static Money Zero(string currency) => new(0, currency);
    public static Money Zero(Currency currency) => new(0, currency.Value);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        if (other.Amount > Amount) throw new InsufficientFundsException(this, other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(int factor) => new(Amount * factor, Currency);

    public bool IsGreaterThan(Money other) { EnsureSameCurrency(other); return Amount > other.Amount; }
    public bool IsZero => Amount == 0;

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new CurrencyMismatchException(Currency, other.Currency);
    }
}

// Email value object
public sealed record Email
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new InvalidEmailException("Email cannot be empty");
        if (!value.Contains('@')) throw new InvalidEmailException($"Invalid email format: {value}");
        Value = value.Trim().ToLowerInvariant();
    }
}

// Strongly-typed IDs — never use raw Guid for entity identity
public sealed record OrderId(Guid Value)
{
    public static OrderId New() => new(Guid.NewGuid());
    public static OrderId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
```

---

## Domain Events

Domain events represent something that **happened** in the domain. They are raised inside aggregates
and dispatched after the aggregate is saved. Other services (or handlers in the same service) react
to them without the aggregate knowing who listens.

### Rules
- Past tense naming: `OrderSubmitted`, `BudgetExceeded`, `UserRegistered`
- Immutable records — only data, no logic
- Raised inside the aggregate, dispatched by infrastructure after save
- In-process events use MediatR; cross-service events use RabbitMQ / message bus

```csharp
// SharedKernel base
public abstract record DomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

// Domain events
public record OrderSubmitted(
    Guid OrderId,
    Guid CustomerId,
    Money Total) : DomainEvent;

public record BudgetExceeded(
    Guid BudgetId,
    Guid UserId,
    string Category,
    Money Limit,
    Money ActualSpend) : DomainEvent;

// AggregateRoot base in SharedKernel
public abstract class AggregateRoot
{
    private readonly List<DomainEvent> _events = new();
    public IReadOnlyCollection<DomainEvent> DomainEvents => _events.AsReadOnly();

    protected void RaiseDomainEvent(DomainEvent evt) => _events.Add(evt);
    public void ClearDomainEvents() => _events.Clear();
}
```

### Dispatching after save (Unit of Work pattern)

```csharp
public async Task SaveChangesAsync(CancellationToken ct = default)
{
    // 1. Collect events before saving
    var aggregates = _dbContext.ChangeTracker
        .Entries<AggregateRoot>()
        .Select(e => e.Entity)
        .Where(a => a.DomainEvents.Any())
        .ToList();

    // 2. Save to DB first
    await _dbContext.SaveChangesAsync(ct);

    // 3. Dispatch in-process events via MediatR
    foreach (var aggregate in aggregates)
    {
        foreach (var evt in aggregate.DomainEvents)
            await _mediator.Publish(evt, ct);

        aggregate.ClearDomainEvents();
    }
}
```

---

## Domain Services

Use a domain service when logic spans multiple aggregates and doesn't naturally belong to any one of them.

### When to use
- Logic requires two or more aggregates
- Logic is stateless (no stored state)
- Logic represents a business concept that isn't an entity ("TransferMoney", "CalculateRisk")

```csharp
// Domain service — lives in Domain/Services/
public class FundsTransferService
{
    // No repositories injected here — passed in or called from application layer
    public void Transfer(BankAccount source, BankAccount destination, Money amount)
    {
        if (!source.CanDebit(amount))
            throw new InsufficientFundsException(source.Id, amount);

        source.Debit(amount);
        destination.Credit(amount);

        // Both aggregates raise their own domain events internally
    }
}
```

---

## Repositories

Repository interfaces live in the **Application layer** (`Application/Interfaces/Repositories/`).
Implementations live in **Infrastructure**. Repositories work only with aggregate roots — never
with raw entities or value objects.

The Domain layer has no persistence concepts at all — it only defines aggregates. The Application
layer defines what it *needs* from persistence (the interface / port), and Infrastructure delivers
the concrete implementation (the adapter).

```csharp
// Application/Interfaces/Repositories/IOrderRepository.cs
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default);
    Task<IEnumerable<Order>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
    Task UpdateAsync(Order order, CancellationToken ct = default);
}

// Infrastructure/Persistence/Repositories/OrderRepository.cs  — implements Application interface
public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _dbContext;

    public OrderRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default) =>
        await _dbContext.Orders
            .Include(o => o.Lines)       // Load full aggregate
            .FirstOrDefaultAsync(o => o.Id == id.Value, ct);

    public async Task AddAsync(Order order, CancellationToken ct = default) =>
        await _dbContext.Orders.AddAsync(order, ct);

    public async Task UpdateAsync(Order order, CancellationToken ct = default) =>
        _dbContext.Orders.Update(order);
}
```

---

## Application Layer (Use Cases)

The application layer orchestrates the domain. It:
- Receives a command or query
- Loads aggregate(s) from repository
- Calls domain methods
- Saves via repository
- Does NOT contain business logic

Use MediatR for command/query handling (CQRS pattern).

```csharp
// Application/UseCases/SubmitOrder/SubmitOrderCommand.cs
public record SubmitOrderCommand(Guid OrderId, Guid UserId) : IRequest<SubmitOrderResult>;

// Application/UseCases/SubmitOrder/SubmitOrderHandler.cs
public class SubmitOrderHandler : IRequestHandler<SubmitOrderCommand, SubmitOrderResult>
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _unitOfWork;

    public SubmitOrderHandler(IOrderRepository orders, IUnitOfWork unitOfWork)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
    }

    public async Task<SubmitOrderResult> Handle(SubmitOrderCommand cmd, CancellationToken ct)
    {
        // 1. Load aggregate
        var order = await _orders.GetByIdAsync(OrderId.From(cmd.OrderId), ct)
            ?? throw new OrderNotFoundException(cmd.OrderId);

        // 2. Authorize
        if (order.CustomerId != cmd.UserId)
            throw new UnauthorizedException();

        // 3. Execute domain logic — all business rules enforced inside
        order.Submit();

        // 4. Save (dispatches domain events after save)
        await _unitOfWork.SaveChangesAsync(ct);

        return new SubmitOrderResult(order.Id);
    }
}
```

---

## Domain Exceptions

Domain exceptions communicate rule violations in business language. Never throw generic exceptions
from the domain.

```csharp
// Base
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

// Specific
public class InvalidOrderStateException : DomainException
{
    public InvalidOrderStateException(string message) : base(message) { }
}

public class InsufficientFundsException : DomainException
{
    public InsufficientFundsException(Money available, Money requested)
        : base($"Insufficient funds. Available: {available.Amount} {available.Currency}, Requested: {requested.Amount}") { }
}
```

Map to HTTP status codes in the API layer, not the domain.

---

## SharedKernel

The SharedKernel is a NuGet package / shared project used by all services. It contains only:

- `AggregateRoot` base class
- `DomainEvent` base record
- `DomainException` base class
- Generic value object helpers
- Common interfaces (`IUnitOfWork`)

**Do NOT put business logic in SharedKernel.** It must never import anything service-specific.

---

## EF Core Configuration (Infrastructure)

EF Core should never leak into the domain. Configure mappings in Infrastructure using `IEntityTypeConfiguration`.

```csharp
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);

        // Map strongly-typed ID
        builder.Property(o => o.Id)
            .HasConversion(id => id.Value, value => OrderId.From(value));

        // Map value object (owned entity)
        builder.OwnsOne(o => o.Total, money =>
        {
            money.Property(m => m.Amount).HasColumnName("total_amount").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("total_currency").HasMaxLength(3);
        });

        // Map enum
        builder.Property(o => o.Status)
            .HasConversion<string>();

        // Map collection of child entities
        builder.HasMany(o => o.Lines)
            .WithOne()
            .HasForeignKey("OrderId")
            .OnDelete(DeleteBehavior.Cascade);

        // Private field backing for collection
        builder.Navigation(o => o.Lines).HasField("_lines");
    }
}
```

---

## DI Registration (API / Composition Root)

`Program.cs` is the **only** place where abstractions are bound to implementations. It references
both Application and Infrastructure. Controllers and handlers never touch Infrastructure types directly.

```csharp
// API/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Application layer registrations (MediatR, validators, etc.)
builder.Services.AddApplication();  // extension method in Application project

// Infrastructure layer registrations — concrete types wired to Application interfaces
builder.Services.AddInfrastructure(builder.Configuration);  // extension method in Infrastructure project

// Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseNpgsql(config.GetConnectionString("Default")));

        // Bind Application interfaces to Infrastructure implementations
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEmailService, SmtpEmailService>();

        return services;
    }
}
```

> **Rule:** Controllers inject only Application types (e.g. `ISender` from MediatR). The API project
> references Infrastructure **only** to call `AddInfrastructure()` in `Program.cs`. This keeps the
> coupling intentional and contained.

---

## Integration Events (Cross-Service)

Integration events are published to the message bus after a transaction commits. They are different
from domain events — they cross service boundaries and must be serialization-safe.

```csharp
// Vaultly.Contracts project — shared DTOs only, no domain logic
public record TransactionImportedIntegrationEvent
{
    public Guid TransactionId { get; init; }
    public Guid UserId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; }
    public string Category { get; init; }
    public DateTime OccurredAt { get; init; }
}

// In Transaction service — publish after domain event fires
public class TransactionImportedHandler : INotificationHandler<TransactionImported>
{
    private readonly IMessageBus _bus;

    public async Task Handle(TransactionImported evt, CancellationToken ct)
    {
        await _bus.PublishAsync(new TransactionImportedIntegrationEvent
        {
            TransactionId = evt.TransactionId,
            UserId = evt.UserId,
            Amount = evt.Amount.Amount,
            Currency = evt.Amount.Currency,
            Category = evt.Category.Value,
            OccurredAt = evt.OccurredAt
        });
    }
}
```

---

## Checklist — Before Generating Any Domain Code

Before writing domain code, answer these questions:

- [ ] What is the **aggregate root**? What cluster of objects must stay consistent together?
- [ ] What are the **invariants**? What must always be true? (enforce inside aggregate methods)
- [ ] Which properties should be **value objects** instead of primitives?
- [ ] Which domain **events** should be raised when state changes?
- [ ] Does any logic span multiple aggregates? (→ domain service)
- [ ] Does the aggregate need data from another context? (→ local projection, not cross-context query)
- [ ] Are factory methods needed for complex creation rules?

---

## Anti-Patterns to Avoid

| Anti-Pattern | Problem | Fix |
|---|---|---|
| Anemic domain model | Aggregates are just data bags with no behaviour | Move logic from services into aggregates |
| Public setters on aggregates | Anyone can mutate state, bypassing invariants | Private setters, enforce change through methods |
| Repositories returning entities | Breaks aggregate boundary | Return only aggregate roots |
| Domain importing infrastructure | Couples business logic to frameworks | Use interfaces, inject from Infrastructure |
| Sharing DB across bounded contexts | Tight coupling, breaks autonomy | One DB per context, communicate via events |
| Giant aggregates | Performance issues, merge conflicts, complexity | Keep aggregates small, split if >4 entities |
| Business logic in application layer | Logic scattered, not reusable | Push all rules into domain methods |
| Raw primitives in domain (`string`, `decimal`, `Guid`) | No validation, no meaning | Wrap in value objects |

---

## Quick Reference

```
Concept          → Use when
─────────────────────────────────────────────────────
Aggregate Root   → Single entry point for state changes
Entity           → Has identity, changes over time, lives inside aggregate
Value Object     → Defined by values, immutable, replace primitives
Domain Event     → Something meaningful happened (past tense)
Domain Service   → Stateless logic spanning multiple aggregates
Repository       → Interface in Application, implementation in Infrastructure
Application Svc  → Orchestrates use cases, no business logic
Integration Evt  → Cross-service communication via message bus
Local Projection → Minimal copy of foreign context data, kept fresh by events
```
