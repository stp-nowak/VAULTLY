import { createFileRoute, redirect } from '@tanstack/react-router';

import { LoginPage } from '@/pages/login';

interface LoginSearch {
  error?: string;
  returnUrl?: string;
}

export const Route = createFileRoute('/login')({
  validateSearch: (search): LoginSearch => ({
    error: typeof search.error === 'string' ? search.error : undefined,
    returnUrl: typeof search.returnUrl === 'string' && search.returnUrl.startsWith('/') ? search.returnUrl : undefined,
  }),
  beforeLoad: async ({ context }) => {
    await context.auth.ensureReady();

    if (context.auth.isAuthenticated) {
      throw redirect({ to: '/app' });
    }
  },
  component: LoginPage,
});
