using MediatR;

using Serilog;

using Vaultly.Identity.Application.Interfaces.Repositories;
using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Domain.ValueObjects;
using Vaultly.SharedKernel;

namespace Vaultly.Identity.Application.UseCases.Logout;

/// <summary>
/// Command to log out a session by refresh token.
/// </summary>
public sealed record LogoutCommand(string? RefreshToken) : IRequest<Unit>;

/// <summary>
/// Handles logout by revoking the session refresh token.
/// </summary>
public sealed class LogoutHandler(
    ISessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    ITokenHasher tokenHasher,
    IClock clock,
    ILogger logger) : IRequestHandler<LogoutCommand, Unit>
{
    private readonly ISessionRepository _sessionRepository = sessionRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ITokenHasher _tokenHasher = tokenHasher;
    private readonly IClock _clock = clock;
    private readonly ILogger _logger = logger.ForContext<LogoutHandler>();

    /// <summary>
    /// Logs out the session if a refresh token is provided.
    /// </summary>
    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Unit.Value;
        }

        var tokenHash = new TokenHash(_tokenHasher.HashToken(request.RefreshToken));
        var session = await _sessionRepository.GetByRefreshTokenHashAsync(tokenHash, cancellationToken);
        if (session is null)
        {
            return Unit.Value;
        }

        session.RevokeWithTokens(_clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.Information(
            "Logged out session {SessionId} for user {UserId}",
            session.Id.Value,
            session.UserId.Value);

        return Unit.Value;
    }
}