import { isIsoDay, parseAmount } from '@/shared/lib';

import type { Transaction, TransactionValues } from './types';

export type TransactionDirection = 'expense' | 'income';

/** What the user types into a transaction form; the amount is unsigned, the direction gives its sign. */
export interface TransactionDraft {
  amount: string;
  bookedOn: string;
  description: string;
  direction: TransactionDirection;
  note: string;
}

export type DraftProblem = 'amount' | 'bookedOn' | 'description' | 'descriptionLength' | 'noteLength';

export type DraftResult = { ok: true; values: TransactionValues } | { ok: false; problem: DraftProblem };

export const descriptionMaxLength = 200;
export const noteMaxLength = 1000;

export function emptyDraft(bookedOn: string): TransactionDraft {
  return { amount: '', bookedOn, description: '', direction: 'expense', note: '' };
}

export function draftOf(transaction: Transaction): TransactionDraft {
  return {
    amount: String(Math.abs(transaction.amount)),
    bookedOn: transaction.bookedOn,
    description: transaction.description,
    direction: transaction.amount < 0 ? 'expense' : 'income',
    note: transaction.note ?? '',
  };
}

function problemOf(draft: TransactionDraft, amount: number | null): DraftProblem | null {
  const description = draft.description.trim();
  const problems: [boolean, DraftProblem][] = [
    [amount === null || amount <= 0, 'amount'],
    [!isIsoDay(draft.bookedOn), 'bookedOn'],
    [description.length === 0, 'description'],
    [description.length > descriptionMaxLength, 'descriptionLength'],
    [draft.note.trim().length > noteMaxLength, 'noteLength'],
  ];

  return problems.find(([failed]) => failed)?.[1] ?? null;
}

/** Checks the draft and turns it into the signed values the service takes. */
export function readDraft(draft: TransactionDraft): DraftResult {
  const amount = parseAmount(draft.amount);
  const problem = problemOf(draft, amount);
  if (problem !== null || amount === null) {
    return { ok: false, problem: problem ?? 'amount' };
  }

  return {
    ok: true,
    values: {
      amount: draft.direction === 'expense' ? -amount : amount,
      bookedOn: draft.bookedOn,
      description: draft.description.trim(),
      note: draft.note.trim() || null,
    },
  };
}
