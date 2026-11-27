using Microsoft.Extensions.Options;

using Serilog;

using Vaultly.Core;
using Vaultly.Core.Messaging;
using Vaultly.Identity.Application.Common;
using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Application.Security;
using Vaultly.Identity.Domain.Sessions;

namespace Vaultly.Identity.Application.Features.Sessions.EndSession;

/// <summary>
/// Ends the browser's SSO session (RP-initiated logout).
/// </summary>
public sealed class EndSessionHandler(
    IClientRegistry clientRegistry,
    ISessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    ITokenHasher tokenHasher,
    TimeProvider clock,
    IOptions<IdentityOptions> identityOptions,
    ILogger logger) : ICommandHandler<EndSessionCommand, EndSessionResult>
{
    private readonly IClientRegistry _clientRegistry = clientRegistry;
    private readonly ISessionRepository _sessionRepository = sessionRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ITokenHasher _tokenHasher = tokenHasher;
    private readonly TimeProvider _clock = clock;
    private readonly IdentityOptions _identityOptions = identityOptions.Value;
    private readonly ILogger _logger = logger.ForContext<EndSessionHandler>();

    public async Task<EndSessionResult> Handle(EndSessionCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.SsoToken))
        {
            var session = await _sessionRepository.GetBySsoTokenHashAsync(
                new TokenHash(_tokenHasher.HashToken(request.SsoToken)),
                cancellationToken);

            if (session is not null && !session.IsRevoked)
            {
                session.RevokeWithTokens(_clock.GetUtcNow());
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.Information("Ended SSO session {SessionId} for user {UserId}", session.Id.Value, session.UserId.Value);
            }
        }

        // Only registered post-logout URIs are honoured; anything else falls back to our own page (no open redirect).
        var postLogoutRedirectUri = ClientRedirect.TryParse(request.PostLogoutRedirectUri);
        if (postLogoutRedirectUri is not null
            && _clientRegistry.Find(request.ClientId) is { } client
            && client.IsPostLogoutRedirectUriAllowed(postLogoutRedirectUri))
        {
            return new EndSessionResult(postLogoutRedirectUri.Value);
        }

        return new EndSessionResult($"{_identityOptions.ResolveLoginUiBaseUrl()}/logged-out");
    }
}
