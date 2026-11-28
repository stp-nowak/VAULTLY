import { createFileRoute } from '@tanstack/react-router';

import { netWorthQueryOptions } from '@/domain/account';
import { NetWorthPage } from '@/pages/net-worth';

export const Route = createFileRoute('/app/net-worth')({
  loader: ({ context }) => {
    void context.queryClient.prefetchQuery(netWorthQueryOptions());
  },
  component: NetWorthPage,
});
