import type { QueryStatus } from '@tanstack/react-query';

import type { AuthorizationRequest } from '@/domain/authorization-request';

/** Why the provider choice cannot be shown (yet). */
export type SignInNotice = 'invalid' | 'loadFailed' | 'loading';

/** Everything the sign-in card can show: the provider choice for a usable request, or a notice. */
export type SignInState = { kind: 'ready'; request: AuthorizationRequest } | { kind: SignInNotice };

/** Notice for a request that is not usable, by query status. A successful load of an unusable request is invalid. */
const noticeByQueryStatus: Record<QueryStatus, SignInNotice> = {
  error: 'loadFailed',
  pending: 'loading',
  success: 'invalid',
};

/**
 * Derives the sign-in state from the `request` search param and the authorization-request query.
 * Only a loaded `pending` request can be used; unknown (404 → `null`), expired and completed ones are invalid,
 * and a missing id is invalid without waiting for the (disabled) query.
 */
export function resolveSignInState(
  hasRequestId: boolean,
  query: { data: AuthorizationRequest | null | undefined; status: QueryStatus },
): SignInState {
  const request = query.data;

  return hasRequestId && request?.status === 'pending'
    ? { kind: 'ready', request }
    : { kind: hasRequestId ? noticeByQueryStatus[query.status] : 'invalid' };
}
