using Vaultly.Core;

namespace Vaultly.Identity.Domain.SignInMethods;

/// <summary>
/// Thrown when switching a method off would leave no available way to sign in.
/// </summary>
public sealed class LastSignInMethodException() : DomainException("At least one sign-in method must stay enabled.");
