namespace Vaultly.Identity.ApiContracts;

/// <summary>
/// Form fields (<c>application/x-www-form-urlencoded</c>) of <c>POST /token</c> and <c>POST /revoke</c>.
/// Client credentials go in <see cref="ClientId"/> and <see cref="ClientSecret"/> or in HTTP Basic authentication.
/// </summary>
public static class TokenRequestFields
{
    public const string GrantType = "grant_type";
    public const string Code = "code";
    public const string CodeVerifier = "code_verifier";
    public const string RedirectUri = "redirect_uri";
    public const string RefreshToken = "refresh_token";
    public const string ClientId = "client_id";
    public const string ClientSecret = "client_secret";

    /// <summary><c>POST /revoke</c>: the token to revoke.</summary>
    public const string Token = "token";

    /// <summary><c>POST /revoke</c>: a <see cref="ApiContracts.TokenTypeHint"/>.</summary>
    public const string TokenTypeHint = "token_type_hint";
}

/// <summary>RFC 7009 hint of what <c>POST /revoke</c> receives.</summary>
public enum TokenTypeHint
{
    RefreshToken,
}
