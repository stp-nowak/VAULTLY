using Vaultly.Core.Messaging;
using Vaultly.Identity.Application.Models;

namespace Vaultly.Identity.Application.Features.ExternalLogin.HandleOAuthCallback;

/// <summary>
/// Command to handle an OAuth callback.
/// </summary>
public sealed record HandleOAuthCallbackCommand(
    string ProviderId,
    string Code,
    string State,
    string? UserAgent,
    string? IpAddress) : ICommand<AuthCallbackResult>;
