using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Application.Security;
using Vaultly.Identity.Domain.AuthorizationRequests;
using Vaultly.Identity.Domain.AuthorizationRequests.Clients;
using Vaultly.Identity.Domain.Sessions;
using Vaultly.Identity.Domain.Sessions.AuthCodes;

namespace Vaultly.Identity.Application.Common;

internal static class SignInCompletion
{
    /// <summary>
    /// Completes a pending authorization request once the user has a signed-in session and returns where the browser
    /// goes next: a client gets an authorization code bound to its redirect URI and PKCE challenge, the admin console
    /// just its URL.
    /// </summary>
    public static string Complete(
        AuthorizationRequest request,
        Session session,
        ITokenGenerator tokenGenerator,
        ITokenHasher tokenHasher,
        IdentityOptions options,
        DateTimeOffset now)
    {
        var redirectUrl = request is { State: { } state, CodeChallenge: { } codeChallenge }
            ? IssueCode(request, state, codeChallenge, session, tokenGenerator, tokenHasher, options, now)
            : request.RedirectUri.Value;

        request.Complete(now);
        return redirectUrl;
    }

    private static string IssueCode(
        AuthorizationRequest request,
        ClientState state,
        CodeChallenge codeChallenge,
        Session session,
        ITokenGenerator tokenGenerator,
        ITokenHasher tokenHasher,
        IdentityOptions options,
        DateTimeOffset now)
    {
        var code = tokenGenerator.GenerateToken(32);
        session.IssueAuthCode(
            new CodeHash(tokenHasher.HashToken(code)),
            now.AddMinutes(options.AuthCodeTtlMinutes),
            request.ClientId,
            request.RedirectUri,
            codeChallenge);

        return ClientRedirect.WithCode(request.RedirectUri, code, state);
    }
}
