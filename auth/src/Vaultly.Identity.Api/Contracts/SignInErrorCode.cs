namespace Vaultly.Identity.Api.Contracts;

/// <summary>Why a sign-in ended on the login UI's error page (<c>/error?code=snake_case</c>).</summary>
public enum SignInErrorCode
{
    InvalidRequest,
    AccessDenied,
    LoginFailed,
    RequestExpired,
}
