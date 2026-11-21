using System.Security.Cryptography;
using System.Text;

using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Application.Common;

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
    /// Builds a same-origin relative URL with one query parameter (e.g. <c>/google/authorize?request=…</c>).
    /// </summary>
    public static string BuildRelativeUrl(string path, string key, string value)
        => $"{path}?{UrlEncode(key)}={UrlEncode(value)}";

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

    private static string UrlEncode(string value) => Uri.EscapeDataString(value);

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
