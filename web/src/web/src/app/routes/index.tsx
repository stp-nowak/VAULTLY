import { createFileRoute, redirect } from '@tanstack/react-router';

export const Route = createFileRoute('/')({
  beforeLoad: async ({ context }) => {
    await context.auth.ensureReady();

    throw redirect({
      to: context.auth.isAuthenticated ? '/app' : '/login',
    });
  },
});
