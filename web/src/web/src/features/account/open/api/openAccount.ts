import { accountRoutes, type Account, type NewAccount } from '@/domain/account';
import { sendJson } from '@/shared/api';

export function openAccount(account: NewAccount): Promise<Account> {
  return sendJson<Account>(accountRoutes.accounts, 'POST', account);
}
