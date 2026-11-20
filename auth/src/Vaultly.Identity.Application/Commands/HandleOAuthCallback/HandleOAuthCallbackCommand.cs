using Vaultly.Identity.Application.Messaging;
using Vaultly.Identity.Application.Models;

namespace Vaultly.Identity.Application.Commands.HandleOAuthCallback;

/// <summary>
/// Command to handle an OAuth callback.
/// </summary>
public sealed record HandleOAuthCallbackCommand(
    string ProviderId,
    string Code,
    string State,
    string? UserAgent,
    string? IpAddress) : ICommand<AuthCallbackResult>;
