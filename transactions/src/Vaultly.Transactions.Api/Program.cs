using Serilog;

using Vaultly.Core.Authentication;
using Vaultly.Core.Persistence;
using Vaultly.Identity.PublicTypes;
using Vaultly.Transactions.Api.Endpoints;
using Vaultly.Transactions.Api.Http;
using Vaultly.Transactions.Application;
using Vaultly.Transactions.Infrastructure;
using Vaultly.Transactions.Infrastructure.Persistence;

// Vaultly Transactions: the transactions users enter on their manual accounts. Reached only through the gateway,
// which sends an internal token issued for this service (audience "transactions").
var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TransactionsProblems>();
builder.Services.ConfigureHttpJsonOptions(options => ApiJson.Configure(options.SerializerOptions));
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddInternalTokenAuthentication(builder.Configuration);
builder.Services.AddHealthChecks().AddDbContextCheck<TransactionsDbContext>(tags: [TransactionsHealth.Ready]);

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await app.Services.MigrateDatabaseAsync<TransactionsDbContext>();
}

// Request logging wraps the exception handler, so it logs the status the client gets (404, 409) and not a 500.
app.UseSerilogRequestLogging(options => options.EnrichDiagnosticContext = (diagnostics, context) =>
    diagnostics.Set("UserId", context.User.FindFirst(IdentityClaims.UserId)?.Value));
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapTransactionsHealth();
app.MapTransactionEndpoints();

app.Run();

/// <summary>
/// Entry point; public partial so integration tests can host the API with <c>WebApplicationFactory</c>.
/// </summary>
public partial class Program;
