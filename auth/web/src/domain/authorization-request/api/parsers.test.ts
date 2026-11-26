import { describe, expect, it } from 'vitest';

import { parseSignInResponse } from './parsers';

describe('parseSignInResponse', () => {
  it.each(['https://app.vaultly.local/api/auth/callback?code=c&state=s', 'http://localhost:5174/admin'])(
    'accepts the absolute http(s) URL %s',
    (redirectUrl) => {
      expect(parseSignInResponse({ redirectUrl })).toBe(redirectUrl);
    },
  );

  it.each(['javascript:alert(1)', '/admin', '//evil.example', ''])('rejects %s', (redirectUrl) => {
    expect(() => parseSignInResponse({ redirectUrl })).toThrow('Sign-in response was invalid.');
  });
});
