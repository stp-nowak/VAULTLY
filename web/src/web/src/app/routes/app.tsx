import { createFileRoute, redirect } from '@tanstack/react-router';

import { buildLoginUrl } from '@/domain/identity';
import { DashboardPage } from '@/pages/dashboard';

export const Route = createFileRoute('/app')({
  beforeLoad: async ({ context, location }) => {
    await context.auth.ensureReady();

    if (!context.auth.isAuthenticated) {
      throw redirect({ href: buildLoginUrl(location.href), reloadDocument: true });
    }
  },
  component: DashboardPage,
});
