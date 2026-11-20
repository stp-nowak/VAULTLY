using System.ComponentModel.DataAnnotations;

namespace Vaultly.Web.Api.Features.Web;

/// <summary>
/// Configuration of the Web feature (section <c>Web</c>).
/// </summary>
public sealed class WebOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Web";

    /// <summary>Base URL of the backend API gateway; <c>/api/x</c> is forwarded to <c>{BackendApiBaseUrl}/x</c>.</summary>
    [Required, Url]
    public string BackendApiBaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// Serve the built SPA (static files + <c>index.html</c> fallback). Off in development, where Vite serves the SPA
    /// and proxies <c>/api</c> here.
    /// </summary>
    public bool ServeSpa { get; init; }

    /// <summary>Directory with the built SPA, relative to the content root.</summary>
    public string SpaRootPath { get; init; } = "wwwroot";
}
