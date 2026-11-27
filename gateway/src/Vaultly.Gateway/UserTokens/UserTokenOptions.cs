using System.ComponentModel.DataAnnotations;

namespace Vaultly.Gateway.UserTokens;

/// <summary>
/// Which access tokens of the Identity service the gateway accepts (section <c>UserTokens</c>).
/// </summary>
public sealed class UserTokenOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "UserTokens";

    /// <summary>The Identity service's public issuer, exactly as in its tokens.</summary>
    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Where the gateway downloads Identity's signing keys. Not taken from discovery: discovery advertises the public
    /// URL, which a container may not reach.
    /// </summary>
    [Required, Url]
    public string JwksUrl { get; init; } = string.Empty;
}
