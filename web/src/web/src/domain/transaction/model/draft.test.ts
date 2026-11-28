import { describe, expect, it } from 'vitest';

import { draftOf, emptyDraft, readDraft, type TransactionDraft } from './draft';
import type { Transaction } from './types';

const draft: TransactionDraft = {
  amount: '12,50',
  bookedOn: '2026-11-28',
  description: '  Groceries ',
  direction: 'expense',
  note: ' ',
};

describe('transaction draft', () => {
  it('signs an expense and trims the texts', () => {
    expect(readDraft(draft)).toEqual({
      ok: true,
      values: { amount: -12.5, bookedOn: '2026-11-28', description: 'Groceries', note: null },
    });
  });

  it('keeps an income positive', () => {
    expect(readDraft({ ...draft, direction: 'income' })).toMatchObject({ values: { amount: 12.5 } });
  });

  it.each([
    [{ amount: '0' }, 'amount'],
    [{ amount: 'ten' }, 'amount'],
    [{ amount: '-5' }, 'amount'],
    [{ bookedOn: '' }, 'bookedOn'],
    [{ description: ' ' }, 'description'],
    [{ description: 'x'.repeat(201) }, 'descriptionLength'],
    [{ note: 'x'.repeat(1001) }, 'noteLength'],
  ])('reports %o as %s', (change, problem) => {
    expect(readDraft({ ...draft, ...change })).toEqual({ ok: false, problem });
  });

  it('starts empty on the given day', () => {
    expect(emptyDraft('2026-11-28')).toMatchObject({ amount: '', bookedOn: '2026-11-28', direction: 'expense' });
  });

  it('reads a transaction back into a draft', () => {
    const transaction = { amount: 300, bookedOn: '2026-11-01', description: 'Salary', note: null } as Transaction;

    expect(draftOf(transaction)).toEqual({
      amount: '300',
      bookedOn: '2026-11-01',
      description: 'Salary',
      direction: 'income',
      note: '',
    });
  });
});
