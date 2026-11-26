import type { QueryStatus } from '@tanstack/react-query';

import type { AuthorizationRequest } from './types';

/** Why the sign-in options cannot be shown (yet). */
export type SignInNotice = 'invalid' | 'loadFailed' | 'loading' | 'unavailable';

/** Everything a sign-in page can show: the options for a usable request, or a notice. */
export type SignInState = { kind: 'ready'; request: AuthorizationRequest } | { kind: SignInNotice };

/** Notice for a request that is not usable, by query status. A successful load of an unusable request is invalid. */
const noticeByQueryStatus: Record<QueryStatus, SignInNotice> = {
  error: 'loadFailed',
  pending: 'loading',
  success: 'invalid',
};

function offersAnyMethod(request: AuthorizationRequest): boolean {
  return request.providers.length > 0 || request.passwordSignIn;
}

/**
 * Derives the sign-in state from the `request` search param and the authorization-request query.
 * Only a loaded `pending` request can be used; unknown (404 → `null`), expired and completed ones are invalid, and a
 * missing id is invalid without waiting for the (disabled) query. A pending request with every way to sign in switched
 * off is unavailable.
 */
export function resolveSignInState(
  hasRequestId: boolean,
  query: { data: AuthorizationRequest | null | undefined; status: QueryStatus },
): SignInState {
  const request = query.data;

  if (!hasRequestId) {
    return { kind: 'invalid' };
  }

  if (request?.status !== 'pending') {
    return { kind: noticeByQueryStatus[query.status] };
  }

  return offersAnyMethod(request) ? { kind: 'ready', request } : { kind: 'unavailable' };
}
