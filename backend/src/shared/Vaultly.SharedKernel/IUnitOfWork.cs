namespace Vaultly.SharedKernel;

public interface IUnitOfWork
{
    /// <summary>
    /// Persists pending changes to the data store.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}