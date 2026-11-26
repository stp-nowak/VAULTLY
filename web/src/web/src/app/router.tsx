import { createRouter, type RouterHistory } from '@tanstack/react-router';
import type { QueryClient } from '@tanstack/react-query';

import type { AuthContextValue } from '@/domain/identity';

import { routeTree } from './routeTree.gen';

export interface AppRouterContext {
  auth: AuthContextValue;
  queryClient: QueryClient;
}

/** Builds the app router. `history` defaults to the browser's; tests pass a memory history. */
export function createAppRouter(history?: RouterHistory) {
  return createRouter({
    routeTree,
    context: {
      auth: undefined!,
      queryClient: undefined!,
    },
    defaultPreload: 'intent',
    defaultPreloadStaleTime: 0,
    history,
    scrollRestoration: true,
  });
}

export type AppRouter = ReturnType<typeof createAppRouter>;

export const router = createAppRouter();

declare module '@tanstack/react-router' {
  interface Register {
    router: AppRouter;
  }
}
