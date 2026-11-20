using Vaultly.Identity.Application.Messaging;
using Vaultly.Identity.Application.Models;

namespace Vaultly.Identity.Application.Commands.RefreshSession;

/// <summary>
/// Command to refresh a session using a refresh token.
/// </summary>
public sealed record RefreshSessionCommand(string RefreshToken) : ICommand<RefreshResult>;
