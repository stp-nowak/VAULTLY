using MediatR;

using Vaultly.SharedKernel;

namespace Vaultly.Identity.Infrastructure.Persistence;

/// <summary>
/// Unit of work implementation backed by the identity DbContext.
/// </summary>
public sealed class UnitOfWork(IdentityDbContext dbContext, IMediator mediator) : IUnitOfWork
{
    private readonly IdentityDbContext _dbContext = dbContext;
    private readonly IMediator _mediator = mediator;

    /// <summary>
    /// Persists pending changes and dispatches domain events after save.
    /// </summary>
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        var aggregates = _dbContext.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.DomainEvents.Count > 0)
            .ToList();

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
