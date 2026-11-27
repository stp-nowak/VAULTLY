using Serilog;

using Vaultly.Core;
using Vaultly.Core.Messaging;
using Vaultly.Identity.Application.Common;
using Vaultly.Identity.Application.Interfaces.Repositories;
using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Application.Features.Sessions.RevokeToken;

/// <summary>
/// Handles refresh token revocation.
/// </summary>
public sealed class RevokeTokenHandler(
    IClientRegistry clientRegistry,
    ISessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    ITokenHasher tokenHasher,
    IClock clock,
    ILogger logger) : ICommandHandler<RevokeTokenCommand>
{
    private readonly IClientRegistry _clientRegistry = clientRegistry;
    private readonly ISessionRepository _sessionRepository = sessionRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ITokenHasher _tokenHasher = tokenHasher;
    private readonly IClock _clock = clock;
    private readonly ILogger _logger = logger.ForContext<RevokeTokenHandler>();

    public async Task Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var client = _clientRegistry.Authenticate(request.ClientId, request.ClientSecret)
            ?? throw OAuthProtocolException.InvalidClient();

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            throw OAuthProtocolException.InvalidRequest("token is required.");
        }

        var session = await _sessionRepository.GetByRefreshTokenHashAsync(
            new TokenHash(_tokenHasher.HashToken(request.Token)),
            cancellationToken);

        if (session is null)
        {
            return;
        }

        session.RevokeWithTokens(_clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.Information(
            "Client {ClientId} revoked session {SessionId} for user {UserId}",
            client.Id.Value,
            session.Id.Value,
            session.UserId.Value);
    }
}
