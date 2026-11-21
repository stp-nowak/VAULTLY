import { createRouter } from '@tanstack/react-router';
import type { QueryClient } from '@tanstack/react-query';

import { parseRawSearch, stringifyRawSearch } from '@/shared/lib';

import { queryClient } from './providers/queryClient';
import { routeTree } from './routeTree.gen';

export interface AppRouterContext {
  queryClient: QueryClient;
}

export const router = createRouter({
  routeTree,
  context: { queryClient },
  defaultPreload: 'intent',
  defaultPreloadStaleTime: 0,
  // Request ids and error codes come from the Identity API and must stay strings (see shared/lib/searchParams).
  parseSearch: parseRawSearch,
  stringifySearch: stringifyRawSearch,
  scrollRestoration: true,
});

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router;
  }
}
