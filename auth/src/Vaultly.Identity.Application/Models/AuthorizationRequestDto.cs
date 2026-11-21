namespace Vaultly.Identity.Application.Models;

/// <summary>
/// What the login UI needs to render a sign-in page for an authorization request.
/// </summary>
/// <param name="Status"><c>pending</c>, <c>expired</c> or <c>completed</c>.</param>
public sealed record AuthorizationRequestDto(
    Guid Id,
    string ClientName,
    string Status,
    IReadOnlyList<LoginProviderDto> Providers);

/// <summary>
/// A sign-in option; <paramref name="StartUrl"/> is a same-origin path the browser navigates to.
/// </summary>
public sealed record LoginProviderDto(string Id, string Name, string StartUrl);
