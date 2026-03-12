using System.Security.Cryptography;
using System.Text;

using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Domain.Exceptions;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Application.UseCases.OAuth;

/// <summary>
/// Shared OAuth workflow helpers used by authorization-related use cases.
/// </summary>
internal static class OAuthFlowUtilities
{
    /// <summary>
    /// Resolves a configured OAuth provider by identifier.
    /// </summary>
    public static IExternalOAuthProvider GetProvider(
        IReadOnlyDictionary<ProviderId, IExternalOAuthProvider> providers,
        ProviderId providerId)
    {
        if (!providers.TryGetValue(providerId, out var provider))
        {
            throw new InvalidOperationException("OAuth provider is not configured.");
        }

        return provider;
    }

    /// <summary>
    /// Validates a caller-provided frontend redirect URI against the configured allow-list.
    /// </summary>
    public static RedirectUri ValidateRedirectUri(string? redirectUri, IdentityOptions identityOptions)
    {
        var normalizedRedirectUri = new RedirectUri(redirectUri ?? string.Empty);
        var allowedRedirectUriPatterns = GetAllowedRedirectUriPatterns(identityOptions);
        if (!allowedRedirectUriPatterns.Any(pattern => pattern.IsMatch(normalizedRedirectUri.Value)))
        {
            throw new InvalidValueException("Redirect URI is not allowed.");
        }

        return normalizedRedirectUri;
    }

    /// <summary>
    /// Builds the frontend redirect URL that completes the OAuth flow.
    /// </summary>
    public static string BuildFrontendRedirectUrl(string authCode, RedirectUri redirectUri)
    {
        var query = new Dictionary<string, string?>
        {
            ["code"] = authCode
        };

        return BuildUrl(redirectUri.Value, query);
    }

    /// <summary>
    /// Creates an RFC 7636 PKCE code challenge from the provided verifier.
    /// </summary>
    public static string CreateCodeChallenge(string codeVerifier)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
        return Base64UrlEncode(bytes);
    }

    /// <summary>
    /// Truncates optional client metadata so it fits persistence limits safely.
    /// </summary>
    public static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }

    /// <summary>
    /// Builds an absolute URL and merges the provided query string values.
    /// </summary>
    public static string BuildUrl(string baseUrl, IReadOnlyDictionary<string, string?> queryParams)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException("Endpoint URL is invalid.");
        }

        var existing = ParseQuery(baseUri.Query);
        foreach (var (key, value) in queryParams)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            existing[key] = value;
        }

        var query = string.Join("&", existing.Select(x => $"{UrlEncode(x.Key)}={UrlEncode(x.Value)}"));
        var builder = new UriBuilder(baseUri) { Query = query };
        return builder.Uri.ToString();
    }

    /// <summary>
    /// Parses configured redirect URI allow-list entries into validated match patterns.
    /// </summary>
    private static List<RedirectUriPattern> GetAllowedRedirectUriPatterns(IdentityOptions identityOptions)
    {
        if (identityOptions.AllowedRedirectUris.Count == 0)
        {
            throw new InvalidOperationException("Allowed redirect URIs are not configured.");
        }

        var allowedRedirectUriPatterns = new List<RedirectUriPattern>();
        foreach (var value in identityOptions.AllowedRedirectUris.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            try
            {
                allowedRedirectUriPatterns.Add(RedirectUriPattern.Parse(value));
            }
            catch (InvalidValueException ex)
            {
                throw new InvalidOperationException("Configured allowed redirect URI is invalid.", ex);
            }
        }

        if (allowedRedirectUriPatterns.Count == 0)
        {
            throw new InvalidOperationException("Allowed redirect URIs are not configured.");
        }

        return allowedRedirectUriPatterns;
    }

    /// <summary>
    /// Parses a raw query string into a mutable dictionary.
    /// </summary>
    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(query))
        {
            return result;
        }

        var trimmed = query.TrimStart('?');
        foreach (var pair in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 0)
            {
                continue;
            }

            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            if (!string.IsNullOrWhiteSpace(key))
            {
                result[key] = value;
            }
        }

        return result;
    }

    /// <summary>
    /// URL-encodes a query-string component.
    /// </summary>
    private static string UrlEncode(string value) => Uri.EscapeDataString(value);

    /// <summary>
    /// Represents a configured redirect URI rule using exact or prefix matching.
    /// </summary>
    private sealed record RedirectUriPattern(string Value, bool IsWildcard)
    {
        /// <summary>
        /// Parses a configured redirect URI rule into a normalized matcher.
        /// </summary>
        public static RedirectUriPattern Parse(string value)
        {
            var wildcardIndex = value.IndexOf('*');
            if (wildcardIndex < 0)
            {
                return new RedirectUriPattern(new RedirectUri(value).Value, false);
            }

            if (wildcardIndex != value.Length - 1 || value.Count(static character => character == '*') != 1 || value.Length < 2 || value[^2] != '/')
            {
                throw new InvalidValueException("Wildcard redirect URI patterns must end with /*.");
            }

            var wildcardPrefix = value[..^1];
            if (!Uri.TryCreate(wildcardPrefix, UriKind.Absolute, out var uri))
            {
                throw new InvalidValueException("Wildcard redirect URI patterns must start with an absolute URI.");
            }

            return new RedirectUriPattern(uri.AbsoluteUri, true);
        }

        /// <summary>
        /// Determines whether the provided redirect URI satisfies this rule.
        /// </summary>
        public bool IsMatch(string redirectUri)
            => IsWildcard
                ? redirectUri.StartsWith(Value, StringComparison.OrdinalIgnoreCase)
                : string.Equals(redirectUri, Value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Converts raw bytes to base64url without padding.
    /// </summary>
    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
