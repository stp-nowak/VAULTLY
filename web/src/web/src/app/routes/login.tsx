import { createFileRoute, redirect } from '@tanstack/react-router';

import { buildLoginUrl } from '@/domain/identity';
import { LoginPage } from '@/pages/login';

interface LoginSearch {
  error?: string;
  returnUrl?: string;
}

// Sign-in itself happens on the Identity service's own UI. This route only shows why a sign-in failed
// (`?error=` from the web API callback); without an error it forwards straight to sign-in.
export const Route = createFileRoute('/login')({
  validateSearch: (search): LoginSearch => ({
    error: typeof search.error === 'string' ? search.error : undefined,
    returnUrl: typeof search.returnUrl === 'string' && search.returnUrl.startsWith('/') ? search.returnUrl : undefined,
  }),
  beforeLoad: async ({ context, search }) => {
    await context.auth.ensureReady();

    if (context.auth.isAuthenticated) {
      throw redirect({ to: '/app' });
    }

    if (!search.error) {
      throw redirect({ href: buildLoginUrl(search.returnUrl), reloadDocument: true });
    }
  },
  component: LoginPage,
});
