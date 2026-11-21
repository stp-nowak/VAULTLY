using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Vaultly.Identity.Api;

/// <summary>
/// Configuration of the login UI hosting (section <c>LoginUi</c>).
/// </summary>
public sealed class LoginUiOptions
{
    public const string SectionName = "LoginUi";

    /// <summary>
    /// Serve the built auth/web SPA from this service. Off in development, where Vite serves it on its own port
    /// (see <c>Identity:LoginUiBaseUrl</c>) and proxies API paths here.
    /// </summary>
    public bool ServeSpa { get; init; }

    /// <summary>Directory with the built SPA (auth/web <c>dist</c>), relative to the content root.</summary>
    public string SpaRootPath { get; init; } = "wwwroot";
}

/// <summary>
/// Hosts the login UI (auth/web) on the same origin as the authorization server, like Keycloak's themes.
/// </summary>
public static class LoginUiHosting
{
    private static readonly string[] SpaRoutes = ["/login", "/error", "/logged-out"];

    /// <summary>
    /// Serves the SPA's static assets. Runs before authentication so assets short-circuit the pipeline.
    /// </summary>
    public static WebApplication UseLoginUiStaticFiles(this WebApplication app)
    {
        if (TryGetFileProvider(app, out var fileProvider))
        {
            app.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });
        }

        return app;
    }

    /// <summary>
    /// Maps the SPA routes to <c>index.html</c>. Only known UI routes are mapped so unknown API paths still 404.
    /// </summary>
    public static WebApplication MapLoginUi(this WebApplication app)
    {
        if (TryGetFileProvider(app, out var fileProvider))
        {
            foreach (var route in SpaRoutes)
            {
                app.MapFallbackToFile(route, "index.html", new StaticFileOptions { FileProvider = fileProvider });
            }
        }

        return app;
    }

    private static bool TryGetFileProvider(WebApplication app, out PhysicalFileProvider fileProvider)
    {
        var options = app.Services.GetRequiredService<IOptions<LoginUiOptions>>().Value;
        fileProvider = null!;
        if (!options.ServeSpa)
        {
            return false;
        }

        var root = Path.GetFullPath(options.SpaRootPath, app.Environment.ContentRootPath);
        if (!Directory.Exists(root))
        {
            throw new InvalidOperationException($"LoginUi:SpaRootPath '{root}' does not exist. Build auth/web or set LoginUi:ServeSpa=false.");
        }

        fileProvider = new PhysicalFileProvider(root);
        return true;
    }
}
