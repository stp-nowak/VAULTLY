using Vaultly.Core;

namespace Vaultly.Identity.Domain.SignInMethods.Events;

/// <summary>
/// Raised when an admin switches a sign-in method off.
/// </summary>
public sealed record SignInMethodDisabled(SignInMethod Method) : DomainEvent;
