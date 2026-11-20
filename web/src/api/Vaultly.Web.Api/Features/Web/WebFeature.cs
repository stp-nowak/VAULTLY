using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

using Vaultly.Web.Api.Features.Web.ApiProxy;

namespace Vaultly.Web.Api.Features.Web;

/// <summary>
/// Composition of the Web feature: everything the SPA needs besides authentication — forwarding API calls to the
/// backend with the session's token and (outside development) hosting the built SPA itself.
/// Depends on the Auth feature only through <see cref="Auth.Sessions.IAccessTokenProvider"/>.
/// </summary>
public static class WebFeature
{
    /// <summary>
    /// Registers options and the YARP forwarder.
    /// </summary>
    public static IServiceCollection AddWebFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<WebOptions>()
            .Bind(configuration.GetSection(WebOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpForwarder();
        services.AddSingleton(_ => BackendHttpClient.Create());

        return services;
    }

    /// <summary>
    /// Serves the built SPA's static files. Must run before routing so assets short-circuit the pipeline.
    /// </summary>
    public static WebApplication UseWebFeatureStaticFiles(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<WebOptions>>().Value;
        if (options.ServeSpa)
        {
            app.UseStaticFiles(new StaticFileOptions { FileProvider = CreateSpaFileProvider(app, options) });
        }

        return app;
    }

    /// <summary>
    /// Maps the API proxy and, when hosting the SPA, the client-side routing fallback.
    /// </summary>
    public static WebApplication MapWebFeature(this WebApplication app)
    {
        app.MapApiProxy();

        var options = app.Services.GetRequiredService<IOptions<WebOptions>>().Value;
        if (options.ServeSpa)
        {
            // Any non-file path that is not an API route belongs to the SPA router.
            app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = CreateSpaFileProvider(app, options) });
        }

        return app;
    }

    /// <summary>
    /// Resolves the SPA directory and fails fast with a clear message when the build output is missing.
    /// </summary>
    private static PhysicalFileProvider CreateSpaFileProvider(WebApplication app, WebOptions options)
    {
        var root = Path.GetFullPath(options.SpaRootPath, app.Environment.ContentRootPath);
        if (!Directory.Exists(root))
        {
            throw new InvalidOperationException($"Web:SpaRootPath '{root}' does not exist. Build the SPA (web/src/web) or set Web:ServeSpa=false.");
        }

        return new PhysicalFileProvider(root);
    }
}
