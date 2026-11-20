import { queryOptions } from '@tanstack/react-query';

import { listSessions } from './identityApi';

/** Query keys of the identity domain. */
export const identityQueryKeys = {
  all: ['identity'] as const,
  sessions: (sessionId: string | null) => ['identity', 'sessions', sessionId] as const,
};

/** Sessions of the signed-in user; keyed by the current session so a new login refetches. */
export function sessionsQueryOptions(sessionId: string | null) {
  return queryOptions({
    enabled: sessionId !== null,
    queryFn: listSessions,
    queryKey: identityQueryKeys.sessions(sessionId),
  });
}
