using Microsoft.Extensions.Options;

using Serilog;

using Vaultly.Core;
using Vaultly.Identity.Application.Common;
using Vaultly.Identity.Application.Features.Authorization;
using Vaultly.Identity.Application.Interfaces.Repositories;
using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Application.Messaging;
using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.Exceptions;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Application.Features.Authorization.StartAuthorization;

/// <summary>
/// Validates an authorization request and either answers it from the SSO session (silent sign-in) or stores it and
/// sends the browser to the login UI.
/// </summary>
public sealed class StartAuthorizationHandler(
    IClientRegistry clientRegistry,
    IAuthorizationRequestRepository authorizationRequestRepository,
    ISessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    ITokenGenerator tokenGenerator,
    ITokenHasher tokenHasher,
    IClock clock,
    IOptions<IdentityOptions> identityOptions,
    ILogger logger) : ICommandHandler<StartAuthorizationCommand, AuthorizationResult>
{
    private readonly IClientRegistry _clientRegistry = clientRegistry;
    private readonly IAuthorizationRequestRepository _authorizationRequestRepository = authorizationRequestRepository;
    private readonly ISessionRepository _sessionRepository = sessionRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ITokenGenerator _tokenGenerator = tokenGenerator;
    private readonly ITokenHasher _tokenHasher = tokenHasher;
    private readonly IClock _clock = clock;
    private readonly IdentityOptions _identityOptions = identityOptions.Value;
    private readonly ILogger _logger = logger.ForContext<StartAuthorizationHandler>();

    public async Task<AuthorizationResult> Handle(StartAuthorizationCommand request, CancellationToken cancellationToken)
    {
        // 1. Client and redirect URI first: until both are trusted, errors must NOT be redirected (open redirect).
        var client = _clientRegistry.Find(request.ClientId)
            ?? throw OAuthProtocolException.InvalidRequest("Unknown client.");

        var redirectUri = ClientRedirect.TryParse(request.RedirectUri);
        if (redirectUri is null || !client.IsRedirectUriAllowed(redirectUri))
        {
            _logger.Warning("Rejected authorization request with unregistered redirect URI for client {ClientId}", client.Id.Value);
            throw OAuthProtocolException.InvalidRequest("Redirect URI is not registered for this client.");
        }

        // 2. From here errors go back to the client as ?error=…&state=… (RFC 6749 §4.1.2.1).
        if (!string.Equals(request.ResponseType, "code", StringComparison.Ordinal))
        {
            return ErrorRedirect(redirectUri, request.State, OAuthError.UnsupportedResponseType, "Only response_type=code is supported.");
        }

        if (string.IsNullOrEmpty(request.State) || request.State.Length > ClientState.MaxLength)
        {
            return ErrorRedirect(redirectUri, request.State, OAuthError.InvalidRequest, "A state parameter is required.");
        }

        if (!string.Equals(request.CodeChallengeMethod, CodeChallenge.MethodS256, StringComparison.Ordinal)
            || !TryCreateChallenge(request.CodeChallenge, out var codeChallenge))
        {
            return ErrorRedirect(redirectUri, request.State, OAuthError.InvalidRequest, "PKCE with code_challenge_method=S256 is required.");
        }

        var state = new ClientState(request.State);
        var now = _clock.UtcNow;

        // 3. Silent sign-in when the browser still has an active SSO session.
        if (!string.IsNullOrEmpty(request.SsoToken))
        {
            var session = await _sessionRepository.GetBySsoTokenHashAsync(
                new TokenHash(_tokenHasher.HashToken(request.SsoToken)),
                cancellationToken);

            if (session is not null && session.IsSsoActive(now))
            {
                var code = _tokenGenerator.GenerateToken(32);
                session.IssueAuthCode(
                    new CodeHash(_tokenHasher.HashToken(code)),
                    now.AddMinutes(_identityOptions.AuthCodeTtlMinutes),
                    client.Id,
                    redirectUri,
                    codeChallenge);
                session.MarkUsed(now);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.Information(
                    "Signed in session {SessionId} to client {ClientId} via SSO",
                    session.Id.Value,
                    client.Id.Value);

                return new AuthorizationResult(ClientRedirect.WithCode(redirectUri, code, state));
            }
        }

        // 4. Otherwise remember the request and let the user sign in on the login UI.
        var authorizationRequest = AuthorizationRequest.Create(
            client.Id,
            redirectUri,
            state,
            codeChallenge,
            now,
            now.AddMinutes(_identityOptions.AuthorizationRequestTtlMinutes));

        _authorizationRequestRepository.Add(authorizationRequest);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.Information(
            "Started authorization request {AuthorizationRequestId} for client {ClientId}",
            authorizationRequest.Id.Value,
            client.Id.Value);

        return new AuthorizationResult(
            OAuthFlowUtilities.BuildUrl(
                $"{_identityOptions.ResolveLoginUiBaseUrl()}/login",
                new Dictionary<string, string?> { ["request"] = authorizationRequest.Id.Value.ToString() }));
    }

    private static AuthorizationResult ErrorRedirect(RedirectUri redirectUri, string? state, OAuthError error, string description)
        => new(OAuthFlowUtilities.BuildUrl(redirectUri.Value, new Dictionary<string, string?>
        {
            ["error"] = WireNames.Of(error),
            ["error_description"] = description,
            ["state"] = state is { Length: <= ClientState.MaxLength } ? state : null,
        }));

    private static bool TryCreateChallenge(string? value, out CodeChallenge challenge)
    {
        try
        {
            challenge = new CodeChallenge(value ?? string.Empty);
            return true;
        }
        catch (InvalidValueException)
        {
            challenge = null!;
            return false;
        }
    }
}
