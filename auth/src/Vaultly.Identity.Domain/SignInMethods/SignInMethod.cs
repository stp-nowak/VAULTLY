namespace Vaultly.Identity.Domain.SignInMethods;

/// <summary>
/// A way to sign in: an external provider (its <see cref="ProviderId"/>, e.g. <c>google</c>) or email and password.
/// </summary>
public sealed record SignInMethod
{
    public static readonly SignInMethod Password = new("password");

    public SignInMethod(string value)
    {
        Value = new ProviderId(value).Value;
    }

    public string Value { get; }

    public static SignInMethod Of(ProviderId provider) => new(provider.Value);

    public override string ToString() => Value;
}
