using Microsoft.Extensions.Options;

using Serilog;

using Vaultly.Core;
using Vaultly.Core.Messaging;
using Vaultly.Identity.Application.Common;
using Vaultly.Identity.Application.Models;
using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Application.Security;
using Vaultly.Identity.Domain.AuthorizationRequests;
using Vaultly.Identity.Domain.OAuthStates;
using Vaultly.Identity.Domain.Sessions;
using Vaultly.Identity.Domain.SignInMethods;
using Vaultly.Identity.Domain.Users;

namespace Vaultly.Identity.Application.Features.ExternalLogin.HandleOAuthCallback;

/// <summary>
/// Handles OAuth callback processing and auth code issuance.
/// </summary>
public sealed class HandleOAuthCallbackHandler(
    IOAuthStateRepository oauthStateRepository,
    IAuthorizationRequestRepository authorizationRequestRepository,
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    IEnumerable<IExternalOAuthProvider> providers,
    ISignInSettingsRepository signInSettingsRepository,
    ITokenGenerator tokenGenerator,
    ITokenHasher tokenHasher,
    TimeProvider clock,
    IOptions<IdentityOptions> identityOptions,
    ILogger logger)
    : ICommandHandler<HandleOAuthCallbackCommand, AuthCallbackResult>
{
    private readonly ISignInSettingsRepository _signInSettingsRepository = signInSettingsRepository;
    private readonly IOAuthStateRepository _oauthStateRepository = oauthStateRepository;
    private readonly IAuthorizationRequestRepository _authorizationRequestRepository = authorizationRequestRepository;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ISessionRepository _sessionRepository = sessionRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IReadOnlyDictionary<ProviderId, IExternalOAuthProvider> _providers = providers.ToDictionary(x => x.Id);
    private readonly ITokenGenerator _tokenGenerator = tokenGenerator;
    private readonly ITokenHasher _tokenHasher = tokenHasher;
    private readonly TimeProvider _clock = clock;
    private readonly IdentityOptions _identityOptions = identityOptions.Value;
    private readonly ILogger _logger = logger.ForContext<HandleOAuthCallbackHandler>();

    public async Task<AuthCallbackResult> Handle(HandleOAuthCallbackCommand request, CancellationToken cancellationToken)
    {
        var provider = OAuthFlowUtilities.GetProvider(_providers, new ProviderId(request.ProviderId));
        await SignInMethodGuard.EnsureUsableAsync(provider, _signInSettingsRepository, cancellationToken);

        var now = _clock.GetUtcNow();
        var oauthState = await _oauthStateRepository.GetByStateAsync(new OAuthStateValue(request.State), cancellationToken);
        if (oauthState is null)
        {
            _logger.Warning("OAuth state was not found for provider {ProviderId}", provider.Id.Value);
            throw new InvalidOperationException("OAuth state is invalid or expired.");
        }

        try
        {
            oauthState.EnsureActive(now);
        }
        catch (OAuthStateExpiredException)
        {
            _logger.Warning("OAuth state expired for provider {ProviderId}", provider.Id.Value);
            throw;
        }

        _oauthStateRepository.Remove(oauthState);

        // The sign-in continues a client's authorization request; it must still be pending.
        var authorizationRequest = await _authorizationRequestRepository.GetByIdAsync(oauthState.AuthorizationRequestId, cancellationToken)
            ?? throw new AuthorizationRequestNotFoundException();
        authorizationRequest.EnsurePending(now);

        var identityInfo = await provider.ExchangeCodeAsync(request.Code, oauthState.CodeVerifier.Value, cancellationToken);
        if (!string.Equals(identityInfo.Nonce, oauthState.Nonce.Value, StringComparison.Ordinal))
        {
            _logger.Warning("OAuth nonce mismatch for provider {ProviderId}", provider.Id.Value);
            throw new InvalidOperationException("OAuth nonce is invalid.");
        }

        if (!identityInfo.EmailVerified)
        {
            _logger.Warning("OAuth email not verified for provider {ProviderId}", provider.Id.Value);
            throw new InvalidOperationException("OAuth account email is not verified.");
        }

        var providerUserId = new ProviderUserId(identityInfo.Subject);
        var user = await _userRepository.GetByExternalIdentityAsync(provider.Id, providerUserId, cancellationToken);

        if (user is null)
        {
            var email = new EmailAddress(identityInfo.Email);
            var name = new PersonName(identityInfo.Name);
            var existingUser = await _userRepository.GetByEmailAsync(email, cancellationToken);
            var isNewUser = existingUser is null;

            user = existingUser ?? User.Create(email, name, identityInfo.EmailVerified, now);
            if (isNewUser)
            {
                _userRepository.Add(user);
                _logger.Information("Created user {UserId} from provider {ProviderId}", user.Id.Value, provider.Id.Value);
            }
            else
            {
                await SecureUnverifiedAccountAsync(user, now, cancellationToken);
            }

            user.LinkExternalIdentity(provider.Id, providerUserId, email, name, now);
        }

        user.UpdateProfile(new PersonName(identityInfo.Name), identityInfo.EmailVerified);

        var started = SsoSessions.Start(
            user.Id, new AuthMethod(provider.Id.Value), request.UserAgent, request.IpAddress, _tokenGenerator, _tokenHasher, _identityOptions, now);
        _sessionRepository.Add(started.Session);

        var redirectUrl = SignInCompletion.Complete(authorizationRequest, started.Session, _tokenGenerator, _tokenHasher, _identityOptions, now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.Information(
            "Handled OAuth callback for provider {ProviderId}, session {SessionId}, user {UserId}, client {ClientId}",
            provider.Id.Value,
            started.Session.Id.Value,
            user.Id.Value,
            authorizationRequest.ClientId.Value);

        return new AuthCallbackResult(redirectUrl, started.SsoToken, started.ExpiresAt);
    }

    // Someone may have registered this email with a password without owning it. The provider has now proven who owns
    // it, so that password and every session started with it are removed before the owner gets in.
    private async Task SecureUnverifiedAccountAsync(User user, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (!user.SecureWithVerifiedEmail())
        {
            return;
        }

        foreach (var session in await _sessionRepository.GetActiveForUserAsync(user.Id, cancellationToken))
        {
            session.RevokeWithTokens(now);
        }

        _logger.Warning("Removed the password and ended the sessions of unverified user {UserId} on provider sign-in", user.Id.Value);
    }
}
