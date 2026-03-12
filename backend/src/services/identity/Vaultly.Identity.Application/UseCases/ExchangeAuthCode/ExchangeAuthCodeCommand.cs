using MediatR;

using Serilog;

using Vaultly.Identity.Application.Interfaces.Repositories;
using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Application.Models;
using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Domain.Exceptions;
using Vaultly.Identity.Domain.ValueObjects;
using Vaultly.SharedKernel;

using Microsoft.Extensions.Options;

namespace Vaultly.Identity.Application.UseCases.ExchangeAuthCode;

/// <summary>
/// Command to exchange an auth code for tokens.
/// </summary>
public sealed record ExchangeAuthCodeCommand(string Code) : IRequest<ExchangeResult>;

/// <summary>
/// Handles exchanging an auth code for access and refresh tokens.
/// </summary>
public sealed class ExchangeAuthCodeHandler(
    ISessionRepository sessionRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IJwtTokenService jwtTokenService,
    ITokenGenerator tokenGenerator,
    ITokenHasher tokenHasher,
    IClock clock,
    IOptions<IdentityOptions> identityOptions,
    ILogger logger)
    : IRequestHandler<ExchangeAuthCodeCommand, ExchangeResult>
{
    private readonly ISessionRepository _sessionRepository = sessionRepository;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IJwtTokenService _jwtTokenService = jwtTokenService;
    private readonly ITokenGenerator _tokenGenerator = tokenGenerator;
    private readonly ITokenHasher _tokenHasher = tokenHasher;
    private readonly IClock _clock = clock;
    private readonly IdentityOptions _identityOptions = identityOptions.Value;
    private readonly ILogger _logger = logger.ForContext<ExchangeAuthCodeHandler>();

    /// <summary>
    /// Exchanges the provided auth code for tokens.
    /// </summary>
    public async Task<ExchangeResult> Handle(ExchangeAuthCodeCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new InvalidOperationException("Auth code is required.");
        }

        var now = _clock.UtcNow;
        var codeHash = new CodeHash(_tokenHasher.HashToken(request.Code));

        var session = await _sessionRepository.GetByAuthCodeHashAsync(codeHash, cancellationToken);
        if (session is null)
        {
            throw new AuthCodeNotFoundException();
        }

        session.ConsumeAuthCode(codeHash, now);

        var refreshToken = _tokenGenerator.GenerateToken(64);
        var issuedRefreshToken = session.IssueRefreshToken(
            new TokenHash(_tokenHasher.HashToken(refreshToken)),
            now,
            now.AddDays(_identityOptions.RefreshTokenTtlDays));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var user = await _userRepository.GetByIdAsync(session.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        var accessToken = _jwtTokenService.CreateAccessToken(user, session, now);
        var userDto = new AuthenticatedUserDto(user.Id.Value, user.Email.Value, user.Name.Value);

        _logger.Information(
            "Exchanged auth code for session {SessionId} and user {UserId}",
            session.Id.Value,
            user.Id.Value);

        return new ExchangeResult(
            accessToken,
            _identityOptions.AccessTokenTtlMinutes * 60,
            refreshToken,
            issuedRefreshToken.ExpiresAt,
            session.Id.Value,
            userDto,
            _identityOptions.RefreshTokenCookieDomain);
    }
}