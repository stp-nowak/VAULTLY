using Vaultly.Identity.Application.Common;
using Vaultly.Identity.Application.Interfaces.ReadServices;
using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Application.Messaging;
using Vaultly.Identity.Application.Models;
using Vaultly.Identity.Domain.Constants;

namespace Vaultly.Identity.Application.Queries.GetAuthorizationRequest;

/// <summary>
/// Reads an authorization request for the login UI (read side: read service, no aggregates).
/// </summary>
public sealed class GetAuthorizationRequestHandler(
    IAuthorizationRequestReadService readService,
    IClientRegistry clientRegistry,
    IEnumerable<IExternalOAuthProvider> providers,
    ISignInSettingsReadService signInSettingsReadService,
    IClock clock) : IQueryHandler<GetAuthorizationRequestQuery, AuthorizationRequestDto?>
{
    public const string ConsoleDisplayName = "Vaultly Admin";

    private readonly IAuthorizationRequestReadService _readService = readService;
    private readonly IClientRegistry _clientRegistry = clientRegistry;
    private readonly IReadOnlyList<IExternalOAuthProvider> _providers = providers.ToList();
    private readonly ISignInSettingsReadService _signInSettingsReadService = signInSettingsReadService;
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

        var providers = status == "pending" ? await GetUsableProvidersAsync(model.Id, cancellationToken) : [];

        return new AuthorizationRequestDto(model.Id, ResolveClientName(model.ClientId), status, providers);
    }

    private async Task<List<LoginProviderDto>> GetUsableProvidersAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var disabled = await _signInSettingsReadService.GetDisabledMethodsAsync(cancellationToken);

        return _providers
            .Where(x => x.IsConfigured && !disabled.Contains(x.Id.Value))
            .Select(x => new LoginProviderDto(
                x.Id.Value,
                x.Name,
                OAuthFlowUtilities.BuildRelativeUrl($"/{x.Id.Value}/authorize", "request", requestId.ToString())))
            .ToList();
    }

    private string ResolveClientName(string clientId)
        => clientId == ConsoleClient.Id.Value
            ? ConsoleDisplayName
            : _clientRegistry.Find(clientId)?.DisplayName ?? clientId;
}
