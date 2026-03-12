namespace Vaultly.SharedKernel;

public abstract class DomainException : Exception
{
    /// <summary>
    /// Creates a new domain exception with the provided message.
    /// </summary>
    protected DomainException(string message) : base(message)
    {
    }
}