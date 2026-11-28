import type { AccountType, NewAccount } from '@/domain/account';
import { parseAmount } from '@/shared/lib';

export interface AccountDraft {
  currency: string;
  name: string;
  openingBalance: string;
  type: AccountType;
}

export type AccountDraftProblem = 'name' | 'openingBalance';

export const accountNameMaxLength = 100;

/** Checks the draft; the opening balance is signed, a debt is negative. */
export function readAccountDraft(
  draft: AccountDraft,
): { ok: true; values: NewAccount } | { ok: false; problem: AccountDraftProblem } {
  const name = draft.name.trim();
  const openingBalance = parseAmount(draft.openingBalance || '0');

  if (name.length === 0 || name.length > accountNameMaxLength) {
    return { ok: false, problem: 'name' };
  }

  if (openingBalance === null) {
    return { ok: false, problem: 'openingBalance' };
  }

  return { ok: true, values: { currency: draft.currency, name, openingBalance, type: draft.type } };
}
