using Microsoft.Extensions.Options;

using Serilog;

using Vaultly.Core;
using Vaultly.Core.Messaging;
using Vaultly.Identity.Application.Common;
using Vaultly.Identity.Application.Interfaces.Repositories;
using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Application.Models;
using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Application.Features.Authorization.ExchangeAuthCode;

/// <summary>
/// Handles the authorization_code grant.
/// </summary>
public sealed class ExchangeAuthCodeHandler(
    IClientRegistry clientRegistry,
    ISessionRepository sessionRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IJwtTokenService jwtTokenService,
    ITokenGenerator tokenGenerator,
    ITokenHasher tokenHasher,
    IClock clock,
    IOptions<IdentityOptions> identityOptions,
    ILogger logger)
    : ICommandHandler<ExchangeAuthCodeCommand, TokenResult>
{
    private readonly IClientRegistry _clientRegistry = clientRegistry;
    private readonly ISessionRepository _sessionRepository = sessionRepository;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IJwtTokenService _jwtTokenService = jwtTokenService;
    private readonly ITokenGenerator _tokenGenerator = tokenGenerator;
    private readonly ITokenHasher _tokenHasher = tokenHasher;
    private readonly IClock _clock = clock;
    private readonly IdentityOptions _identityOptions = identityOptions.Value;
    private readonly ILogger _logger = logger.ForContext<ExchangeAuthCodeHandler>();

    public async Task<TokenResult> Handle(ExchangeAuthCodeCommand request, CancellationToken cancellationToken)
    {
        var client = _clientRegistry.Authenticate(request.ClientId, request.ClientSecret)
            ?? throw OAuthProtocolException.InvalidClient();

        var redirectUri = ClientRedirect.TryParse(request.RedirectUri);
        if (string.IsNullOrWhiteSpace(request.Code) || redirectUri is null || string.IsNullOrEmpty(request.CodeVerifier))
        {
            throw OAuthProtocolException.InvalidRequest("code, redirect_uri and code_verifier are required.");
        }

        var now = _clock.UtcNow;
        var codeHash = new CodeHash(_tokenHasher.HashToken(request.Code));
        var session = await _sessionRepository.GetByAuthCodeHashAsync(codeHash, cancellationToken)
            ?? throw OAuthProtocolException.InvalidGrant("Authorization code is invalid.");

        try
        {
            session.ConsumeAuthCode(codeHash, client.Id, redirectUri, request.CodeVerifier, now);
        }
        catch (DomainException ex)
        {
            // Persist the burned code: a failed attempt (e.g. wrong verifier) must not leave it redeemable.
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.Warning("Rejected authorization code for client {ClientId}: {Reason}", client.Id.Value, ex.Message);
            throw OAuthProtocolException.InvalidGrant("Authorization code is invalid, expired or does not match the request.");
        }

        var plainRefreshToken = _tokenGenerator.GenerateToken(64);
        var refreshToken = session.IssueRefreshToken(
            new TokenHash(_tokenHasher.HashToken(plainRefreshToken)),
            now,
            now.AddDays(_identityOptions.RefreshTokenTtlDays));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var user = await _userRepository.GetByIdAsync(session.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        _logger.Information(
            "Exchanged auth code for session {SessionId}, user {UserId}, client {ClientId}",
            session.Id.Value,
            user.Id.Value,
            client.Id.Value);

        return TokenResults.Create(_jwtTokenService, _identityOptions, user, session, plainRefreshToken, refreshToken, now);
    }
}
