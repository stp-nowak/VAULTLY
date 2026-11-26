using Vaultly.Identity.Application.Messaging;
using Vaultly.Identity.Application.Models;

namespace Vaultly.Identity.Application.Queries.GetConsoleUser;

/// <summary>
/// Who is signed in to the admin console, by the browser's SSO cookie; <c>null</c> when nobody is.
/// </summary>
public sealed record GetConsoleUserQuery(string? SsoToken) : IQuery<ConsoleUserDto?>;
