namespace Vaultly.Web.Api.Features.Auth.Sessions;

/// <summary>
/// Serialises token refreshes per session within one API instance.
/// </summary>
/// <remarks>
/// Uses a fixed set of striped semaphores (hash of the session id → stripe) so memory stays bounded regardless of
/// the number of sessions. With several API replicas, sticky sessions or a Redis lock are needed to avoid
/// cross-instance refresh races; until then run a single instance or enable session affinity.
/// </remarks>
public sealed class SessionRefreshLock
{
    private const int StripeCount = 64;

    private readonly SemaphoreSlim[] _stripes = Enumerable.Range(0, StripeCount).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    /// <summary>
    /// Waits for the stripe of the given session and returns a handle that releases it on dispose.
    /// </summary>
    public async Task<IDisposable> AcquireAsync(string sessionId, CancellationToken cancellationToken)
    {
        var stripe = _stripes[(int)((uint)StringComparer.Ordinal.GetHashCode(sessionId) % StripeCount)];
        await stripe.WaitAsync(cancellationToken);
        return new Releaser(stripe);
    }

    private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        private SemaphoreSlim? _semaphore = semaphore;

        public void Dispose() => Interlocked.Exchange(ref _semaphore, null)?.Release();
    }
}
