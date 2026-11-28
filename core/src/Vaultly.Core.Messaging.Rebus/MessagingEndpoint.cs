namespace Vaultly.Core.Messaging.Rebus;

/// <summary>How one service takes part in messaging.</summary>
/// <param name="Queue">The service's own input queue, e.g. <c>accounts</c>; its failed messages go to <c>accounts.error</c>.</param>
/// <param name="DatabaseConnectionName">The connection string of the service's database, where the outbox, sagas and
/// timeouts are kept.</param>
public sealed record MessagingEndpoint(string Queue, string DatabaseConnectionName)
{
    /// <summary>Events of other services this service subscribes to.</summary>
    public IReadOnlyCollection<Type> Subscriptions { get; init; } = [];

    /// <summary>The queue of the service that owns each command this service sends.</summary>
    public IReadOnlyDictionary<Type, string> CommandOwners { get; init; } = new Dictionary<Type, string>();

    /// <summary>Whether the service runs sagas, which also need timeouts for deferred messages.</summary>
    public bool Sagas { get; init; }
}
