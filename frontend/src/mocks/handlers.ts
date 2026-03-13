import { http, HttpResponse } from 'msw';

export const handlers = [
  http.get('https://identity.vaultly.local/.well-known/openid-configuration', () =>
    HttpResponse.json({
      authorization_endpoint: 'https://identity.vaultly.local/oauth/authorize',
      issuer: 'https://identity.vaultly.local',
      jwks_uri: 'https://identity.vaultly.local/.well-known/jwks.json',
      token_endpoint: 'https://identity.vaultly.local/oauth/token',
    })),
];
