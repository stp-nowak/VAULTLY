using Vaultly.Identity.SharedKernel;

using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Domain.Exceptions;

/// <summary>Thrown when a new password does not meet the password policy.</summary>
public sealed class WeakPasswordException()
    : DomainException($"Password must be {Password.MinLength} to {Password.MaxLength} characters.");
