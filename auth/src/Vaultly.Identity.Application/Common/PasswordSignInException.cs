namespace Vaultly.Identity.Application.Common;

public enum PasswordSignInFailure
{
    /// <summary>Wrong email or password; never tells which, nor whether the account exists.</summary>
    InvalidCredentials,
    EmailTaken,
}

/// <summary>An email and password sign-in or registration was refused.</summary>
public sealed class PasswordSignInException(PasswordSignInFailure failure, string message) : Exception(message)
{
    public PasswordSignInFailure Failure { get; } = failure;

    public static PasswordSignInException WrongCredentials()
        => new(PasswordSignInFailure.InvalidCredentials, "Email or password is incorrect.");

    public static PasswordSignInException EmailAlreadyUsed()
        => new(PasswordSignInFailure.EmailTaken, "An account with this email already exists.");
}
