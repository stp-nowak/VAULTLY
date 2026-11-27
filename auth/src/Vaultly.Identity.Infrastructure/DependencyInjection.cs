using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Vaultly.Core;
using Vaultly.Core.Persistence;
using Vaultly.Identity.Application.Features.Authorization.GetAuthorizationRequest;
using Vaultly.Identity.Application.Features.ExternalLogin;
using Vaultly.Identity.Application.Features.Sessions.GetSessions;
using Vaultly.Identity.Application.Features.SignInMethods;
using Vaultly.Identity.Application.Features.Users;
using Vaultly.Identity.Application.Security;
using Vaultly.Identity.Domain.AuthorizationRequests;
using Vaultly.Identity.Domain.OAuthStates;
using Vaultly.Identity.Domain.Sessions;
using Vaultly.Identity.Domain.SignInMethods;
using Vaultly.Identity.Domain.Users;
using Vaultly.Identity.Infrastructure.AuthorizationRequests;
using Vaultly.Identity.Infrastructure.ExternalLogin;
using Vaultly.Identity.Infrastructure.OAuthStates;
using Vaultly.Identity.Infrastructure.Persistence;
using Vaultly.Identity.Infrastructure.Security;
using Vaultly.Identity.Infrastructure.Sessions;
using Vaultly.Identity.Infrastructure.SignInMethods;
using Vaultly.Identity.Infrastructure.Users;

namespace Vaultly.Identity.Infrastructure;

/// <summary>
/// Dependency injection helpers for the infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers infrastructure services, repositories, and persistence.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient();

        services.AddDbContext<IdentityDbContext>(options =>
            options.UsePostgres(configuration.GetConnectionString("IdentityDb")));

        services.AddScoped<IOAuthStateRepository, OAuthStateRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IAuthorizationRequestRepository, AuthorizationRequestRepository>();
        services.AddScoped<ISignInSettingsRepository, SignInSettingsRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork<IdentityDbContext>>();

        // Read side (CQRS): query handlers depend on read services, never on repositories.
        services.AddScoped<ISessionReadService, SessionReadService>();
        services.AddScoped<IAuthorizationRequestReadService, AuthorizationRequestReadService>();
        services.AddScoped<IUserReadService, UserReadService>();
        services.AddScoped<ISignInSettingsReadService, SignInSettingsReadService>();

        services.AddScoped<IExternalOAuthProvider, GoogleOAuthClient>();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ITokenGenerator, SecureTokenGenerator>();
        services.AddSingleton<ITokenHasher, Sha256TokenHasher>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IJwtKeyProvider, JwtKeyProvider>();

        return services;
    }
}