import { describe, expect, it } from 'vitest';

import type { AuthorizationRequest } from '@/domain/authorization-request';

import { resolveSignInState } from './signInState';

const request = (status: AuthorizationRequest['status']): AuthorizationRequest => ({
  clientName: 'Vaultly',
  id: 'request-1',
  providers: [{ id: 'google', name: 'Google', startUrl: '/google/authorize?request=request-1' }],
  status,
});

describe('resolveSignInState', () => {
  it('is ready for a loaded pending request', () => {
    const pending = request('pending');

    expect(resolveSignInState(true, { data: pending, status: 'success' })).toEqual({ kind: 'ready', request: pending });
  });

  it.each([
    ['missing request id', false, { data: undefined, status: 'pending' as const }, 'invalid'],
    ['still loading', true, { data: undefined, status: 'pending' as const }, 'loading'],
    ['failed to load', true, { data: undefined, status: 'error' as const }, 'loadFailed'],
    ['unknown request (404)', true, { data: null, status: 'success' as const }, 'invalid'],
    ['expired request', true, { data: request('expired'), status: 'success' as const }, 'invalid'],
    ['completed request', true, { data: request('completed'), status: 'success' as const }, 'invalid'],
  ])('shows a notice when %s', (_case, hasRequestId, query, expected) => {
    expect(resolveSignInState(hasRequestId, query)).toEqual({ kind: expected });
  });
});
