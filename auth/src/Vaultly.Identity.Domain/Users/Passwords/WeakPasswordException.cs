using Vaultly.Core;

namespace Vaultly.Identity.Domain.Users.Passwords;

/// <summary>Thrown when a new password does not meet the password policy.</summary>
public sealed class WeakPasswordException()
    : DomainException($"Password must be {Password.MinLength} to {Password.MaxLength} characters.");
