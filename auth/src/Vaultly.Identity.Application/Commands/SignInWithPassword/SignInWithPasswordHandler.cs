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
using Vaultly.Identity.Domain.Exceptions;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Application.Commands.SignInWithPassword;

public sealed class SignInWithPasswordHandler(
    IAuthorizationRequestRepository authorizationRequestRepository,
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    ISignInSettingsRepository signInSettingsRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator,
    ITokenHasher tokenHasher,
    IClock clock,
    IOptions<IdentityOptions> identityOptions,
    ILogger logger) : ICommandHandler<SignInWithPasswordCommand, AuthCallbackResult>
{
    private readonly IAuthorizationRequestRepository _authorizationRequestRepository = authorizationRequestRepository;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ISessionRepository _sessionRepository = sessionRepository;
    private readonly ISignInSettingsRepository _signInSettingsRepository = signInSettingsRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly ITokenGenerator _tokenGenerator = tokenGenerator;
    private readonly ITokenHasher _tokenHasher = tokenHasher;
    private readonly IClock _clock = clock;
    private readonly IdentityOptions _identityOptions = identityOptions.Value;
    private readonly ILogger _logger = logger.ForContext<SignInWithPasswordHandler>();

    public async Task<AuthCallbackResult> Handle(SignInWithPasswordCommand request, CancellationToken cancellationToken)
    {
        await SignInMethodGuard.EnsurePasswordUsableAsync(_signInSettingsRepository, cancellationToken);

        var now = _clock.UtcNow;
        var authorizationRequest = await _authorizationRequestRepository.GetByIdAsync(
            AuthorizationRequestId.From(request.AuthorizationRequestId),
            cancellationToken) ?? throw new AuthorizationRequestNotFoundException();
        authorizationRequest.EnsurePending(now);

        var password = ParseCredentials(request, out var email);
        var user = await VerifyPasswordAsync(email, password, now, cancellationToken);

        var started = SsoSessions.Start(
            user.Id, new AuthMethod(SignInMethod.Password.Value), request.UserAgent, request.IpAddress, _tokenGenerator, _tokenHasher, _identityOptions, now);
        _sessionRepository.Add(started.Session);
        var redirectUrl = SignInCompletion.Complete(authorizationRequest, started.Session, _tokenGenerator, _tokenHasher, _identityOptions, now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.Information(
            "Signed in user {UserId} with a password, session {SessionId}, client {ClientId}",
            user.Id.Value,
            started.Session.Id.Value,
            authorizationRequest.ClientId.Value);

        return new AuthCallbackResult(redirectUrl, started.SsoToken, started.ExpiresAt);
    }

    // Malformed input is just a wrong password: it must not reveal anything the other failures do not.
    private static Password ParseCredentials(SignInWithPasswordCommand request, out EmailAddress email)
    {
        try
        {
            email = new EmailAddress(request.Email);
            return new Password(request.Password);
        }
        catch (Exception ex) when (ex is InvalidValueException or WeakPasswordException)
        {
            throw PasswordSignInException.WrongCredentials();
        }
    }

    private async Task<User> VerifyPasswordAsync(EmailAddress email, Password password, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user?.PasswordHash is not { } hash || user.IsLockedOut(now))
        {
            _passwordHasher.VerifyWithoutAccount(password);
            throw PasswordSignInException.WrongCredentials();
        }

        var verification = _passwordHasher.Verify(hash, password);
        if (verification == PasswordVerification.Failed)
        {
            user.RecordFailedSignIn(now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.Warning("Wrong password for user {UserId}", user.Id.Value);
            throw PasswordSignInException.WrongCredentials();
        }

        if (verification == PasswordVerification.SucceededRehashNeeded)
        {
            user.UpgradePasswordHash(_passwordHasher.Hash(password));
        }

        user.RecordSuccessfulSignIn();
        return user;
    }
}
