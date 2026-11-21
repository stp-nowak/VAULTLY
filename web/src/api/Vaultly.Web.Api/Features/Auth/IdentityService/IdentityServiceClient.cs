using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Options;

namespace Vaultly.Web.Api.Features.Auth.IdentityService;

/// <summary>
/// OIDC confidential client of the Identity service: token and revocation endpoints authenticated with the client
/// secret, session management with the user's access token.
/// </summary>
public sealed class IdentityServiceClient(HttpClient httpClient, IOptions<AuthOptions> options, TimeProvider timeProvider) : IIdentityServiceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient = httpClient;
    private readonly AuthOptions _options = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider;

    /// <inheritdoc />
    public async Task<IdentityTokenSet> ExchangeCodeAsync(string code, string codeVerifier, string redirectUri, CancellationToken cancellationToken)
    {
        using var response = await PostFormAsync("token", new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["code_verifier"] = codeVerifier,
            ["redirect_uri"] = redirectUri,
        }, cancellationToken);

        EnsureSuccess(response, "exchange authorization code");
        return await ReadTokenSetAsync(response, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IdentityTokenSet?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        using var response = await PostFormAsync("token", new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
        }, cancellationToken);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return null;
        }

        EnsureSuccess(response, "refresh session");
        return await ReadTokenSetAsync(response, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken)
    {
        using var response = await PostFormAsync("revoke", new Dictionary<string, string>
        {
            ["token"] = refreshToken,
            ["token_type_hint"] = "refresh_token",
        }, cancellationToken);

        EnsureSuccess(response, "revoke token");
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

    private Task<HttpResponseMessage> PostFormAsync(string path, Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        form["client_id"] = _options.ClientId;
        form["client_secret"] = _options.ClientSecret;
        return _httpClient.PostAsync(path, new FormUrlEncodedContent(form), cancellationToken);
    }

    private async Task<IdentityTokenSet> ReadTokenSetAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions, cancellationToken)
                   ?? throw new IdentityServiceException((int)response.StatusCode, "Identity service returned an empty token response.");

        var now = _timeProvider.GetUtcNow();
        return new IdentityTokenSet(
            body.AccessToken,
            now.AddSeconds(body.ExpiresIn),
            body.RefreshToken,
            now.AddSeconds(body.RefreshExpiresIn),
            body.SessionId,
            body.User);
    }

    private static void EnsureSuccess(HttpResponseMessage response, string operation)
    {
        // The body is not logged or surfaced: it may echo request data.
        if (!response.IsSuccessStatusCode)
        {
            throw new IdentityServiceException(
                (int)response.StatusCode,
                $"Identity service failed to {operation} (HTTP {(int)response.StatusCode}).");
        }
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("refresh_token")] string RefreshToken,
        [property: JsonPropertyName("refresh_expires_in")] int RefreshExpiresIn,
        [property: JsonPropertyName("session_id")] Guid SessionId,
        [property: JsonPropertyName("user")] AuthenticatedUser User);

    private sealed record SessionsResponse(IReadOnlyList<IdentitySession> Sessions);
}
