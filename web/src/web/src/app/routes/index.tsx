import { createFileRoute, redirect } from '@tanstack/react-router';

import { buildLoginUrl } from '@/domain/identity';

export const Route = createFileRoute('/')({
  beforeLoad: async ({ context }) => {
    await context.auth.ensureReady();

    throw context.auth.isAuthenticated
      ? redirect({ to: '/app' })
      : redirect({ href: buildLoginUrl('/app'), reloadDocument: true });
  },
});
