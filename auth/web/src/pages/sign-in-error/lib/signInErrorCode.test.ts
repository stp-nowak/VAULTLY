import { describe, expect, it } from 'vitest';

import { toSignInErrorCode } from './signInErrorCode';

describe('toSignInErrorCode', () => {
  it.each(['access_denied', 'invalid_request', 'login_failed', 'request_expired'] as const)('keeps %s', (code) => {
    expect(toSignInErrorCode(code)).toBe(code);
  });

  it.each([undefined, '', 'toString', 'unknown'])('falls back to login_failed for %s', (code) => {
    expect(toSignInErrorCode(code)).toBe('login_failed');
  });
});
