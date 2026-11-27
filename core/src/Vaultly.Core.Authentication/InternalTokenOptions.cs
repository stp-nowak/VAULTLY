using System.ComponentModel.DataAnnotations;

namespace Vaultly.Core.Authentication;

/// <summary>
/// Which gateway-issued tokens a service accepts (section <c>InternalTokens</c>).
/// </summary>
public sealed class InternalTokenOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "InternalTokens";

    /// <summary>The gateway's issuer (its <c>InternalTokens:Issuer</c>).</summary>
    [Required]
    public string Issuer { get; init; } = string.Empty;

    /// <summary>This service's name: the cluster id its gateway routes use.</summary>
    [Required]
    public string Audience { get; init; } = string.Empty;

    /// <summary>The gateway's <c>/.well-known/jwks.json</c>.</summary>
    [Required, Url]
    public string JwksUrl { get; init; } = string.Empty;
}
