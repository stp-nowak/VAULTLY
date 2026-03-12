using MediatR;

using Microsoft.Extensions.DependencyInjection;

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


        return services;
    }
}