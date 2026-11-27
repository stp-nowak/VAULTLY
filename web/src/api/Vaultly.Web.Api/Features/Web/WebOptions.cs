using System.ComponentModel.DataAnnotations;

namespace Vaultly.Web.Api.Features.Web;

/// <summary>
/// Configuration of the Web feature (section <c>Web</c>).
/// </summary>
public sealed class WebOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Web";

    /// <summary>Base URL of the API gateway (./gateway); <c>/api/x</c> is forwarded to <c>{GatewayBaseUrl}/x</c>.</summary>
    [Required, Url]
    public string GatewayBaseUrl { get; init; } = string.Empty;
}
