using Vaultly.SharedKernel;

namespace Vaultly.Identity.Domain.Exceptions;

public sealed class InvalidValueException : DomainException
{
    /// <summary>
    /// Creates a new invalid value exception with the provided message.
    /// </summary>
    public InvalidValueException(string message) : base(message)
    {
    }
}