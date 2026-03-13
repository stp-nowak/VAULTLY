import { createFileRoute, redirect } from '@tanstack/react-router';

import { LoginPanel } from '@/features/auth/components/LoginPanel';

export const Route = createFileRoute('/login')({
  beforeLoad: async ({ context }) => {
    await context.auth.ensureReady();

    if (context.auth.isAuthenticated) {
      throw redirect({ to: '/app' });
    }
  },
  component: LoginPanel,
});
