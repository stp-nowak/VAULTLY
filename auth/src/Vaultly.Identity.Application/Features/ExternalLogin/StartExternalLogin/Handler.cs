using Microsoft.Extensions.Options;

using Serilog;

using Vaultly.Core;
using Vaultly.Core.Messaging;
using Vaultly.Identity.Application.Common;
using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Application.Security;
using Vaultly.Identity.Domain.AuthorizationRequests;
using Vaultly.Identity.Domain.AuthorizationRequests.Exceptions;
using Vaultly.Identity.Domain.OAuthStates;
using Vaultly.Identity.Domain.SignInMethods;

namespace Vaultly.Identity.Application.Features.ExternalLogin.StartExternalLogin;

/// <summary>
/// Handles starting an external OAuth login by creating the OAuth state aggregate and building the authorize URL.
/// </summary>
public sealed class StartExternalLoginHandler(
    IOAuthStateRepository oauthStateRepository,
    IAuthorizationRequestRepository authorizationRequestRepository,
    IUnitOfWork unitOfWork,
    IEnumerable<IExternalOAuthProvider> providers,
    ISignInSettingsRepository signInSettingsRepository,
    ITokenGenerator tokenGenerator,
    TimeProvider clock,
    IOptions<IdentityOptions> identityOptions,
    ILogger logger) : ICommandHandler<StartExternalLoginCommand, string>
{
    private readonly IOAuthStateRepository _oauthStateRepository = oauthStateRepository;
    private readonly IAuthorizationRequestRepository _authorizationRequestRepository = authorizationRequestRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IReadOnlyDictionary<ProviderId, IExternalOAuthProvider> _providers = providers.ToDictionary(x => x.Id);
    private readonly ISignInSettingsRepository _signInSettingsRepository = signInSettingsRepository;
    private readonly ITokenGenerator _tokenGenerator = tokenGenerator;
    private readonly TimeProvider _clock = clock;
    private readonly IdentityOptions _identityOptions = identityOptions.Value;
    private readonly ILogger _logger = logger.ForContext<StartExternalLoginHandler>();

    public async Task<string> Handle(StartExternalLoginCommand request, CancellationToken cancellationToken)
    {
        var provider = OAuthFlowUtilities.GetProvider(_providers, new ProviderId(request.ProviderId));
        await SignInMethodGuard.EnsureUsableAsync(provider, _signInSettingsRepository, cancellationToken);

        var now = _clock.GetUtcNow();

        // The login UI may be reopened from history: only a pending request can start a provider login.
        var authorizationRequest = await _authorizationRequestRepository.GetByIdAsync(
            AuthorizationRequestId.From(request.AuthorizationRequestId),
            cancellationToken) ?? throw new AuthorizationRequestNotFoundException();
        authorizationRequest.EnsurePending(now);

        var state = OAuthState.Create(
            new OAuthStateValue(_tokenGenerator.GenerateToken(32)),
            new CodeVerifier(_tokenGenerator.GenerateToken(64)),
            new Nonce(_tokenGenerator.GenerateToken(32)),
            now.AddMinutes(_identityOptions.OAuthStateTtlMinutes),
            authorizationRequest.Id);

        _oauthStateRepository.Add(state);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var authorizeUrl = OAuthFlowUtilities.BuildUrl(provider.AuthorizationEndpoint, new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = provider.ClientId,
            ["redirect_uri"] = provider.RedirectUri,
            ["scope"] = string.Join(' ', provider.Scopes),
            ["state"] = state.State.Value,
            ["code_challenge"] = OAuthFlowUtilities.CreateCodeChallenge(state.CodeVerifier.Value),
            ["code_challenge_method"] = "S256",
            ["nonce"] = state.Nonce.Value
        });

        _logger.Information(
            "Starting {ProviderId} login for authorization request {AuthorizationRequestId}",
            provider.Id.Value,
            authorizationRequest.Id.Value);
        return authorizeUrl;
    }
}
