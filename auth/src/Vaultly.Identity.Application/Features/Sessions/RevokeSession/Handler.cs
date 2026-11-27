using Serilog;

using Vaultly.Core;
using Vaultly.Core.Messaging;
using Vaultly.Identity.Domain.Sessions;
using Vaultly.Identity.Domain.Users;

namespace Vaultly.Identity.Application.Features.Sessions.RevokeSession;

/// <summary>
/// Handles session revocation for a user.
/// </summary>
public sealed class RevokeSessionHandler(
    ISessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    ILogger logger) : ICommandHandler<RevokeSessionCommand, bool>
{
    private readonly ISessionRepository _sessionRepository = sessionRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly TimeProvider _clock = clock;
    private readonly ILogger _logger = logger.ForContext<RevokeSessionHandler>();

    /// <summary>
    /// Revokes the requested session if it exists.
    /// </summary>
    public async Task<bool> Handle(RevokeSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetWithTokensForUserAsync(
            SessionId.From(request.SessionId),
            UserId.From(request.UserId),
            cancellationToken);

        if (session is null)
        {
            return false;
        }

        session.RevokeWithTokens(_clock.GetUtcNow());
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.Information(
            "Revoked session {SessionId} for user {UserId}",
            session.Id.Value,
            session.UserId.Value);

        return true;
    }
}
