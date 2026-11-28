import type { Transaction } from './types';

/** What a transaction's row tells the user about its way to the balance. */
export type TransactionState =
  | { kind: 'deleted' }
  | { kind: 'pending'; step: 'change' | 'delete' | 'record' }
  | { kind: 'posted' }
  | { kind: 'rejected'; reason: Transaction['declineReason'] }
  | { kind: 'undone'; reason: NonNullable<Transaction['declineReason']> };

/** A posted transaction with a decline reason is one whose change or deletion Accounts declined and was undone. */
export function stateOf(transaction: Transaction): TransactionState {
  const states: Record<Transaction['status'], () => TransactionState> = {
    deleted: () => ({ kind: 'deleted' }),
    pending: () => ({ kind: 'pending', step: transaction.pendingStep ?? 'record' }),
    posted: () => (transaction.declineReason ? { kind: 'undone', reason: transaction.declineReason } : { kind: 'posted' }),
    rejected: () => ({ kind: 'rejected', reason: transaction.declineReason }),
  };

  return states[transaction.status]();
}
