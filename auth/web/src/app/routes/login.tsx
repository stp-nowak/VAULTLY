import { createFileRoute } from '@tanstack/react-router';

import { authorizationRequestQueryOptions } from '@/domain/authorization-request';
import { LoginPage } from '@/pages/login';
import { readSearchString } from '@/shared/lib';

interface LoginSearch {
  request?: string;
}

export const Route = createFileRoute('/login')({
  validateSearch: (search): LoginSearch => ({ request: readSearchString(search.request) }),
  loaderDeps: ({ search }) => ({ request: search.request }),
  // Start loading the authorization request with the route; the page renders its own loading/error states, so the
  // loader does not wait for it.
  loader: ({ context, deps }) => {
    if (deps.request) {
      void context.queryClient.prefetchQuery(authorizationRequestQueryOptions(deps.request));
    }
  },
  component: LoginPage,
});
