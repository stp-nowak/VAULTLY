namespace Vaultly.Identity.Api.Contracts;

/// <summary>OAuth 2.0 grants the token endpoint supports (<c>grant_type=authorization_code</c>, …).</summary>
public enum GrantType
{
    AuthorizationCode,
    RefreshToken,
}
