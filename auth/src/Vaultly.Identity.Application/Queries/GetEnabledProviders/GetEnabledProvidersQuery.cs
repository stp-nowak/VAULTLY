using Vaultly.Identity.Application.Messaging;
using Vaultly.Identity.Application.Models;

namespace Vaultly.Identity.Application.Queries.GetEnabledProviders;

/// <summary>
/// Query to get enabled OAuth providers.
/// </summary>
public sealed record GetEnabledProvidersQuery : IQuery<IReadOnlyList<ProviderInfo>>;
