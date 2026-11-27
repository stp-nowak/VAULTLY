namespace Vaultly.Web.Api.Features.Auth.IdentityService;

/// <summary>
/// User returned by the Identity service and exposed to the SPA.
/// </summary>
public sealed record AuthenticatedUser(Guid Id, string Email, string Name);

/// <summary>
/// Tokens issued by the Identity service for one login session. Never leaves the web API.
/// </summary>
public sealed record IdentityTokenSet(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    Guid AuthSessionId,
    AuthenticatedUser User);

/// <summary>
/// Raised when the Identity service answers with an unexpected status code.
/// </summary>
public sealed class IdentityServiceException(int statusCode, string message) : Exception(message)
{
    /// <summary>HTTP status code returned by the Identity service.</summary>
    public int StatusCode { get; } = statusCode;
}
