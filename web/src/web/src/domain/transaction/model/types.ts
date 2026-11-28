import type {
  ChangeTransactionRequest,
  DeclineReason as DeclineReasonOrNull,
  PostingStep as PostingStepOrNull,
  TransactionDto,
  TransactionPageDto,
  TransactionStatus,
} from '../api/generated/transactions';

export type { TransactionStatus };

/** A transaction the user entered on a manual account (Transactions service). The amount is signed. */
export type Transaction = TransactionDto;

/** The change Accounts is applying to the balance while the transaction is pending. */
export type PostingStep = NonNullable<PostingStepOrNull>;

/** Why Accounts did not apply the last step. */
export type DeclineReason = NonNullable<DeclineReasonOrNull>;

/** One page of the list, newest first; `nextCursor` is `null` on the last page. */
export type TransactionPage = TransactionPageDto;

/** What `PUT /transactions/{id}` takes, and `POST /transactions` without the account. */
export type TransactionValues = ChangeTransactionRequest;

/** Which transactions a list shows; days are ISO days, both ends included. */
export interface TransactionFilters {
  accountId?: string;
  from?: string;
  to?: string;
}
