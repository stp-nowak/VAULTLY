import { createFileRoute, redirect } from '@tanstack/react-router';

import { LoginPage } from '@/pages/login';
import { readLocalPath } from '@/shared/lib';

interface LoginSearch {
  error?: string;
  returnUrl?: string;
}

// Sign-in itself happens on the Identity service's own UI. This route only shows why a sign-in failed
// (`?error=` from the web API callback); without an error there is nothing to show, so it opens the home page.
export const Route = createFileRoute('/login')({
  validateSearch: (search): LoginSearch => ({
    error: typeof search.error === 'string' ? search.error : undefined,
    returnUrl: readLocalPath(search.returnUrl),
  }),
  beforeLoad: async ({ context, search }) => {
    await context.auth.ensureReady();

    if (context.auth.isAuthenticated) {
      throw redirect({ to: '/app' });
    }

    if (!search.error) {
      throw redirect({ to: '/', search: { returnUrl: search.returnUrl } });
    }
  },
  component: LoginPage,
});
