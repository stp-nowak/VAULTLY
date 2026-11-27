using System.Text.Json.Serialization;

namespace Vaultly.Identity.ApiContracts;

/// <summary>
/// Body of a successful <c>POST /token</c>: OAuth 2.0 token response plus the session and its user.
/// </summary>
public sealed record TokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("refresh_expires_in")] int RefreshExpiresIn,
    [property: JsonPropertyName("session_id")] Guid SessionId,
    [property: JsonPropertyName("user")] UserResponse User)
{
    public const string BearerTokenType = "Bearer";
}

public sealed record UserResponse(Guid Id, string Email, string Name);
