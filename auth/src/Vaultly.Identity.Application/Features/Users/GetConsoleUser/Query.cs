using Vaultly.Identity.Application.Messaging;

namespace Vaultly.Identity.Application.Features.Users.GetConsoleUser;

/// <summary>
/// Who is signed in to the admin console, by the browser's SSO cookie; <c>null</c> when nobody is.
/// </summary>
public sealed record GetConsoleUserQuery(string? SsoToken) : IQuery<ConsoleUserDto?>;
