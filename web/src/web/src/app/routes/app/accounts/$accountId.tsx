import { createFileRoute } from '@tanstack/react-router';

import { AccountRoute } from '@/app/routeComponents/AccountRoute';
import { accountQueryOptions } from '@/domain/account';

export const Route = createFileRoute('/app/accounts/$accountId')({
  loader: ({ context, params }) => {
    void context.queryClient.prefetchQuery(accountQueryOptions(params.accountId));
  },
  component: AccountRoute,
});
