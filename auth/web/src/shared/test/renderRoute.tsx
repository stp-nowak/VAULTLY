import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import {
  createMemoryHistory,
  createRootRoute,
  createRoute,
  createRouter,
  Outlet,
  RouterProvider,
  type AnyRoute,
} from '@tanstack/react-router';
import { render } from '@testing-library/react';

import { parseRawSearch, readSearchString, stringifyRawSearch } from '@/shared/lib';

interface RenderRouteOptions {
  /** Component rendered for the route, e.g. a page that reads its search params with `getRouteApi(path)`. */
  component: () => React.ReactNode;
  /** URL the memory history starts at, e.g. `/login?request=abc`. */
  initialEntry: string;
  /** Route path, e.g. `/login`. Search params are passed through as strings. */
  path: string;
}

/**
 * Renders a component inside a minimal in-memory router (same search param handling as the app) and a fresh
 * QueryClient without retries. Use for pages that read route state.
 */
export async function renderRoute({ component, initialEntry, path }: RenderRouteOptions) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const rootRoute = createRootRoute({ component: Outlet });
  const route: AnyRoute = createRoute({
    component,
    getParentRoute: () => rootRoute,
    path,
    validateSearch: (search: Record<string, unknown>) =>
      Object.fromEntries(Object.entries(search).map(([key, value]) => [key, readSearchString(value)])),
  });
  const router = createRouter({
    history: createMemoryHistory({ initialEntries: [initialEntry] }),
    parseSearch: parseRawSearch,
    routeTree: rootRoute.addChildren([route]),
    stringifySearch: stringifyRawSearch,
  });

  await router.load();

  return {
    queryClient,
    router,
    ...render(
      <QueryClientProvider client={queryClient}>
        <RouterProvider router={router} />
      </QueryClientProvider>,
    ),
  };
}
