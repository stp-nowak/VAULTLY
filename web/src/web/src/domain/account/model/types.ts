import type {
  AccountDto,
  AccountType,
  CurrencyNetWorthDto,
  DailyBalanceDto,
  OpenAccountRequest,
} from '../api/generated/accounts';

export type { AccountType };

/** One of the user's accounts (Accounts service). The balance is signed: a debt is negative. */
export type Account = AccountDto;

/** Net worth of the open accounts in one currency; liabilities are debts as a positive amount. */
export type CurrencyNetWorth = CurrencyNetWorthDto;

/** The balance at the end of a day with a transaction. */
export type DailyBalance = DailyBalanceDto;

/** What `POST /accounts` takes. */
export type NewAccount = OpenAccountRequest & { type: AccountType };

const accountTypeOrder: Record<AccountType, number> = { cash: 3, checking: 0, credit_card: 2, savings: 1 };

/** Every account type, in the order a picker offers them; the record above fails to compile when one is missing. */
export const accountTypes = (Object.keys(accountTypeOrder) as AccountType[]).sort(
  (left, right) => accountTypeOrder[left] - accountTypeOrder[right],
);
