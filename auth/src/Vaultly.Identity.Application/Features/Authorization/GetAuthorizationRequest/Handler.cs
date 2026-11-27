using Vaultly.Core.Messaging;
using Vaultly.Identity.Application.Common;
using Vaultly.Identity.Application.Features.ExternalLogin;
using Vaultly.Identity.Application.Features.SignInMethods;
using Vaultly.Identity.Domain.AuthorizationRequests.Clients;
using Vaultly.Identity.Domain.SignInMethods;

namespace Vaultly.Identity.Application.Features.Authorization.GetAuthorizationRequest;

/// <summary>
/// Reads an authorization request for the login UI (read side: read service, no aggregates).
/// </summary>
public sealed class GetAuthorizationRequestHandler(
    IAuthorizationRequestReadService readService,
    IClientRegistry clientRegistry,
    IEnumerable<IExternalOAuthProvider> providers,
    ISignInSettingsReadService signInSettingsReadService,
    TimeProvider clock) : IQueryHandler<GetAuthorizationRequestQuery, AuthorizationRequestDto?>
{
    public const string ConsoleDisplayName = "Vaultly Admin";

    private readonly IAuthorizationRequestReadService _readService = readService;
    private readonly IClientRegistry _clientRegistry = clientRegistry;
    private readonly IReadOnlyList<IExternalOAuthProvider> _providers = providers.ToList();
    private readonly ISignInSettingsReadService _signInSettingsReadService = signInSettingsReadService;
    private readonly TimeProvider _clock = clock;

    public async Task<AuthorizationRequestDto?> Handle(GetAuthorizationRequestQuery request, CancellationToken cancellationToken)
    {
        var model = await _readService.GetAsync(request.Id, cancellationToken);
        if (model is null)
        {
            return null;
        }

        var status = model.CompletedAt.HasValue
            ? AuthorizationRequestStatus.Completed
            : model.ExpiresAt <= _clock.GetUtcNow() ? AuthorizationRequestStatus.Expired : AuthorizationRequestStatus.Pending;

        var clientName = ResolveClientName(model.ClientId);
        if (status != AuthorizationRequestStatus.Pending)
        {
            return new AuthorizationRequestDto(model.Id, clientName, status, [], PasswordSignIn: false);
        }

        var disabled = await _signInSettingsReadService.GetDisabledMethodsAsync(cancellationToken);
        return new AuthorizationRequestDto(
            model.Id,
            clientName,
            status,
            UsableProviders(model.Id, disabled),
            PasswordSignIn: !disabled.Contains(SignInMethod.Password.Value));
    }

    private List<LoginProviderDto> UsableProviders(Guid requestId, IReadOnlySet<string> disabled)
        => _providers
            .Where(x => x.IsConfigured && !disabled.Contains(x.Id.Value))
            .Select(x => new LoginProviderDto(
                x.Id.Value,
                x.Name,
                OAuthFlowUtilities.BuildRelativeUrl($"/{x.Id.Value}/authorize", "request", requestId.ToString())))
            .ToList();

    private string ResolveClientName(string clientId)
        => clientId == ConsoleClient.Id.Value
            ? ConsoleDisplayName
            : _clientRegistry.Find(clientId)?.DisplayName ?? clientId;
}
