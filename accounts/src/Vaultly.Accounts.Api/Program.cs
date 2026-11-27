using Serilog;

using Vaultly.Accounts.Api.Endpoints;
using Vaultly.Accounts.Api.Http;
using Vaultly.Accounts.Application;
using Vaultly.Accounts.Infrastructure;
using Vaultly.Accounts.Infrastructure.Persistence;
using Vaultly.Core.Authentication;
using Vaultly.Core.Persistence;
using Vaultly.Identity.PublicTypes;

// Vaultly Accounts: the user's financial accounts, their balances and net worth. Reached only through the gateway,
// which sends an internal token issued for this service (audience "accounts").
var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AccountsProblems>();
builder.Services.ConfigureHttpJsonOptions(options => ApiJson.Configure(options.SerializerOptions));
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddInternalTokenAuthentication(builder.Configuration);
builder.Services.AddHealthChecks().AddDbContextCheck<AccountsDbContext>(tags: [AccountsHealth.Ready]);

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await app.Services.MigrateDatabaseAsync<AccountsDbContext>();
}

app.UseExceptionHandler();
app.UseSerilogRequestLogging(options => options.EnrichDiagnosticContext = (diagnostics, context) =>
    diagnostics.Set("UserId", context.User.FindFirst(IdentityClaims.UserId)?.Value));
app.UseAuthentication();
app.UseAuthorization();

app.MapAccountsHealth();
app.MapAccountsEndpoints();

app.Run();

/// <summary>
/// Entry point; public partial so integration tests can host the API with <c>WebApplicationFactory</c>.
/// </summary>
public partial class Program;
