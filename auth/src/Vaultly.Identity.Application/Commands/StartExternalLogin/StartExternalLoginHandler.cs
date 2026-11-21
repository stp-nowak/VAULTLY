using Microsoft.Extensions.Options;

using Serilog;

using Vaultly.Identity.Application.Common;
using Vaultly.Identity.Application.Interfaces.Repositories;
using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Application.Messaging;
using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.Exceptions;
using Vaultly.Identity.Domain.ValueObjects;
using Vaultly.Identity.SharedKernel;

namespace Vaultly.Identity.Application.Commands.StartExternalLogin;

/// <summary>
/// Handles starting an external OAuth login by creating the OAuth state aggregate and building the authorize URL.
/// </summary>
public sealed class StartExternalLoginHandler(
    IOAuthStateRepository oauthStateRepository,
    IAuthorizationRequestRepository authorizationRequestRepository,
    IUnitOfWork unitOfWork,
    IEnumerable<IExternalOAuthProvider> providers,
    ITokenGenerator tokenGenerator,
    IClock clock,
    IOptions<IdentityOptions> identityOptions,
    ILogger logger) : ICommandHandler<StartExternalLoginCommand, string>
{
    private readonly IOAuthStateRepository _oauthStateRepository = oauthStateRepository;
    private readonly IAuthorizationRequestRepository _authorizationRequestRepository = authorizationRequestRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IReadOnlyDictionary<ProviderId, IExternalOAuthProvider> _providers = providers.ToDictionary(x => x.Id);
    private readonly ITokenGenerator _tokenGenerator = tokenGenerator;
    private readonly IClock _clock = clock;
    private readonly IdentityOptions _identityOptions = identityOptions.Value;
    private readonly ILogger _logger = logger.ForContext<StartExternalLoginHandler>();

    public async Task<string> Handle(StartExternalLoginCommand request, CancellationToken cancellationToken)
    {
        var provider = OAuthFlowUtilities.GetProvider(_providers, new ProviderId(request.ProviderId));
        if (!provider.IsEnabled)
        {
            _logger.Warning("OAuth provider {ProviderId} is disabled", provider.Id.Value);
            throw new InvalidOperationException("OAuth provider is disabled.");
        }

        var now = _clock.UtcNow;

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
