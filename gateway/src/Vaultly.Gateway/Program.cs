using Serilog;

using Vaultly.Gateway.Http;
using Vaultly.Gateway.InternalTokens;
using Vaultly.Gateway.UserTokens;
using Vaultly.Identity.PublicTypes;

// The single entry to Vaultly's domain services: accepts the Identity service's access tokens, routes by path
// prefix (section ReverseProxy) and hands each service its own short-lived internal token instead.
var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddProblemDetails();
builder.Services.AddUserTokens(builder.Configuration);
builder.Services.AddInternalTokens(builder.Configuration);
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddInternalTokenTransform();

var app = builder.Build();

// Fail at startup, not on the first request, when the signing key is missing or broken.
app.Services.GetRequiredService<InternalTokenSigningKey>();

app.UseSerilogRequestLogging(options => options.EnrichDiagnosticContext = (diagnostics, context) =>
    diagnostics.Set("UserId", context.User.FindFirst(IdentityClaims.UserId)?.Value));
app.UseAuthentication();
app.UseAuthorization();

app.MapGatewayHealth();
app.MapInternalTokenKeys();
app.MapReverseProxy(proxy => proxy.Use(GatewayProblems.DescribeForwarderErrors));

app.Run();

/// <summary>
/// Entry point; public partial so integration tests can host the gateway with <c>WebApplicationFactory</c>.
/// </summary>
public partial class Program;
