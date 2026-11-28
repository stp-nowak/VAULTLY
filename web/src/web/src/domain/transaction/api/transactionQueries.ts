import { infiniteQueryOptions, type InfiniteData } from '@tanstack/react-query';

import type { TransactionFilters, TransactionPage } from '../model/types';
import { getTransactions } from './transactionsApi';

/** How often a list asks again while one of its transactions waits for Accounts. */
export const pendingRefreshMs = 1000;

export const transactionQueryKeys = {
  all: ['transactions'] as const,
  list: (filters: TransactionFilters) => ['transactions', 'list', filters] as const,
};

export function hasPending(data: InfiniteData<TransactionPage> | undefined): boolean {
  return data?.pages.some((page) => page.items.some((transaction) => transaction.status === 'pending')) ?? false;
}

/** The transactions matching the filters, page by page with the cursor; refreshed while any is pending. */
export function transactionsQueryOptions(filters: TransactionFilters) {
  return infiniteQueryOptions({
    getNextPageParam: (page: TransactionPage) => page.nextCursor,
    initialPageParam: null as string | null,
    queryFn: ({ pageParam }) => getTransactions(filters, pageParam),
    queryKey: transactionQueryKeys.list(filters),
    refetchInterval: (query) => (hasPending(query.state.data) ? pendingRefreshMs : false),
  });
}
