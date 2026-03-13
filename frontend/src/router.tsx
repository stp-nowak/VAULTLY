import { createRouter } from '@tanstack/react-router';
import type { QueryClient } from '@tanstack/react-query';

import type { AuthContextValue } from '@/features/auth/context/AuthContext';
import { routeTree } from '@/routeTree.gen';

export interface AppRouterContext {
  auth: AuthContextValue;
  queryClient: QueryClient;
}

export const router = createRouter({
  routeTree,
  context: {
    auth: undefined!,
    queryClient: undefined!,
  },
  defaultPreload: 'intent',
  defaultPreloadStaleTime: 0,
  scrollRestoration: true,
});

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router;
  }
}
