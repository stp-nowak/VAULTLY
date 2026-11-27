using MediatR;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using Serilog;

using Vaultly.Core.Persistence;
using Vaultly.Identity.Api.Configuration;
using Vaultly.Identity.Api.Endpoints;
using Vaultly.Identity.Api.Http;
using Vaultly.Identity.Application;
using Vaultly.Identity.Application.Features.Users.EnsureBootstrapAdmin;
using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Infrastructure;
using Vaultly.Identity.Infrastructure.ExternalLogin;
using Vaultly.Identity.Infrastructure.Persistence;
using Vaultly.Identity.Infrastructure.Security;

// Vaultly Identity: OAuth 2.0 / OIDC authorization server (authorization code + PKCE, SSO session). Client
// applications only redirect to /authorize and call /token server-to-server. Its login UI (auth/web) is hosted
// separately and reaches this API through its own web server's proxy.
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddOptionalDotEnvFile(Path.Combine(builder.Environment.ContentRootPath, ".env"));

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddSingleton(Log.Logger);
builder.Services.AddSingleton(new EndpointErrorHandler(Log.ForContext<EndpointErrorHandler>()));

builder.Services.AddOpenApi();

builder.Services.Configure<IdentityOptions>(builder.Configuration.GetSection("Identity"));
builder.Services.Configure<GoogleOAuthOptions>(builder.Configuration.GetSection("Google"));

var identityOptions = builder.Configuration.GetSection("Identity").Get<IdentityOptions>()
                      ?? throw new InvalidOperationException("Identity configuration is missing.");

var resolvedJwtPrivateKey = JwtPrivateKeyResolver.Resolve(
    identityOptions,
    builder.Environment.ContentRootPath,
    builder.Environment.IsDevelopment());
Log.Information("Loaded JWT signing key from {JwtSigningKeySource}.", resolvedJwtPrivateKey.SourceDescription);

var rsaKeyProvider = new RsaKeyProvider(resolvedJwtPrivateKey.Pem);
builder.Services.AddSingleton(rsaKeyProvider);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// No CORS: browsers only navigate to this service (or reach it same-origin via the login UI); clients call it
// server-to-server.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = identityOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = identityOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = rsaKeyProvider.SecurityKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddPasswordRateLimiting();
builder.Services.ConfigureHttpJsonOptions(options => ApiJson.Configure(options.SerializerOptions));

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await app.Services.MigrateDatabaseAsync<IdentityDbContext>();
}

if (!string.IsNullOrWhiteSpace(identityOptions.BootstrapAdmin.Email))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<ISender>().Send(new EnsureBootstrapAdminCommand());
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapIdentityEndpoints();

app.Run();

/// <summary>
/// Entry point; public partial so integration tests can host the API with <c>WebApplicationFactory</c>.
/// </summary>
public partial class Program;
