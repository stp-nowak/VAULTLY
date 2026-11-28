import { createFileRoute } from '@tanstack/react-router';

import { accountsQueryOptions } from '@/domain/account';
import { AccountsPage } from '@/pages/accounts';

export const Route = createFileRoute('/app/accounts/')({
  loader: ({ context }) => {
    void context.queryClient.prefetchQuery(accountsQueryOptions());
  },
  component: AccountsPage,
});
