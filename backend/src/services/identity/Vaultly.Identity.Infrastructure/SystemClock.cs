using Vaultly.Identity.Application.Interfaces.Services;

namespace Vaultly.Identity.Infrastructure;

public sealed class SystemClock : IClock
{
    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}