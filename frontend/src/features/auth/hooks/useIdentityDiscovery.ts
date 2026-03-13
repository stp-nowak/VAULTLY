import { queryOptions, useQuery } from '@tanstack/react-query';

import { getIdentityDiscovery } from '@/features/auth/api/identity';
import { appConfig } from '@/services/config';

export const identityDiscoveryQueryOptions = queryOptions({
  queryFn: getIdentityDiscovery,
  queryKey: ['identity', 'discovery', appConfig.identityBaseUrl],
  staleTime: 5 * 60_000,
});

export function useIdentityDiscovery() {
  return useQuery({
    ...identityDiscoveryQueryOptions,
    enabled: Boolean(appConfig.identityBaseUrl),
  });
}
