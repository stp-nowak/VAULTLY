import { createFileRoute } from '@tanstack/react-router';

import { TransactionsRoute } from '@/app/routeComponents/TransactionsRoute';
import type { TransactionFilters } from '@/domain/transaction';
import { isIsoDay } from '@/shared/lib';

export const Route = createFileRoute('/app/transactions')({
  validateSearch: (search): TransactionFilters => ({
    accountId: typeof search.accountId === 'string' && search.accountId ? search.accountId : undefined,
    from: isIsoDay(search.from) ? search.from : undefined,
    to: isIsoDay(search.to) ? search.to : undefined,
  }),
  component: TransactionsRoute,
});
