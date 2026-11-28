import { fetchJson } from '@/shared/api';

import type { TransactionFilters, TransactionPage } from '../model/types';

/** Transactions service endpoints, reached through the web API and the gateway. */
export const transactionRoutes = {
  transaction: (id: string) => `/api/transactions/${encodeURIComponent(id)}`,
  transactions: '/api/transactions',
} as const;

export const transactionsPageSize = 25;

export function getTransactions(filters: TransactionFilters, cursor: string | null): Promise<TransactionPage> {
  const search = new URLSearchParams({ limit: String(transactionsPageSize) });
  for (const [name, value] of Object.entries({ ...filters, cursor })) {
    if (value) {
      search.set(name, value);
    }
  }

  return fetchJson<TransactionPage>(`${transactionRoutes.transactions}?${search}`);
}
