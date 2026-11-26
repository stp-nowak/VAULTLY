import { createFileRoute } from '@tanstack/react-router';

import { authorizationRequestQueryOptions } from '@/domain/authorization-request';
import { RegisterPage } from '@/pages/register';
import { readSearchString } from '@/shared/lib';

interface RegisterSearch {
  request?: string;
}

export const Route = createFileRoute('/register')({
  validateSearch: (search): RegisterSearch => ({ request: readSearchString(search.request) }),
  loaderDeps: ({ search }) => ({ request: search.request }),
  loader: ({ context, deps }) => {
    if (deps.request) {
      void context.queryClient.prefetchQuery(authorizationRequestQueryOptions(deps.request));
    }
  },
  component: RegisterPage,
});
