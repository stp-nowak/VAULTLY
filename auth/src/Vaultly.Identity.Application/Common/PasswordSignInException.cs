namespace Vaultly.Identity.Application.Common;

/// <summary>
/// An email and password sign-in or registration was refused. <see cref="Code"/> is safe to show the login UI; it never
/// tells whether an account exists for a failed sign-in.
/// </summary>
public sealed class PasswordSignInException(string code, string message) : Exception(message)
{
    public const string InvalidCredentials = "invalid_credentials";
    public const string EmailTaken = "email_taken";

    public string Code { get; } = code;

    public static PasswordSignInException WrongCredentials() => new(InvalidCredentials, "Email or password is incorrect.");

    public static PasswordSignInException EmailAlreadyUsed() => new(EmailTaken, "An account with this email already exists.");
}
