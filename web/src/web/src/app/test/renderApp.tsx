import { QueryClient } from '@tanstack/react-query';
import { createMemoryHistory } from '@tanstack/react-router';
import { render } from '@testing-library/react';
import { vi } from 'vitest';

import { App } from '../App';
import { createAppRouter } from '../router';

/**
 * Renders the whole app (providers, real route tree) at `initialEntry` with an in-memory history and a fresh
 * QueryClient. The session comes from the MSW handler for `GET /api/auth/user`.
 */
export function renderApp(initialEntry: string) {
  // jsdom does not implement scrolling; the router's scroll restoration calls it on every navigation.
  vi.spyOn(window, 'scrollTo').mockImplementation(() => undefined);

  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const router = createAppRouter(createMemoryHistory({ initialEntries: [initialEntry] }));

  return { router, ...render(<App queryClient={queryClient} router={router} />) };
}
