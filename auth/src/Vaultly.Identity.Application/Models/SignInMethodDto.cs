namespace Vaultly.Identity.Application.Models;

/// <summary>
/// A sign-in method as the admin console shows it.
/// </summary>
/// <param name="IsConfigured">Whether this deployment is set up for it; an unconfigured method cannot be used even if enabled.</param>
/// <param name="IsEnabled">Whether admins allow it.</param>
public sealed record SignInMethodDto(string Id, string Name, bool IsConfigured, bool IsEnabled);
