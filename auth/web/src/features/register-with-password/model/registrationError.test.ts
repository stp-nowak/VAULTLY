import { describe, expect, it } from 'vitest';

import { HttpError } from '@/shared/api';

import { registrationErrorKey } from './registrationError';

describe('registrationErrorKey', () => {
  it.each([
    [new HttpError(409, 'x', { error: 'email_taken' }), 'errors.emailTaken'],
    [new HttpError(400, 'x', { error: 'weak_password' }), 'errors.weakPassword'],
    [new HttpError(400, 'x', { error: 'invalid_input' }), 'errors.invalidInput'],
    [new HttpError(429, 'x', null), 'common:errors.tooManyAttempts'],
    [new HttpError(400, 'x', { error: 'request_expired' }), 'domain/authorization-request:invalidLink'],
    [new HttpError(403, 'x', { error: 'sign_in_method_disabled' }), 'errors.methodDisabled'],
    [new Error('offline'), 'errors.unknown'],
  ])('maps %o to %s', (error, key) => {
    expect(registrationErrorKey(error)).toBe(key);
  });
});
