import { fetchJson } from '@/shared/api';

import type { Account, CurrencyNetWorth, DailyBalance } from '../model/types';

/** Accounts service endpoints, reached through the web API and the gateway. */
export const accountRoutes = {
  account: (id: string) => `/api/accounts/${encodeURIComponent(id)}`,
  accounts: '/api/accounts',
  balances: (id: string) => `/api/accounts/${encodeURIComponent(id)}/balances`,
  close: (id: string) => `/api/accounts/${encodeURIComponent(id)}/close`,
  name: (id: string) => `/api/accounts/${encodeURIComponent(id)}/name`,
  netWorth: '/api/accounts/net-worth',
} as const;

export function getAccounts(includeClosed: boolean): Promise<Account[]> {
  return fetchJson<Account[]>(`${accountRoutes.accounts}?includeClosed=${includeClosed}`);
}

export function getAccount(id: string): Promise<Account> {
  return fetchJson<Account>(accountRoutes.account(id));
}

export function getNetWorth(): Promise<CurrencyNetWorth[]> {
  return fetchJson<CurrencyNetWorth[]>(accountRoutes.netWorth);
}

export function getBalanceHistory(id: string): Promise<DailyBalance[]> {
  return fetchJson<DailyBalance[]>(accountRoutes.balances(id));
}
