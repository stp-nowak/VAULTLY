using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Npgsql;

using Rebus.Config.Outbox;
using Rebus.Pipeline;
using Rebus.Transport;

using Vaultly.Core.Persistence;

namespace Vaultly.Core.Messaging.Rebus;

/// <summary>
/// A <see cref="UnitOfWork{TDbContext}"/> whose messages leave only if its database transaction commits. Outside a
/// message handler they are written to the Rebus outbox in that transaction and forwarded after the commit. Inside a
/// handler Rebus sends them once the handler finishes, before the incoming message is acknowledged.
/// </summary>
public sealed class OutboxUnitOfWork<TDbContext>(TDbContext dbContext, IPublisher publisher)
    : UnitOfWork<TDbContext>(dbContext, publisher)
    where TDbContext : DbContext
{
    public override async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (!DbContext.Database.IsRelational())
        {
            await SaveAndPublishDomainEventsAsync(cancellationToken);
            return;
        }

        await using var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);
        using var scope = MessageContext.Current is null ? new RebusTransactionScope() : null;
        scope?.UseOutbox(
            (NpgsqlConnection)DbContext.Database.GetDbConnection(),
            (NpgsqlTransaction)transaction.GetDbTransaction());

        await SaveAndPublishDomainEventsAsync(cancellationToken);

        if (scope is not null)
        {
            await scope.CompleteAsync();
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
