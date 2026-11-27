using Serilog;

using Vaultly.Core;
using Vaultly.Core.Messaging;
using Vaultly.Identity.Application.Common;
using Vaultly.Identity.Application.Interfaces.Repositories;
using Vaultly.Identity.Domain.Aggregates;

namespace Vaultly.Identity.Application.Features.SignInMethods.SetSignInMethodEnabled;

public sealed class SetSignInMethodEnabledHandler(
    SignInMethodCatalog catalog,
    ISignInSettingsRepository settingsRepository,
    IUnitOfWork unitOfWork,
    ILogger logger) : ICommandHandler<SetSignInMethodEnabledCommand, bool>
{
    private readonly SignInMethodCatalog _catalog = catalog;
    private readonly ISignInSettingsRepository _settingsRepository = settingsRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger _logger = logger.ForContext<SetSignInMethodEnabledHandler>();

    public async Task<bool> Handle(SetSignInMethodEnabledCommand request, CancellationToken cancellationToken)
    {
        var method = _catalog.Find(request.Method)?.Method;
        if (method is null)
        {
            return false;
        }

        var settings = await _settingsRepository.FindAsync(cancellationToken);
        if (settings is null)
        {
            settings = SignInSettings.CreateDefault();
            _settingsRepository.Add(settings);
        }

        if (request.Enabled)
        {
            settings.Enable(method);
        }
        else
        {
            settings.Disable(method, _catalog.Configured);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.Information(
            "Admin {AdminUserId} {Action} sign-in method {Method}",
            request.AdminUserId,
            request.Enabled ? "enabled" : "disabled",
            method.Value);

        return true;
    }
}
