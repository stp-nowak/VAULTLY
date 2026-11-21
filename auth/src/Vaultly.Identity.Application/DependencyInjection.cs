using Microsoft.Extensions.DependencyInjection;

using Vaultly.Identity.Application.Common;

namespace Vaultly.Identity.Application;

/// <summary>
/// Dependency injection helpers for the application layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers application services and MediatR handlers.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(config =>
            config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // Registered clients are static configuration: parse and validate them once.
        services.AddSingleton<IClientRegistry, ClientRegistry>();

        return services;
    }
}