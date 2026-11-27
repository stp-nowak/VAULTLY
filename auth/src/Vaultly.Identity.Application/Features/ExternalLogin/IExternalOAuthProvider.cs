using Vaultly.Identity.Domain.SignInMethods;

namespace Vaultly.Identity.Application.Features.ExternalLogin;

public interface IExternalOAuthProvider
{
    ProviderId Id { get; }
    string Name { get; }
    /// <summary>Whether this deployment is set up for the provider (enabled in configuration, with a client id).</summary>
    bool IsConfigured { get; }
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
