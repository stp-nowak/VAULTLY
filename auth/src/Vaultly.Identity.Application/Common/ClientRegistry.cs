using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Options;

using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Domain.Constants;
using Vaultly.Identity.Domain.Exceptions;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Application.Common;

/// <summary>
/// Registered OAuth clients and the checks the protocol needs (redirect URI allow-lists, secret verification).
/// </summary>
public interface IClientRegistry
{
    /// <summary>Finds a client by id, or <c>null</c> when it is unknown or the id is malformed.</summary>
    RegisteredClient? Find(string? clientId);

    /// <summary>Returns the client when id and secret are valid, otherwise <c>null</c>.</summary>
    RegisteredClient? Authenticate(string? clientId, string? clientSecret);
}

/// <summary>
/// A registered client with pre-parsed redirect URI rules.
/// </summary>
public sealed class RegisteredClient
{
    private readonly byte[] _secretHash;
    private readonly IReadOnlyList<RedirectUriPattern> _redirectUris;
    private readonly IReadOnlyList<RedirectUriPattern> _postLogoutRedirectUris;

    internal RegisteredClient(OAuthClientOptions options)
    {
        Id = new ClientId(options.ClientId);
        DisplayName = string.IsNullOrWhiteSpace(options.DisplayName) ? options.ClientId : options.DisplayName;

        if (Id == ConsoleClient.Id)
        {
            throw new InvalidOperationException($"Client id '{options.ClientId}' is reserved for the admin console.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            throw new InvalidOperationException($"Client '{options.ClientId}' has no secret configured.");
        }

        // Only the hash is kept in memory; comparisons are constant-time over equal-length digests.
        _secretHash = SHA256.HashData(Encoding.UTF8.GetBytes(options.ClientSecret));
        _redirectUris = ParsePatterns(options.ClientId, options.RedirectUris, "redirect URI");
        _postLogoutRedirectUris = ParsePatterns(options.ClientId, options.PostLogoutRedirectUris, "post-logout redirect URI");

        if (_redirectUris.Count == 0)
        {
            throw new InvalidOperationException($"Client '{options.ClientId}' has no redirect URIs configured.");
        }
    }

    public ClientId Id { get; }

    public string DisplayName { get; }

    /// <summary>Whether the redirect URI is registered for this client.</summary>
    public bool IsRedirectUriAllowed(RedirectUri redirectUri) => _redirectUris.Any(x => x.IsMatch(redirectUri.Value));

    /// <summary>Whether the post-logout redirect URI is registered for this client.</summary>
    public bool IsPostLogoutRedirectUriAllowed(RedirectUri redirectUri)
        => _postLogoutRedirectUris.Any(x => x.IsMatch(redirectUri.Value));

    /// <summary>Constant-time secret check.</summary>
    public bool VerifySecret(string? secret)
        => !string.IsNullOrEmpty(secret)
           && CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(secret)), _secretHash);

    private static List<RedirectUriPattern> ParsePatterns(string clientId, IEnumerable<string> values, string kind)
    {
        var patterns = new List<RedirectUriPattern>();
        foreach (var value in values.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            try
            {
                patterns.Add(RedirectUriPattern.Parse(value));
            }
            catch (InvalidValueException ex)
            {
                throw new InvalidOperationException($"Client '{clientId}' has an invalid {kind} '{value}'.", ex);
            }
        }

        return patterns;
    }
}

/// <inheritdoc />
public sealed class ClientRegistry : IClientRegistry
{
    private readonly IReadOnlyDictionary<string, RegisteredClient> _clients;

    /// <summary>
    /// Builds the registry from configuration and fails fast on invalid client definitions.
    /// </summary>
    public ClientRegistry(IOptions<IdentityOptions> options)
    {
        _clients = options.Value.Clients
            .Select(x => new RegisteredClient(x))
            .ToDictionary(x => x.Id.Value, StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public RegisteredClient? Find(string? clientId)
        => !string.IsNullOrWhiteSpace(clientId) && _clients.TryGetValue(clientId, out var client) ? client : null;

    /// <inheritdoc />
    public RegisteredClient? Authenticate(string? clientId, string? clientSecret)
        => Find(clientId) is { } client && client.VerifySecret(clientSecret) ? client : null;
}

internal sealed record RedirectUriPattern(string Value, bool IsWildcard)
{
    /// <summary>
    /// Parses a configured rule into a normalized matcher.
    /// </summary>
    public static RedirectUriPattern Parse(string value)
    {
        var wildcardIndex = value.IndexOf('*');
        if (wildcardIndex < 0)
        {
            return new RedirectUriPattern(new RedirectUri(value).Value, false);
        }

        if (wildcardIndex != value.Length - 1 || value.Count(static c => c == '*') != 1 || value.Length < 2 || value[^2] != '/')
        {
            throw new InvalidValueException("Wildcard redirect URI patterns must end with /*.");
        }

        if (!Uri.TryCreate(value[..^1], UriKind.Absolute, out var uri))
        {
            throw new InvalidValueException("Wildcard redirect URI patterns must start with an absolute URI.");
        }

        return new RedirectUriPattern(uri.AbsoluteUri, true);
    }

    /// <summary>
    /// Determines whether a normalized redirect URI satisfies this rule.
    /// </summary>
    public bool IsMatch(string redirectUri)
        => IsWildcard
            ? redirectUri.StartsWith(Value, StringComparison.OrdinalIgnoreCase)
            : string.Equals(redirectUri, Value, StringComparison.OrdinalIgnoreCase);
}
