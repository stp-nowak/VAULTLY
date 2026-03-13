using System.Net.Http.Json;

using Google.Apis.Auth;

using Microsoft.Extensions.Options;

using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Domain.Constants;
using Vaultly.Identity.Domain.ValueObjects;
using Vaultly.Identity.Infrastructure.Options;

namespace Vaultly.Identity.Infrastructure.ExternalServices;

// IHttpClientFactory (System.Net.Http) is used so that HttpClient lifetime is managed by the DI container.
public sealed class GoogleOAuthClient(IHttpClientFactory httpClientFactory, IOptions<GoogleOAuthOptions> options)
    : IExternalOAuthProvider
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(nameof(GoogleOAuthClient));
    private readonly GoogleOAuthOptions _options = options.Value;

    public ProviderId Id => ProviderIds.Google;
    public string Name => "Google";
    public bool IsEnabled => _options.Enabled;
    public string AuthorizationEndpoint => _options.AuthorizationEndpoint;
    public string ClientId => _options.ClientId;
    public string RedirectUri => _options.RedirectUri;
    public IReadOnlyList<string> Scopes => _options.Scopes;

    /// <summary>
    /// Exchanges an authorization code for external identity information.
    /// </summary>
    public async Task<ExternalIdentityInfo> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken cancellationToken)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["redirect_uri"] = _options.RedirectUri,
            ["code_verifier"] = codeVerifier
        };

        using var response = await _httpClient.PostAsync(_options.TokenEndpoint, new FormUrlEncodedContent(form), cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken: cancellationToken);
        if (tokenResponse is null || string.IsNullOrWhiteSpace(tokenResponse.IdToken))
        {
            throw new InvalidOperationException("Google token response is invalid.");
        }

        var payload = await GoogleJsonWebSignature.ValidateAsync(tokenResponse.IdToken, new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = [_options.ClientId]
        });

        return new ExternalIdentityInfo(
            payload.Subject,
            payload.Email,
            payload.EmailVerified,
            payload.Name ?? payload.Email,
            payload.Nonce);
    }

    private sealed record GoogleTokenResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string AccessToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("expires_in")] int ExpiresIn,
        [property: System.Text.Json.Serialization.JsonPropertyName("token_type")] string TokenType,
        [property: System.Text.Json.Serialization.JsonPropertyName("scope")] string Scope,
        [property: System.Text.Json.Serialization.JsonPropertyName("id_token")] string IdToken);
}
