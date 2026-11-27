import { describe, expect, it } from 'vitest';

import { toSignInError } from './signInError';

describe('toSignInError', () => {
  it.each(['invalid_state', 'login_failed'] as const)('keeps the known code %s', (code) => {
    expect(toSignInError(code)).toBe(code);
  });

  it.each([undefined, '', 'unknown'])('falls back to login_failed for %s', (code) => {
    expect(toSignInError(code)).toBe('login_failed');
  });
});
