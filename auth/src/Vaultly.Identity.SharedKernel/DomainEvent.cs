using MediatR;

namespace Vaultly.Identity.SharedKernel;

public abstract record DomainEvent : INotification
{
    /// <summary>
    /// Gets the unique event identifier.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the UTC timestamp when the event occurred.
    /// </summary>
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}