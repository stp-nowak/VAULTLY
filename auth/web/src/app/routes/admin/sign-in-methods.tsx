import { createFileRoute } from '@tanstack/react-router';

import { signInMethodsQueryOptions } from '@/domain/sign-in-settings';
import { AdminSignInMethodsPage } from '@/pages/admin-sign-in-methods';

export const Route = createFileRoute('/admin/sign-in-methods')({
  loader: ({ context }) => {
    void context.queryClient.prefetchQuery(signInMethodsQueryOptions());
  },
  component: AdminSignInMethodsPage,
});
