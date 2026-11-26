import { createFileRoute, redirect } from '@tanstack/react-router';

import { LandingPage } from '@/pages/landing';
import { readLocalPath } from '@/shared/lib';

interface LandingSearch {
  returnUrl?: string;
}

// The home page never starts sign-in by itself: it only checks for a session and otherwise waits for "Log in".
export const Route = createFileRoute('/')({
  validateSearch: (search): LandingSearch => ({ returnUrl: readLocalPath(search.returnUrl) }),
  beforeLoad: async ({ context }) => {
    await context.auth.ensureReady();

    if (context.auth.isAuthenticated) {
      throw redirect({ to: '/app' });
    }
  },
  component: LandingPage,
});
