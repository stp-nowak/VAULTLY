using Serilog;

using Vaultly.Web.Api.Common.Csrf;
using Vaultly.Web.Api.Common.Hosting;
using Vaultly.Web.Api.Features.Auth;
using Vaultly.Web.Api.Features.Web;

// Backend-for-frontend of the Vaultly web app: the browser holds only an HttpOnly session cookie, this API keeps
// the Identity tokens in Redis and calls the gateway on the user's behalf.
var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddPlatform(builder.Configuration, builder.Environment);
builder.Services.AddAuthFeature(builder.Configuration);
builder.Services.AddWebFeature(builder.Configuration);

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseRouting();
app.UseMiddleware<CsrfHeaderMiddleware>();

app.MapAuthFeature();
app.MapWebFeature();

app.Run();

/// <summary>
/// Entry point; public partial so integration tests can host the app with <c>WebApplicationFactory</c>.
/// </summary>
public partial class Program;
