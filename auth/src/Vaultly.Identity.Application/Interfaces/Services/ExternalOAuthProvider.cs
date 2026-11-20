using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Application.Interfaces.Services;

public interface IExternalOAuthProvider
{
    ProviderId Id { get; }
    string Name { get; }
    bool IsEnabled { get; }
    string AuthorizationEndpoint { get; }
    string ClientId { get; }
    string RedirectUri { get; }
    IReadOnlyList<string> Scopes { get; }

    /// <summary>
    /// Exchanges an authorization code for external identity information.
    /// </summary>
    Task<ExternalIdentityInfo> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken cancellationToken);
}

public sealed record ExternalIdentityInfo(
    string Subject,
    string Email,
    bool EmailVerified,
    string Name,
    string? Nonce);