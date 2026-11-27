using Microsoft.Extensions.Options;

using Serilog;

using Vaultly.Core;
using Vaultly.Identity.Application.Interfaces.Repositories;
using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Application.Messaging;
using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.Services;
using Vaultly.Identity.Domain.ValueObjects;
using Vaultly.Identity.PublicTypes;

namespace Vaultly.Identity.Application.Commands.EnsureBootstrapAdmin;

public sealed class EnsureBootstrapAdminHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IClock clock,
    IOptions<IdentityOptions> identityOptions,
    ILogger logger) : ICommandHandler<EnsureBootstrapAdminCommand>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly IClock _clock = clock;
    private readonly BootstrapAdminOptions _options = identityOptions.Value.BootstrapAdmin;
    private readonly ILogger _logger = logger.ForContext<EnsureBootstrapAdminHandler>();

    public async Task Handle(EnsureBootstrapAdminCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Email))
        {
            return;
        }

        var adminCount = await _userRepository.CountAdminsAsync(cancellationToken);
        if (adminCount > 0)
        {
            return;
        }

        var email = new EmailAddress(_options.Email);
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            // Treated as verified: the operator vouches for this address.
            user = User.Create(email, NameFrom(email), emailVerified: true, _clock.UtcNow);
            _userRepository.Add(user);
        }

        if (!string.IsNullOrEmpty(_options.Password) && !user.HasPassword)
        {
            user.SetPassword(_passwordHasher.Hash(new Password(_options.Password)));
        }

        RoleAssignment.Assign(user, UserRole.Admin, adminCount);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.Information("Made user {UserId} the first admin", user.Id.Value);
    }

    private static PersonName NameFrom(EmailAddress email)
    {
        var localPart = email.Value[..email.Value.IndexOf('@')];
        return new PersonName(localPart.Length > 0 ? localPart : email.Value);
    }
}
