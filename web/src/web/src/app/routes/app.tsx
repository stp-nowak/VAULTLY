import { createFileRoute, redirect } from '@tanstack/react-router';

import { DashboardPage } from '@/pages/dashboard';

export const Route = createFileRoute('/app')({
  beforeLoad: async ({ context, location }) => {
    await context.auth.ensureReady();

    if (!context.auth.isAuthenticated) {
      throw redirect({ search: { returnUrl: location.href }, to: '/login' });
    }
  },
  component: DashboardPage,
});
