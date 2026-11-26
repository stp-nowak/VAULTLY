namespace Vaultly.Identity.Api;

/// <summary>Why the login UI or admin console API refused a call; sent as <c>{ "error": "snake_case" }</c>.</summary>
public enum ApiError
{
    CsrfHeaderMissing,
    InvalidCredentials,
    EmailTaken,
    WeakPassword,
    InvalidInput,
    RequestExpired,
    SignInMethodDisabled,
    LastAdmin,
    LastSignInMethod,
}

/// <summary>Body of an API error response.</summary>
public sealed record ApiErrorResponse(ApiError Error);
