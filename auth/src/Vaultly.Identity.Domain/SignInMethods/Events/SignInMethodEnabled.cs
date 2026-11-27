using Vaultly.Core;

namespace Vaultly.Identity.Domain.SignInMethods.Events;

/// <summary>
/// Raised when an admin switches a sign-in method on.
/// </summary>
public sealed record SignInMethodEnabled(SignInMethod Method) : DomainEvent;
