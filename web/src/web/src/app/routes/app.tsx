import { createFileRoute, redirect } from '@tanstack/react-router';

import { AppLayout } from '@/app/layouts/AppLayout';

export const Route = createFileRoute('/app')({
  beforeLoad: async ({ context, location }) => {
    const auth = await context.auth.ensureReady();

    if (auth.status !== 'authenticated') {
      throw redirect({ to: '/', search: { returnUrl: location.href } });
    }
  },
  component: AppLayout,
});
