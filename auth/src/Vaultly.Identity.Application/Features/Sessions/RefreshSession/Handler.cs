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

namespace Vaultly.Identity.Application.Features.Sessions.RefreshSession;

/// <summary>
/// Handles the refresh_token grant with rotation.
/// </summary>
public sealed class RefreshSessionHandler(
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
    : ICommandHandler<RefreshSessionCommand, TokenResult>
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
    private readonly ILogger _logger = logger.ForContext<RefreshSessionHandler>();

    public async Task<TokenResult> Handle(RefreshSessionCommand request, CancellationToken cancellationToken)
    {
        var client = _clientRegistry.Authenticate(request.ClientId, request.ClientSecret)
            ?? throw OAuthProtocolException.InvalidClient();

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw OAuthProtocolException.InvalidRequest("refresh_token is required.");
        }

        var now = _clock.UtcNow;
        var tokenHash = new TokenHash(_tokenHasher.HashToken(request.RefreshToken));
        var session = await _sessionRepository.GetByRefreshTokenHashAsync(tokenHash, cancellationToken);
        if (session is null)
        {
            _logger.Warning("Refresh token lookup failed for client {ClientId}", client.Id.Value);
            throw OAuthProtocolException.InvalidGrant("Refresh token is invalid.");
        }

        try
        {
            var plainRefreshToken = _tokenGenerator.GenerateToken(64);
            var rotated = session.RotateRefreshToken(
                tokenHash,
                new TokenHash(_tokenHasher.HashToken(plainRefreshToken)),
                now,
                now.AddDays(_identityOptions.RefreshTokenTtlDays));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var user = await _userRepository.GetByIdAsync(session.UserId, cancellationToken)
                ?? throw new InvalidOperationException("User not found.");

            _logger.Information(
                "Rotated refresh token for session {SessionId}, user {UserId}, client {ClientId}",
                session.Id.Value,
                user.Id.Value,
                client.Id.Value);

            return TokenResults.Create(_jwtTokenService, _identityOptions, user, session, plainRefreshToken, rotated, now);
        }
        catch (DomainException)
        {
            session.RevokeWithTokens(now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.Warning(
                "Refresh token rotation failed for session {SessionId}; session revoked",
                session.Id.Value);

            throw OAuthProtocolException.InvalidGrant("Refresh token is invalid or expired.");
        }
    }
}
