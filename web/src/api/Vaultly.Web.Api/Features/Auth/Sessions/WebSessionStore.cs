using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;

namespace Vaultly.Web.Api.Features.Auth.Sessions;

/// <summary>
/// Persistence of <see cref="WebSession"/> keyed by the session cookie value.
/// </summary>
public interface IWebSessionStore
{
    /// <summary>Loads a session, or <c>null</c> when it does not exist, expired or cannot be decrypted.</summary>
    Task<WebSession?> GetAsync(string sessionId, CancellationToken cancellationToken);

    /// <summary>Creates or replaces a session; it expires together with its refresh token.</summary>
    Task SetAsync(string sessionId, WebSession session, CancellationToken cancellationToken);

    /// <summary>Deletes a session.</summary>
    Task RemoveAsync(string sessionId, CancellationToken cancellationToken);
}

/// <summary>
/// <see cref="IWebSessionStore"/> on top of <see cref="IDistributedCache"/> (Redis in every non-local environment).
/// </summary>
/// <remarks>
/// Defence in depth for a Redis leak: the key is a SHA-256 hash of the session id (a dump does not reveal usable
/// cookies) and the payload is encrypted with Data Protection (tokens are not readable at rest).
/// </remarks>
public sealed class DistributedWebSessionStore(
    IDistributedCache cache,
    IDataProtectionProvider dataProtectionProvider,
    ILogger<DistributedWebSessionStore> logger) : IWebSessionStore
{
    private const string KeyPrefix = "session:";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache = cache;
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("Vaultly.Web.Api.Session.v1");
    private readonly ILogger<DistributedWebSessionStore> _logger = logger;

    /// <inheritdoc />
    public async Task<WebSession?> GetAsync(string sessionId, CancellationToken cancellationToken)
    {
        var key = BuildKey(sessionId);
        var payload = await _cache.GetAsync(key, cancellationToken);
        if (payload is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<WebSession>(_protector.Unprotect(payload), JsonOptions);
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException)
        {
            // Key ring rotated/lost or payload corrupted: the session is unusable, force a new login.
            _logger.LogWarning(exception, "Discarding unreadable web session payload");
            await _cache.RemoveAsync(key, cancellationToken);
            return null;
        }
    }

    /// <inheritdoc />
    public Task SetAsync(string sessionId, WebSession session, CancellationToken cancellationToken)
    {
        var payload = _protector.Protect(JsonSerializer.SerializeToUtf8Bytes(session, JsonOptions));
        var options = new DistributedCacheEntryOptions { AbsoluteExpiration = session.RefreshTokenExpiresAt };
        return _cache.SetAsync(BuildKey(sessionId), payload, options, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string sessionId, CancellationToken cancellationToken)
        => _cache.RemoveAsync(BuildKey(sessionId), cancellationToken);

    /// <summary>
    /// Derives the storage key from the session id so the raw id never reaches Redis.
    /// </summary>
    internal static string BuildKey(string sessionId)
        => KeyPrefix + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sessionId)));
}
