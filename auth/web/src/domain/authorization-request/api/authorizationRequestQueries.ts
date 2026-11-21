import { queryOptions } from '@tanstack/react-query';

import { getAuthorizationRequest } from './authorizationRequestApi';

/** Query keys of the authorization-request domain. */
export const authorizationRequestQueryKeys = {
  all: ['authorization-request'] as const,
  byId: (id: string) => ['authorization-request', id] as const,
};

/** One authorization request; `null` data means the id is unknown. */
export function authorizationRequestQueryOptions(id: string) {
  return queryOptions({
    queryFn: () => getAuthorizationRequest(id),
    queryKey: authorizationRequestQueryKeys.byId(id),
  });
}
