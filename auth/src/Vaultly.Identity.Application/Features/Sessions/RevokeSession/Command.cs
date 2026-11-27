using Vaultly.Core.Messaging;

namespace Vaultly.Identity.Application.Features.Sessions.RevokeSession;

/// <summary>
/// Command to revoke a session for a user.
/// </summary>
public sealed record RevokeSessionCommand(Guid UserId, Guid SessionId) : ICommand<bool>;
