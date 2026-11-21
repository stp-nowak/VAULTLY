# Vaultly Identity — client integration

Vaultly Identity is an OAuth 2.0 / OpenID Connect authorization server with its own login UI. Applications never
show a sign-in form: they redirect the browser to the Identity service and receive an authorization code back.

## Clients

Every application is a registered **confidential client** (`Identity:Clients`): client id, client secret, allowed
redirect URIs and post-logout redirect URIs. The web app's server side (`web/src/api`) is the `vaultly-web` client.
Browsers never receive tokens.

Discovery: `GET /.well-known/openid-configuration` (endpoints below), `GET /.well-known/jwks.json` (keys to validate
access tokens).

## Sign-in (authorization code + PKCE)

1. The client creates `state` and a PKCE `code_verifier`, keeps them server-side (e.g. an encrypted cookie) and
   redirects the browser to
   `GET /authorize?response_type=code&client_id=…&redirect_uri=…&state=…&code_challenge=…&code_challenge_method=S256`.
2. If the browser has an active SSO session (`vaultly_sso` cookie on the Identity domain), the user is sent straight
   back. Otherwise the Identity login UI (`/login`) lets the user pick a provider (Google) and signs them in.
3. The browser returns to `redirect_uri?code=…&state=…`. The client checks `state` against what it stored.
   Errors come back as `redirect_uri?error=…&state=…`; an unknown client or unregistered `redirect_uri` is never
   redirected (the Identity error page is shown instead).
4. The client exchanges the code server-to-server:

   ```http
   POST /token
   Content-Type: application/x-www-form-urlencoded

   grant_type=authorization_code&code=…&code_verifier=…&redirect_uri=…&client_id=…&client_secret=…
   ```

   Response: `access_token`, `token_type`, `expires_in`, `refresh_token`, `refresh_expires_in`, `session_id`, `user`.
   A code is single-use and bound to the client, redirect URI and PKCE challenge; a failed attempt burns it.

## Refresh and revocation

- `POST /token` with `grant_type=refresh_token&refresh_token=…` + client credentials rotates the refresh token
  (single use; reuse of an old token revokes the whole session). A rejected refresh returns `400 invalid_grant`.
- `POST /revoke` with `token=<refresh token>` + client credentials ends the session (always `200`).

## Sign-out

After revoking its tokens, the client sends the browser to
`GET /logout?client_id=…&post_logout_redirect_uri=…` so the Identity service can end the SSO session and clear its
cookie; the browser then returns to the registered post-logout URI.

## Using access tokens

Send `Authorization: Bearer <access_token>`; tokens are short-lived JWTs (RS256) with `sub` (user) and `sid`
(session). `GET /sessions` and `DELETE /sessions/{id}` let a user manage their sessions.
