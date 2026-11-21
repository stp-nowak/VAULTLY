using Vaultly.Identity.Application.Common;
using Vaultly.Identity.Application.Interfaces.ReadServices;
using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Application.Messaging;
using Vaultly.Identity.Application.Models;

namespace Vaultly.Identity.Application.Queries.GetAuthorizationRequest;

/// <summary>
/// Reads an authorization request for the login UI (read side: read service, no aggregates).
/// </summary>
public sealed class GetAuthorizationRequestHandler(
    IAuthorizationRequestReadService readService,
    IClientRegistry clientRegistry,
    IEnumerable<IExternalOAuthProvider> providers,
    IClock clock) : IQueryHandler<GetAuthorizationRequestQuery, AuthorizationRequestDto?>
{
    private readonly IAuthorizationRequestReadService _readService = readService;
    private readonly IClientRegistry _clientRegistry = clientRegistry;
    private readonly IReadOnlyList<IExternalOAuthProvider> _providers = providers.ToList();
    private readonly IClock _clock = clock;

    public async Task<AuthorizationRequestDto?> Handle(GetAuthorizationRequestQuery request, CancellationToken cancellationToken)
    {
        var model = await _readService.GetAsync(request.Id, cancellationToken);
        if (model is null)
        {
            return null;
        }

        var status = model.CompletedAt.HasValue
            ? "completed"
            : model.ExpiresAt <= _clock.UtcNow ? "expired" : "pending";

        var providers = status == "pending"
            ? _providers
                .Where(x => x.IsEnabled)
                .Select(x => new LoginProviderDto(
                    x.Id.Value,
                    x.Name,
                    OAuthFlowUtilities.BuildRelativeUrl($"/{x.Id.Value}/authorize", "request", model.Id.ToString())))
                .ToList()
            : [];

        var clientName = _clientRegistry.Find(model.ClientId)?.DisplayName ?? model.ClientId;
        return new AuthorizationRequestDto(model.Id, clientName, status, providers);
    }
}
