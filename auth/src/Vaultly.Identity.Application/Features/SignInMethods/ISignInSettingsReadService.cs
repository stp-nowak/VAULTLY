namespace Vaultly.Identity.Application.Features.SignInMethods;

/// <summary>
/// Read side (CQRS) for the sign-in settings.
/// </summary>
public interface ISignInSettingsReadService
{
    /// <summary>Values of the sign-in methods admins switched off (e.g. <c>google</c>); empty when none.</summary>
    Task<IReadOnlySet<string>> GetDisabledMethodsAsync(CancellationToken cancellationToken);
}
