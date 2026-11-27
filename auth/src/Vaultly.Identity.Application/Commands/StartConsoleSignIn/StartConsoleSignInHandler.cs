using Microsoft.Extensions.Options;

using Serilog;

using Vaultly.Core;
using Vaultly.Identity.Application.Common;
using Vaultly.Identity.Application.Interfaces.Repositories;
using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Application.Messaging;
using Vaultly.Identity.Application.Models;
using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Application.Commands.StartConsoleSignIn;

public sealed class StartConsoleSignInHandler(
    IAuthorizationRequestRepository authorizationRequestRepository,
    ISessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    ITokenHasher tokenHasher,
    IClock clock,
    IOptions<IdentityOptions> identityOptions,
    ILogger logger) : ICommandHandler<StartConsoleSignInCommand, AuthorizationResult>
{
    private readonly IAuthorizationRequestRepository _authorizationRequestRepository = authorizationRequestRepository;
    private readonly ISessionRepository _sessionRepository = sessionRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ITokenHasher _tokenHasher = tokenHasher;
    private readonly IClock _clock = clock;
    private readonly IdentityOptions _identityOptions = identityOptions.Value;
    private readonly ILogger _logger = logger.ForContext<StartConsoleSignInHandler>();

    public async Task<AuthorizationResult> Handle(StartConsoleSignInCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var consoleUrl = _identityOptions.ResolveConsoleUrl();

        if (await HasActiveSsoSessionAsync(request.SsoToken, now, cancellationToken))
        {
            return new AuthorizationResult(consoleUrl);
        }

        var authorizationRequest = AuthorizationRequest.CreateForConsole(
            new RedirectUri(consoleUrl),
            now,
            now.AddMinutes(_identityOptions.AuthorizationRequestTtlMinutes));

        _authorizationRequestRepository.Add(authorizationRequest);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.Information("Started admin console sign-in {AuthorizationRequestId}", authorizationRequest.Id.Value);

        return new AuthorizationResult(
            OAuthFlowUtilities.BuildUrl(
                $"{_identityOptions.ResolveLoginUiBaseUrl()}/login",
                new Dictionary<string, string?> { ["request"] = authorizationRequest.Id.Value.ToString() }));
    }

    private async Task<bool> HasActiveSsoSessionAsync(string? ssoToken, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(ssoToken))
        {
            return false;
        }

        var session = await _sessionRepository.GetBySsoTokenHashAsync(new TokenHash(_tokenHasher.HashToken(ssoToken)), cancellationToken);
        return session is not null && session.IsSsoActive(now);
    }
}
