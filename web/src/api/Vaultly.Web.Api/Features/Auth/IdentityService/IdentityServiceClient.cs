using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Vaultly.Web.Api.Features.Auth.IdentityService;

/// <summary>
/// HTTP implementation of <see cref="IIdentityServiceClient"/>.
/// </summary>
/// <remarks>
/// The Identity service transports the refresh token only in its HttpOnly <c>vaultly_refresh</c> cookie (it was
/// designed for browser clients). The web API therefore reads it from <c>Set-Cookie</c> and sends it back in a
/// <c>Cookie</c> header. The primary handler must have <c>UseCookies = false</c> so these headers are passed through
/// untouched and no cookie state is shared between users.
/// </remarks>
public sealed class IdentityServiceClient(HttpClient httpClient, TimeProvider timeProvider) : IIdentityServiceClient
{
    /// <summary>Cookie used by the Identity service to carry the refresh token.</summary>
    public const string RefreshCookieName = "vaultly_refresh";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient = httpClient;
    private readonly TimeProvider _timeProvider = timeProvider;

    /// <inheritdoc />
    public async Task<IdentityTokenSet> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync("token", new { code }, JsonOptions, cancellationToken);
        EnsureSuccess(response, "exchange authorization code");
        return await ReadTokenSetAsync(response, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IdentityTokenSet?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "refresh");
        AddRefreshCookie(request, refreshToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        EnsureSuccess(response, "refresh session");
        return await ReadTokenSetAsync(response, cancellationToken);
    }

    /// <inheritdoc />
    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "logout");
        AddRefreshCookie(request, refreshToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        EnsureSuccess(response, "logout");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IdentitySession>> GetSessionsAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "sessions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        EnsureSuccess(response, "list sessions");

        var body = await response.Content.ReadFromJsonAsync<SessionsResponse>(JsonOptions, cancellationToken);
        return body?.Sessions ?? [];
    }

    /// <inheritdoc />
    public async Task<bool> RevokeSessionAsync(string accessToken, Guid sessionId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"sessions/{sessionId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        EnsureSuccess(response, "revoke session");
        return true;
    }

    /// <summary>
    /// Builds the token set from the JSON body (access token) and the <c>Set-Cookie</c> header (refresh token).
    /// </summary>
    private async Task<IdentityTokenSet> ReadTokenSetAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions, cancellationToken)
                   ?? throw new IdentityServiceException((int)response.StatusCode, "Identity service returned an empty token response.");

        var now = _timeProvider.GetUtcNow();
        var (refreshToken, refreshTokenExpiresAt) = ReadRefreshCookie(response, now);

        return new IdentityTokenSet(
            body.AccessToken,
            now.AddSeconds(body.ExpiresIn),
            refreshToken,
            refreshTokenExpiresAt,
            body.SessionId,
            body.User);
    }

    /// <summary>
    /// Extracts the refresh token cookie. ASP.NET Core URL-encodes cookie values, hence the unescape.
    /// </summary>
    private static (string Value, DateTimeOffset ExpiresAt) ReadRefreshCookie(HttpResponseMessage response, DateTimeOffset now)
    {
        if (response.Headers.TryGetValues(HeaderNames.SetCookie, out var setCookieHeaders)
            && SetCookieHeaderValue.TryParseList(setCookieHeaders.ToList(), out var cookies))
        {
            var cookie = cookies.FirstOrDefault(x => x.Name.Equals(RefreshCookieName, StringComparison.Ordinal));
            if (cookie is not null && !StringSegment.IsNullOrEmpty(cookie.Value))
            {
                var expiresAt = cookie.Expires ?? (cookie.MaxAge is { } maxAge ? now.Add(maxAge) : now.AddDays(1));
                return (Uri.UnescapeDataString(cookie.Value.Value!), expiresAt);
            }
        }

        throw new IdentityServiceException((int)response.StatusCode, "Identity service did not return a refresh token.");
    }

    /// <summary>
    /// Sends the refresh token the same way a browser would, so the Identity service contract stays unchanged.
    /// </summary>
    private static void AddRefreshCookie(HttpRequestMessage request, string refreshToken)
        => request.Headers.Add(HeaderNames.Cookie, $"{RefreshCookieName}={Uri.EscapeDataString(refreshToken)}");

    /// <summary>
    /// Throws <see cref="IdentityServiceException"/> for non-success responses. The body is not logged because
    /// it may echo sensitive request data.
    /// </summary>
    private static void EnsureSuccess(HttpResponseMessage response, string operation)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new IdentityServiceException(
                (int)response.StatusCode,
                $"Identity service failed to {operation} (HTTP {(int)response.StatusCode}).");
        }
    }

    private sealed record TokenResponse(string AccessToken, int ExpiresIn, Guid SessionId, AuthenticatedUser User);

    private sealed record SessionsResponse(IReadOnlyList<IdentitySession> Sessions);
}
