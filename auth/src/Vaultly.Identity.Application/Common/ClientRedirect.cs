using Vaultly.Identity.Domain;
using Vaultly.Identity.Domain.AuthorizationRequests;

namespace Vaultly.Identity.Application.Common;

internal static class ClientRedirect
{
    /// <summary>
    /// Parses a caller-supplied redirect URI, returning <c>null</c> instead of throwing for malformed input.
    /// </summary>
    public static RedirectUri? TryParse(string? value)
    {
        try
        {
            return string.IsNullOrWhiteSpace(value) ? null : new RedirectUri(value);
        }
        catch (InvalidValueException)
        {
            return null;
        }
    }

    /// <summary>
    /// Successful authorization response: <c>redirect_uri?code=…&amp;state=…</c>.
    /// </summary>
    public static string WithCode(RedirectUri redirectUri, string code, ClientState state)
        => OAuthFlowUtilities.BuildUrl(redirectUri.Value, new Dictionary<string, string?>
        {
            ["code"] = code,
            ["state"] = state.Value,
        });
}
