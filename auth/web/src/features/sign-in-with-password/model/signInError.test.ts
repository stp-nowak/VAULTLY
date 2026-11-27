import { describe, expect, it } from 'vitest';

import { HttpError } from '@/shared/api';

import { signInErrorKey } from './signInError';

describe('signInErrorKey', () => {
  it.each([
    [new HttpError(400, 'x', { error: 'invalid_credentials' }), 'errors.invalidCredentials'],
    [new HttpError(429, 'x', null), 'common:errors.tooManyAttempts'],
    [new HttpError(400, 'x', { error: 'request_expired' }), 'domain/authorization-request:invalidLink'],
    [new HttpError(403, 'x', { error: 'sign_in_method_disabled' }), 'errors.methodDisabled'],
    [new HttpError(500, 'x', null), 'errors.unknown'],
    [new Error('offline'), 'errors.unknown'],
  ])('maps %o to %s', (error, key) => {
    expect(signInErrorKey(error)).toBe(key);
  });
});
