using Vaultly.Identity.Application.Messaging;

namespace Vaultly.Identity.Application.Features.Sessions.RevokeToken;

/// <summary>
/// RFC 7009 token revocation for a refresh token. Revoking it ends the whole session (and its SSO), which is what
/// "sign out" in a client means.
/// </summary>
public sealed record RevokeTokenCommand(string? Token, string? ClientId, string? ClientSecret) : ICommand;
