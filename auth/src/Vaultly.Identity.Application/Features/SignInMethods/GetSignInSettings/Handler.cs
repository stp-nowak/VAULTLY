using Vaultly.Identity.Application.Common;
using Vaultly.Identity.Application.Interfaces.ReadServices;
using Vaultly.Identity.Application.Messaging;

namespace Vaultly.Identity.Application.Features.SignInMethods.GetSignInSettings;

public sealed class GetSignInSettingsHandler(SignInMethodCatalog catalog, ISignInSettingsReadService readService)
    : IQueryHandler<GetSignInSettingsQuery, IReadOnlyList<SignInMethodDto>>
{
    private readonly SignInMethodCatalog _catalog = catalog;
    private readonly ISignInSettingsReadService _readService = readService;

    public async Task<IReadOnlyList<SignInMethodDto>> Handle(GetSignInSettingsQuery request, CancellationToken cancellationToken)
    {
        var disabled = await _readService.GetDisabledMethodsAsync(cancellationToken);

        return _catalog.All
            .Select(x => new SignInMethodDto(x.Method.Value, x.Name, x.IsConfigured, !disabled.Contains(x.Method.Value)))
            .ToList();
    }
}
