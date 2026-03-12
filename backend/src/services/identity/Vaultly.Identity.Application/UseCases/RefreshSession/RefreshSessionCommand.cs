using MediatR;

using Microsoft.Extensions.Options;

using Serilog;

using Vaultly.Identity.Application.Interfaces.Repositories;
using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Application.Models;
using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Domain.Exceptions;
using Vaultly.Identity.Domain.ValueObjects;
using Vaultly.SharedKernel;

namespace Vaultly.Identity.Application.UseCases.RefreshSession;

/// <summary>
/// Command to refresh a session using a refresh token.
/// </summary>
public sealed record RefreshSessionCommand(string RefreshToken) : IRequest<RefreshResult>;

/// <summary>
/// Handles refresh token rotation and access token issuance.
/// </summary>
public sealed class RefreshSessionHandler(
    ISessionRepository sessionRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IJwtTokenService jwtTokenService,
    ITokenGenerator tokenGenerator,
    ITokenHasher tokenHasher,
    IClock clock,
    IOptions<IdentityOptions> identityOptions,
    ILogger logger)
    : IRequestHandler<RefreshSessionCommand, RefreshResult>
{
    private readonly ISessionRepository _sessionRepository = sessionRepository;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IJwtTokenService _jwtTokenService = jwtTokenService;
    private readonly ITokenGenerator _tokenGenerator = tokenGenerator;
    private readonly ITokenHasher _tokenHasher = tokenHasher;
    private readonly IClock _clock = clock;
    private readonly IdentityOptions _identityOptions = identityOptions.Value;
    private readonly ILogger _logger = logger.ForContext<RefreshSessionHandler>();

    /// <summary>
    /// Refreshes the session for the provided refresh token.
    /// </summary>
    public async Task<RefreshResult> Handle(RefreshSessionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return RefreshResult.Failed(_identityOptions.RefreshTokenCookieDomain);
        }

        var now = _clock.UtcNow;
        var tokenHash = new TokenHash(_tokenHasher.HashToken(request.RefreshToken));
        var session = await _sessionRepository.GetByRefreshTokenHashAsync(tokenHash, cancellationToken);
        if (session is null)
        {
            _logger.Warning("Refresh token lookup failed");
            return RefreshResult.Failed(_identityOptions.RefreshTokenCookieDomain);
        }

        string plainRefreshToken;
        try
        {
            plainRefreshToken = _tokenGenerator.GenerateToken(64);
            var rotatedToken = session.RotateRefreshToken(
                tokenHash,
                new TokenHash(_tokenHasher.HashToken(plainRefreshToken)),
                now,
                now.AddDays(_identityOptions.RefreshTokenTtlDays));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var user = await _userRepository.GetByIdAsync(session.UserId, cancellationToken)
                ?? throw new InvalidOperationException("User not found.");

            var accessToken = _jwtTokenService.CreateAccessToken(user, session, now);
            var userDto = new AuthenticatedUserDto(user.Id.Value, user.Email.Value, user.Name.Value);

            _logger.Information(
                "Rotated refresh token for session {SessionId} and user {UserId}",
                session.Id.Value,
                user.Id.Value);

            return RefreshResult.Succeeded(
                accessToken,
                _identityOptions.AccessTokenTtlMinutes * 60,
                plainRefreshToken,
                rotatedToken.ExpiresAt,
                session.Id.Value,
                userDto,
                _identityOptions.RefreshTokenCookieDomain);
        }
        catch (DomainException)
        {
            session.RevokeWithTokens(now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.Warning(
                "Refresh token rotation failed for session {SessionId} and user {UserId}",
                session.Id.Value,
                session.UserId.Value);

            return RefreshResult.Failed(_identityOptions.RefreshTokenCookieDomain);
        }
    }
}