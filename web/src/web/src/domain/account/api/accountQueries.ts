import { queryOptions } from '@tanstack/react-query';

import { getAccount, getAccounts, getBalanceHistory, getNetWorth } from './accountsApi';

/** Query keys of the account domain; invalidating `all` refreshes every balance on screen. */
export const accountQueryKeys = {
  account: (id: string) => ['accounts', 'detail', id] as const,
  accounts: (includeClosed: boolean) => ['accounts', 'list', { includeClosed }] as const,
  all: ['accounts'] as const,
  balances: (id: string) => ['accounts', 'balances', id] as const,
  netWorth: ['accounts', 'net-worth'] as const,
};

export function accountsQueryOptions(includeClosed = false) {
  return queryOptions({ queryFn: () => getAccounts(includeClosed), queryKey: accountQueryKeys.accounts(includeClosed) });
}

export function accountQueryOptions(id: string) {
  return queryOptions({ queryFn: () => getAccount(id), queryKey: accountQueryKeys.account(id) });
}

export function netWorthQueryOptions() {
  return queryOptions({ queryFn: getNetWorth, queryKey: accountQueryKeys.netWorth });
}

export function balanceHistoryQueryOptions(id: string) {
  return queryOptions({ queryFn: () => getBalanceHistory(id), queryKey: accountQueryKeys.balances(id) });
}
