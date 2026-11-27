using Vaultly.Core.Messaging;
using Vaultly.Identity.Application.Models;

namespace Vaultly.Identity.Application.Features.Authorization.ExchangeAuthCode;

/// <summary>
/// <c>grant_type=authorization_code</c>: exchanges a code for tokens. The client authenticates with its secret and
/// proves possession of the PKCE verifier; the redirect URI must equal the one used at <c>/authorize</c>.
/// </summary>
public sealed record ExchangeAuthCodeCommand(
    string? Code,
    string? ClientId,
    string? ClientSecret,
    string? RedirectUri,
    string? CodeVerifier) : ICommand<TokenResult>;
