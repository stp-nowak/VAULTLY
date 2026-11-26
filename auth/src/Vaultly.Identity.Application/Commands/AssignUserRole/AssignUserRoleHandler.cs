using Serilog;

using Vaultly.Identity.Application.Interfaces.Repositories;
using Vaultly.Identity.Application.Messaging;
using Vaultly.Identity.Domain.Services;
using Vaultly.Identity.Domain.ValueObjects;
using Vaultly.Identity.SharedKernel;

namespace Vaultly.Identity.Application.Commands.AssignUserRole;

public sealed class AssignUserRoleHandler(IUserRepository userRepository, IUnitOfWork unitOfWork, ILogger logger)
    : ICommandHandler<AssignUserRoleCommand, bool>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger _logger = logger.ForContext<AssignUserRoleHandler>();

    public async Task<bool> Handle(AssignUserRoleCommand request, CancellationToken cancellationToken)
    {
        var role = UserRole.Parse(request.Role);
        var user = await _userRepository.GetByIdAsync(UserId.From(request.UserId), cancellationToken);
        if (user is null)
        {
            return false;
        }

        var adminCount = await _userRepository.CountAdminsAsync(cancellationToken);
        RoleAssignment.Assign(user, role, adminCount);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.Information(
            "Admin {AdminUserId} set the role of user {UserId} to {Role}",
            request.AdminUserId,
            user.Id.Value,
            role.Value);

        return true;
    }
}
