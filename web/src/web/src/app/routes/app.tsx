import { createFileRoute, redirect } from '@tanstack/react-router';

import { DashboardPage } from '@/pages/dashboard';

export const Route = createFileRoute('/app')({
  beforeLoad: async ({ context, location }) => {
    await context.auth.ensureReady();

    if (!context.auth.isAuthenticated) {
      throw redirect({ to: '/', search: { returnUrl: location.href } });
    }
  },
  component: DashboardPage,
});
