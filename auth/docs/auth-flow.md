# Vaultly Identity authentication flow

This document is intentionally brief and only covers what a client needs to know.

## Authentication model

Vaultly Identity handles authentication using a standard OAuth 2.0 Authorization Code flow with PKCE.

For client applications, the important part is:

- start authentication with the identity service
- complete the normal PKCE login flow
- exchange the returned code for an access token
- use the access token as a bearer token for protected calls

The PKCE protocol itself is standard and is not re-documented here.

## Discovery endpoints

The service exposes standard discovery endpoints:

- `GET /.well-known/openid-configuration`
- `GET /.well-known/jwks.json`

Clients and downstream services can use them to:

- discover the issuer, authorization endpoint, token endpoint, and JWKS URI
- validate JWT access tokens issued by the identity service

## Session management

Session management is the main Vaultly-specific behavior a client should care about.

### Access token

- The client receives a JWT access token after authentication.
- The access token is used as `Authorization: Bearer <token>` for protected endpoints.
- The access token is short-lived and should be treated as temporary client state.

### Refresh token

- Refresh state is stored in the `vaultly_refresh` cookie.
- The cookie is `HttpOnly`, `Secure`, and `SameSite=None`.
- The client should not try to read or store the refresh token directly in JavaScript.
- Browser clients should send requests with credentials included so the cookie is sent.

### Refresh flow

- When the access token expires, the client should call the refresh endpoint.
- A successful refresh returns a new access token and rotates the refresh cookie.
- If refresh fails with `401 Unauthorized`, the client should treat the session as ended and restart login.

### Logout

- Logging out clears the current refresh cookie and ends the current session.
- After logout, the client should clear any locally stored access token and user state.

### Session listing and revocation

- The client can fetch the user’s sessions using the access token.
- The client can revoke a specific session using the access token.
- If the current session is revoked, the refresh cookie is cleared and the client should consider the user signed out.

## Client guidance

- Keep the access token in short-lived client state, ideally in memory.
- Include credentials on requests that depend on refresh/logout behavior.
- Use bearer authentication for protected API calls.
- If refresh fails, clear local auth state and restart authentication.

## Summary

From the client perspective, Vaultly Identity is:

- a standard PKCE-based authentication service
- an issuer of JWT access tokens
- a provider of well-known discovery metadata
- a session manager with refresh, logout, session listing, and session revocation support



