using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Application.Common;

/// <summary>A sign-in method this service knows and whether this deployment is set up for it.</summary>
public sealed record SignInMethodInfo(SignInMethod Method, string Name, bool IsConfigured);

/// <summary>
/// Every sign-in method the service knows: the external providers and email + password. Whether a method may be used
/// also depends on <see cref="Domain.Aggregates.SignInSettings"/>.
/// </summary>
public sealed class SignInMethodCatalog(IEnumerable<IExternalOAuthProvider> providers)
{
    // Built in: needs no deployment configuration.
    private static readonly SignInMethodInfo Password = new(SignInMethod.Password, "Email and password", IsConfigured: true);

    public IReadOnlyList<SignInMethodInfo> All { get; } =
        [.. providers.Select(x => new SignInMethodInfo(SignInMethod.Of(x.Id), x.Name, x.IsConfigured)), Password];

    public IReadOnlyCollection<SignInMethod> Configured => All.Where(x => x.IsConfigured).Select(x => x.Method).ToList();

    public SignInMethodInfo? Find(string? method)
        => All.FirstOrDefault(x => string.Equals(x.Method.Value, method?.Trim(), StringComparison.OrdinalIgnoreCase));
}
