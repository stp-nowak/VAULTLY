using Microsoft.Extensions.Options;

using Serilog;

using Vaultly.Core;
using Vaultly.Core.Messaging;
using Vaultly.Identity.Application.Common;
using Vaultly.Identity.Application.Models;
using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Application.Security;
using Vaultly.Identity.Domain.AuthorizationRequests;
using Vaultly.Identity.Domain.Sessions;
using Vaultly.Identity.Domain.SignInMethods;
using Vaultly.Identity.Domain.Users;

namespace Vaultly.Identity.Application.Features.Passwords.RegisterWithPassword;

public sealed class RegisterWithPasswordHandler(
    IAuthorizationRequestRepository authorizationRequestRepository,
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    ISignInSettingsRepository signInSettingsRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator,
    ITokenHasher tokenHasher,
    TimeProvider clock,
    IOptions<IdentityOptions> identityOptions,
    ILogger logger) : ICommandHandler<RegisterWithPasswordCommand, AuthCallbackResult>
{
    private readonly IAuthorizationRequestRepository _authorizationRequestRepository = authorizationRequestRepository;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ISessionRepository _sessionRepository = sessionRepository;
    private readonly ISignInSettingsRepository _signInSettingsRepository = signInSettingsRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly ITokenGenerator _tokenGenerator = tokenGenerator;
    private readonly ITokenHasher _tokenHasher = tokenHasher;
    private readonly TimeProvider _clock = clock;
    private readonly IdentityOptions _identityOptions = identityOptions.Value;
    private readonly ILogger _logger = logger.ForContext<RegisterWithPasswordHandler>();

    public async Task<AuthCallbackResult> Handle(RegisterWithPasswordCommand request, CancellationToken cancellationToken)
    {
        await SignInMethodGuard.EnsurePasswordUsableAsync(_signInSettingsRepository, cancellationToken);

        var now = _clock.GetUtcNow();
        var authorizationRequest = await _authorizationRequestRepository.GetByIdAsync(
            AuthorizationRequestId.From(request.AuthorizationRequestId),
            cancellationToken) ?? throw new AuthorizationRequestNotFoundException();
        authorizationRequest.EnsurePending(now);

        var email = new EmailAddress(request.Email);
        var name = new PersonName(request.Name);
        var password = new Password(request.Password);

        if (await _userRepository.GetByEmailAsync(email, cancellationToken) is not null)
        {
            throw PasswordSignInException.EmailAlreadyUsed();
        }

        var user = User.RegisterWithPassword(email, name, _passwordHasher.Hash(password), now);
        _userRepository.Add(user);

        var started = SsoSessions.Start(
            user.Id, new AuthMethod(SignInMethod.Password.Value), request.UserAgent, request.IpAddress, _tokenGenerator, _tokenHasher, _identityOptions, now);
        _sessionRepository.Add(started.Session);
        var redirectUrl = SignInCompletion.Complete(authorizationRequest, started.Session, _tokenGenerator, _tokenHasher, _identityOptions, now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.Information("Registered user {UserId} with a password, client {ClientId}", user.Id.Value, authorizationRequest.ClientId.Value);

        return new AuthCallbackResult(redirectUrl, started.SsoToken, started.ExpiresAt);
    }
}
