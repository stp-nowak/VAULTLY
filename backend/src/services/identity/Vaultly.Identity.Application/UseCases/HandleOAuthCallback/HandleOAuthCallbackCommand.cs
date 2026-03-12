using MediatR;

using Microsoft.Extensions.Options;

using Serilog;

using Vaultly.Identity.Application.Interfaces.Repositories;
using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Application.Models;
using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Application.UseCases.OAuth;
using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.Exceptions;
using Vaultly.Identity.Domain.ValueObjects;
using Vaultly.SharedKernel;

namespace Vaultly.Identity.Application.UseCases.HandleOAuthCallback;

/// <summary>
/// Command to handle an OAuth callback.
/// </summary>
public sealed record HandleOAuthCallbackCommand(
    string ProviderId,
    string Code,
    string State,
    string? UserAgent,
    string? IpAddress) : IRequest<AuthCallbackResult>;

/// <summary>
/// Handles OAuth callback processing and auth code issuance.
/// </summary>
public sealed class HandleOAuthCallbackHandler(
    IOAuthStateRepository oauthStateRepository,
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    IEnumerable<IExternalOAuthProvider> providers,
    ITokenGenerator tokenGenerator,
    ITokenHasher tokenHasher,
    IClock clock,
    IOptions<IdentityOptions> identityOptions,
    ILogger logger)
    : IRequestHandler<HandleOAuthCallbackCommand, AuthCallbackResult>
{
    private readonly IOAuthStateRepository _oauthStateRepository = oauthStateRepository;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ISessionRepository _sessionRepository = sessionRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IReadOnlyDictionary<ProviderId, IExternalOAuthProvider> _providers = providers.ToDictionary(x => x.Id);
    private readonly ITokenGenerator _tokenGenerator = tokenGenerator;
    private readonly ITokenHasher _tokenHasher = tokenHasher;
    private readonly IClock _clock = clock;
    private readonly IdentityOptions _identityOptions = identityOptions.Value;
    private readonly ILogger _logger = logger.ForContext<HandleOAuthCallbackHandler>();

    /// <summary>
    /// Handles the OAuth callback and returns the redirect URL.
    /// </summary>
    public async Task<AuthCallbackResult> Handle(HandleOAuthCallbackCommand request, CancellationToken cancellationToken)
    {
        var provider = OAuthFlowUtilities.GetProvider(_providers, new ProviderId(request.ProviderId));
        if (!provider.IsEnabled)
        {
            _logger.Warning("OAuth provider {ProviderId} is disabled", provider.Id.Value);
            throw new InvalidOperationException("OAuth provider is disabled.");
        }

        var now = _clock.UtcNow;
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

            user.LinkExternalIdentity(provider.Id, providerUserId, email, name, now);
        }

        user.UpdateProfile(new PersonName(identityInfo.Name), identityInfo.EmailVerified);

        var session = Session.Create(
            user.Id,
            new AuthMethod(provider.Id.Value),
            UserAgent.FromNullable(OAuthFlowUtilities.Truncate(request.UserAgent, 512)),
            IpAddress.FromNullable(OAuthFlowUtilities.Truncate(request.IpAddress, 64)),
            now);

        _sessionRepository.Add(session);

        var authCode = _tokenGenerator.GenerateToken(32);
        session.IssueAuthCode(
            new CodeHash(_tokenHasher.HashToken(authCode)),
            now.AddMinutes(_identityOptions.AuthCodeTtlMinutes));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.Information(
            "Handled OAuth callback for provider {ProviderId}, session {SessionId}, user {UserId}",
            provider.Id.Value,
            session.Id.Value,
            user.Id.Value);

        if (oauthState.RedirectUri is null)
        {
            throw new InvalidOperationException("OAuth redirect URI is missing.");
        }

        return new AuthCallbackResult(OAuthFlowUtilities.BuildFrontendRedirectUrl(authCode, oauthState.RedirectUri));
    }
}