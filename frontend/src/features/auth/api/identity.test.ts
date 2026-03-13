import { describe, expect, it, vi } from 'vitest';

import { exchangeAuthorizationCode, getIdentityDiscovery } from '@/features/auth/api/identity';

describe('identity api', () => {
  it('parses the discovery document', async () => {
    vi.stubEnv('IDENTITY_BASE_URL', 'https://identity.example.com');

    const fetchMock = vi.fn().mockResolvedValue(
      new Response(
        JSON.stringify({
          authorization_endpoint: 'https://identity.example.com/oauth/authorize',
          issuer: 'https://identity.example.com',
          jwks_uri: 'https://identity.example.com/.well-known/jwks.json',
          token_endpoint: 'https://identity.example.com/oauth/token',
        }),
        {
          headers: {
            'Content-Type': 'application/json',
          },
          status: 200,
        },
      ),
    );

    vi.stubGlobal('fetch', fetchMock);

    const discovery = await getIdentityDiscovery();

    expect(discovery.authorizationEndpoint).toBe('https://identity.example.com/oauth/authorize');
    expect(discovery.tokenEndpoint).toBe('https://identity.example.com/oauth/token');

    vi.unstubAllEnvs();
    vi.unstubAllGlobals();
  });

  it('sends the expected PKCE token exchange payload', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(
        JSON.stringify({
          accessToken: 'access-token',
          expiresIn: 900,
          sessionId: 'session-1',
          user: {
            email: 'hello@vaultly.test',
            id: 'user-1',
            name: 'Vaultly User',
          },
        }),
        {
          headers: {
            'Content-Type': 'application/json',
          },
          status: 200,
        },
      ),
    );

    vi.stubGlobal('fetch', fetchMock);

    const result = await exchangeAuthorizationCode({
      clientId: 'vaultly-frontend',
      code: 'auth-code',
      codeVerifier: 'verifier',
      redirectUri: 'http://localhost:5173/callback',
      tokenEndpoint: 'https://identity.example.com/oauth/token',
    });

    const [, requestInit] = fetchMock.mock.calls[0] as [string, RequestInit];
    const requestBody = requestInit.body as URLSearchParams;

    expect(requestInit.credentials).toBe('include');
    expect(requestBody.get('client_id')).toBe('vaultly-frontend');
    expect(requestBody.get('code')).toBe('auth-code');
    expect(requestBody.get('code_verifier')).toBe('verifier');
    expect(result.user.email).toBe('hello@vaultly.test');

    vi.unstubAllGlobals();
  });
});
