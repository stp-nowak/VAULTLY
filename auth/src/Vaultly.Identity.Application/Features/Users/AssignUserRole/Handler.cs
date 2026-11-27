using Serilog;

using Vaultly.Core;
using Vaultly.Core.Messaging;
using Vaultly.Identity.Domain.Users;

namespace Vaultly.Identity.Application.Features.Users.AssignUserRole;

public sealed class AssignUserRoleHandler(IUserRepository userRepository, IUnitOfWork unitOfWork, ILogger logger)
    : ICommandHandler<AssignUserRoleCommand, bool>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger _logger = logger.ForContext<AssignUserRoleHandler>();

    public async Task<bool> Handle(AssignUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(UserId.From(request.UserId), cancellationToken);
        if (user is null)
        {
            return false;
        }

        var adminCount = await _userRepository.CountAdminsAsync(cancellationToken);
        RoleAssignment.Assign(user, request.Role, adminCount);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.Information(
            "Admin {AdminUserId} set the role of user {UserId} to {Role}",
            request.AdminUserId,
            user.Id.Value,
            request.Role);

        return true;
    }
}
