using Vaultly.Identity.Application.Messaging;

namespace Vaultly.Identity.Application.Commands.RevokeSession;

/// <summary>
/// Command to revoke a session for a user.
/// </summary>
public sealed record RevokeSessionCommand(Guid UserId, Guid SessionId) : ICommand<bool>;
