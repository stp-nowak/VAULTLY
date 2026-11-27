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
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
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
    }
}
