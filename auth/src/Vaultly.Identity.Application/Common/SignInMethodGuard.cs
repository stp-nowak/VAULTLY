using Vaultly.Identity.Application.Features.ExternalLogin;
using Vaultly.Identity.Domain.SignInMethods;

namespace Vaultly.Identity.Application.Common;

internal static class SignInMethodGuard
{
    /// <summary>
    /// Throws unless the provider is configured for this deployment and admins have not switched it off. Checked when
    /// a provider login starts and again on its callback, so a login begun before the switch cannot finish.
    /// </summary>
    public static Task EnsureUsableAsync(
        IExternalOAuthProvider provider,
        ISignInSettingsRepository settingsRepository,
        CancellationToken cancellationToken)
        => EnsureUsableAsync(SignInMethod.Of(provider.Id), provider.IsConfigured, settingsRepository, cancellationToken);

    /// <summary>Throws unless admins have not switched email and password sign-in off.</summary>
    public static Task EnsurePasswordUsableAsync(ISignInSettingsRepository settingsRepository, CancellationToken cancellationToken)
        => EnsureUsableAsync(SignInMethod.Password, isConfigured: true, settingsRepository, cancellationToken);

    private static async Task EnsureUsableAsync(
        SignInMethod method,
        bool isConfigured,
        ISignInSettingsRepository settingsRepository,
        CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.FindAsync(cancellationToken) ?? SignInSettings.CreateDefault();
        if (!isConfigured || !settings.IsEnabled(method))
        {
            throw new InvalidOperationException("Sign-in method is disabled.");
        }
    }
}
