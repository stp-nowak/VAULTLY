import { describe, expect, it } from 'vitest';

import {
  buildAuthorizationRequest,
  clearStoredAuthTransaction,
  loadStoredAuthTransaction,
  storeAuthTransaction,
} from '@/features/auth/lib/pkce';

describe('pkce helpers', () => {
  it('builds an authorization request with a stored transaction', async () => {
    const result = await buildAuthorizationRequest({
      authorizationEndpoint: 'https://identity.example.com/oauth/authorize',
      clientId: 'vaultly-frontend',
      redirectUri: 'http://localhost:5173/callback',
      scopes: ['openid', 'email', 'profile'],
    });

    expect(result.authorizationUrl).toContain('code_challenge=');
    expect(result.authorizationUrl).toContain('state=');
    expect(result.transaction.codeVerifier).toHaveLength(96);
    expect(result.transaction.redirectUri).toBe('http://localhost:5173/callback');
  });

  it('round-trips the auth transaction through session storage', () => {
    const transaction = {
      codeVerifier: 'verifier',
      createdAt: Date.now(),
      redirectUri: 'http://localhost:5173/callback',
      state: 'state',
    };

    storeAuthTransaction(transaction);
    expect(loadStoredAuthTransaction()).toEqual(transaction);

    clearStoredAuthTransaction();
    expect(loadStoredAuthTransaction()).toBeNull();
  });
});
