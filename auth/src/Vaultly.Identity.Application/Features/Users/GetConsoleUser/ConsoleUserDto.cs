using Vaultly.Identity.PublicTypes;

namespace Vaultly.Identity.Application.Features.Users.GetConsoleUser;

/// <summary>
/// The user signed in through an active SSO session, as the admin console sees them.
/// </summary>
public sealed record ConsoleUserDto(Guid Id, string Email, string Name, UserRole Role);
