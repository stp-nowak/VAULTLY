import { createFileRoute, redirect } from '@tanstack/react-router';

import { AppRouteView } from '@/route-components/AppRouteView';

export const Route = createFileRoute('/app')({
  beforeLoad: async ({ context }) => {
    await context.auth.ensureReady();

    if (!context.auth.isAuthenticated) {
      throw redirect({ to: '/login' });
    }
  },
  component: AppRouteView,
});
