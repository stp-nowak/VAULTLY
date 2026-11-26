import { queryOptions } from '@tanstack/react-query';

import { getConsoleAccess } from './consoleAdminApi';

export const consoleAdminQueryKeys = {
  access: ['console-admin', 'access'] as const,
  all: ['console-admin'] as const,
};

export function consoleAccessQueryOptions() {
  return queryOptions({
    queryFn: getConsoleAccess,
    queryKey: consoleAdminQueryKeys.access,
  });
}
