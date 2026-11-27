namespace Vaultly.Identity.Domain.SignInMethods;

public interface ISignInSettingsRepository
{
    /// <summary>
    /// Gets the stored settings, or <c>null</c> while no admin has changed them (then every method is enabled, see
    /// <see cref="SignInSettings.CreateDefault"/>).
    /// </summary>
    Task<SignInSettings?> FindAsync(CancellationToken cancellationToken);

    void Add(SignInSettings settings);
}
