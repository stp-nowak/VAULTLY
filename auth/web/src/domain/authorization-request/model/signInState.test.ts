import { describe, expect, it } from 'vitest';

import type { AuthorizationRequest } from './types';

import { resolveSignInState } from './signInState';

const request = (status: AuthorizationRequest['status'], overrides: Partial<AuthorizationRequest> = {}): AuthorizationRequest => ({
  clientName: 'Vaultly',
  id: 'request-1',
  passwordSignIn: false,
  providers: [{ id: 'google', name: 'Google', startUrl: '/google/authorize?request=request-1' }],
  status,
  ...overrides,
});

describe('resolveSignInState', () => {
  it('is ready for a loaded pending request', () => {
    const pending = request('pending');

    expect(resolveSignInState(true, { data: pending, status: 'success' })).toEqual({ kind: 'ready', request: pending });
  });

  it('is ready for a pending request that only offers a password', () => {
    const passwordOnly = request('pending', { passwordSignIn: true, providers: [] });

    expect(resolveSignInState(true, { data: passwordOnly, status: 'success' })).toEqual({ kind: 'ready', request: passwordOnly });
  });

  it.each([
    ['missing request id', false, { data: undefined, status: 'pending' as const }, 'invalid'],
    ['still loading', true, { data: undefined, status: 'pending' as const }, 'loading'],
    ['failed to load', true, { data: undefined, status: 'error' as const }, 'loadFailed'],
    ['unknown request (404)', true, { data: null, status: 'success' as const }, 'invalid'],
    ['expired request', true, { data: request('expired'), status: 'success' as const }, 'invalid'],
    ['completed request', true, { data: request('completed'), status: 'success' as const }, 'invalid'],
    ['pending request without any way to sign in', true, { data: request('pending', { providers: [] }), status: 'success' as const }, 'unavailable'],
  ])('shows a notice when %s', (_case, hasRequestId, query, expected) => {
    expect(resolveSignInState(hasRequestId, query)).toEqual({ kind: expected });
  });
});
