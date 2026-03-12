using MediatR;

using Vaultly.SharedKernel;

namespace Vaultly.Identity.Infrastructure.Persistence;

/// <summary>
/// EF Core unit of work implementation.
/// </summary>
public sealed class EfUnitOfWork(IdentityDbContext dbContext, IMediator mediator) : IUnitOfWork
{
    private readonly IdentityDbContext _dbContext = dbContext;
    private readonly IMediator _mediator = mediator;

    /// <summary>
    /// Persists pending changes and dispatches domain events after save.
    /// </summary>
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        var aggregates = _dbContext.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.DomainEvents.Count > 0)
            .ToList();

        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                await _mediator.Publish(domainEvent, cancellationToken);
            }

            aggregate.ClearDomainEvents();
        }
    }
}