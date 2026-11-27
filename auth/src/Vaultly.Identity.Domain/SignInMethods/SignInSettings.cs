using Vaultly.Core;
using Vaultly.Identity.Domain.SignInMethods.Events;

namespace Vaultly.Identity.Domain.SignInMethods;

/// <summary>
/// Which sign-in methods admins have switched off. A method is enabled unless listed here, so a newly configured
/// provider works without extra setup. There is a single instance for the whole service.
/// </summary>
public sealed class SignInSettings : AggregateRoot
{
    public const int SingletonId = 1;

    private SignInSettings()
    {
    }

    public int Id { get; private set; }

    private List<string> DisabledMethodValues { get; set; } = [];

    public IReadOnlyCollection<SignInMethod> DisabledMethods => DisabledMethodValues.Select(x => new SignInMethod(x)).ToList();

    /// <summary>Settings before any admin changed them: every method enabled.</summary>
    public static SignInSettings CreateDefault() => new() { Id = SingletonId };

    public bool IsEnabled(SignInMethod method) => !DisabledMethodValues.Contains(method.Value);

    public void Enable(SignInMethod method)
    {
        if (DisabledMethodValues.Remove(method.Value))
        {
            RaiseDomainEvent(new SignInMethodEnabledDomainEvent(method));
        }
    }

    /// <param name="method">Method to switch off.</param>
    /// <param name="availableMethods">Methods this deployment is configured for.</param>
    /// <exception cref="LastSignInMethodException">
    /// <paramref name="method"/> is the last available method that is still enabled, so nobody could sign in.
    /// </exception>
    public void Disable(SignInMethod method, IReadOnlyCollection<SignInMethod> availableMethods)
    {
        if (!IsEnabled(method))
        {
            return;
        }

        if (availableMethods.Contains(method) && !availableMethods.Any(other => other != method && IsEnabled(other)))
        {
            throw new LastSignInMethodException();
        }

        DisabledMethodValues = [.. DisabledMethodValues, method.Value];
        RaiseDomainEvent(new SignInMethodDisabledDomainEvent(method));
    }
}
