namespace Vaultly.SharedKernel;

public abstract class AggregateRoot
{
    private readonly List<DomainEvent> _domainEvents = new();

    /// <summary>
    /// Gets the domain events raised by the aggregate.
    /// </summary>
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Registers a domain event to be dispatched after persistence.
    /// </summary>
    protected void RaiseDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all pending domain events after they have been dispatched.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}