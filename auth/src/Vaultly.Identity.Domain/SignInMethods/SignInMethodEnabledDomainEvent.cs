using Vaultly.Core;

namespace Vaultly.Identity.Domain.SignInMethods;

/// <summary>
/// Raised when an admin switches a sign-in method on.
/// </summary>
public sealed record SignInMethodEnabledDomainEvent(SignInMethod Method) : DomainEvent;
