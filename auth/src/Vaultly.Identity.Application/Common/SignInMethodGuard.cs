using Vaultly.Identity.Application.Interfaces.Repositories;
using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Application.Common;

internal static class SignInMethodGuard
{
    /// <summary>
    /// Throws unless the provider is configured for this deployment and admins have not switched it off. Checked when
    /// a provider login starts and again on its callback, so a login begun before the switch cannot finish.
    /// </summary>
    public static async Task EnsureUsableAsync(
        IExternalOAuthProvider provider,
        ISignInSettingsRepository settingsRepository,
        CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.FindAsync(cancellationToken) ?? SignInSettings.CreateDefault();
        if (!provider.IsConfigured || !settings.IsEnabled(SignInMethod.Of(provider.Id)))
        {
            throw new InvalidOperationException("Sign-in method is disabled.");
        }
    }
}
