import { createFileRoute } from '@tanstack/react-router';

import { accountsQueryOptions, netWorthQueryOptions } from '@/domain/account';
import { OverviewPage } from '@/pages/overview';

export const Route = createFileRoute('/app/')({
  loader: ({ context }) => {
    void context.queryClient.prefetchQuery(accountsQueryOptions());
    void context.queryClient.prefetchQuery(netWorthQueryOptions());
  },
  component: OverviewPage,
});
