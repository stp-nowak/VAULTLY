using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Vaultly.Core.Persistence;

/// <summary>
/// Saves a service's <typeparamref name="TDbContext"/> and then publishes the domain events its tracked aggregates
/// raised, so handlers of those events only ever see committed state.
/// </summary>
public class UnitOfWork<TDbContext>(TDbContext dbContext, IPublisher publisher) : IUnitOfWork
    where TDbContext : DbContext
{
    protected TDbContext DbContext => dbContext;

    public virtual Task SaveChangesAsync(CancellationToken cancellationToken)
        => SaveAndPublishDomainEventsAsync(cancellationToken);

    /// <summary>Saves, publishes the raised domain events, then saves what their handlers changed.</summary>
    protected async Task SaveAndPublishDomainEventsAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);

        var aggregates = dbContext.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        foreach (var aggregate in aggregates)
        {
            var domainEvents = aggregate.DomainEvents.ToList();
            aggregate.ClearDomainEvents();

            foreach (var domainEvent in domainEvents)
            {
                await publisher.Publish(domainEvent, cancellationToken);
            }
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
