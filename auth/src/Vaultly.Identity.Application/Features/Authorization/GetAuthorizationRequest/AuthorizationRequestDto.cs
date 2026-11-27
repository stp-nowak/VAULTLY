namespace Vaultly.Identity.Application.Features.Authorization.GetAuthorizationRequest;

/// <summary>
/// What the login UI needs to render a sign-in page for an authorization request.
/// </summary>
/// <param name="PasswordSignIn">Whether to offer email and password sign-in and registration (only while pending).</param>
public sealed record AuthorizationRequestDto(
    Guid Id,
    string ClientName,
    AuthorizationRequestStatus Status,
    IReadOnlyList<LoginProviderDto> Providers,
    bool PasswordSignIn);

/// <summary>
/// A sign-in option; <paramref name="StartUrl"/> is a same-origin path the browser navigates to.
/// </summary>
public sealed record LoginProviderDto(string Id, string Name, string StartUrl);
