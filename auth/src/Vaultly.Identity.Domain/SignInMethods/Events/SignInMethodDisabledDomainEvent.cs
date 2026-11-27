using Vaultly.Core;

namespace Vaultly.Identity.Domain.SignInMethods.Events;

/// <summary>
/// Raised when an admin switches a sign-in method off.
/// </summary>
public sealed record SignInMethodDisabledDomainEvent(SignInMethod Method) : DomainEvent;
