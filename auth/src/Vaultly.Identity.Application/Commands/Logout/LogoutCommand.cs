using Vaultly.Identity.Application.Messaging;

namespace Vaultly.Identity.Application.Commands.Logout;

/// <summary>
/// Command to log out a session by refresh token.
/// </summary>
public sealed record LogoutCommand(string? RefreshToken) : ICommand;
