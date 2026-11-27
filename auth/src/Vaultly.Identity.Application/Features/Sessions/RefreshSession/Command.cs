using Vaultly.Identity.Application.Messaging;
using Vaultly.Identity.Application.Models;

namespace Vaultly.Identity.Application.Features.Sessions.RefreshSession;

/// <summary>
/// <c>grant_type=refresh_token</c>: rotates a refresh token (single use) for an authenticated client.
/// </summary>
public sealed record RefreshSessionCommand(string? RefreshToken, string? ClientId, string? ClientSecret) : ICommand<TokenResult>;
