using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Application.Messaging;
using Vaultly.Identity.Application.Models;

namespace Vaultly.Identity.Application.Queries.GetEnabledProviders;

/// <summary>
/// Handles retrieval of enabled OAuth providers.
/// </summary>
public sealed class GetEnabledProvidersHandler(IEnumerable<IExternalOAuthProvider> providers)
    : IQueryHandler<GetEnabledProvidersQuery, IReadOnlyList<ProviderInfo>>
{
    private readonly IReadOnlyList<IExternalOAuthProvider> _providers = providers.ToList();

    /// <summary>
    /// Returns the enabled providers for the current configuration.
    /// </summary>
    public Task<IReadOnlyList<ProviderInfo>> Handle(GetEnabledProvidersQuery request, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<ProviderInfo>>(
            _providers
                .Where(x => x.IsEnabled)
                .Select(x => new ProviderInfo(x.Id.Value, x.Name, $"/{x.Id.Value}/authorize"))
                .ToList());
}
