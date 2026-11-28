import { http, HttpResponse } from 'msw';

import { server } from '@/shared/test/msw/server';

interface FakeAccount {
  closedAt: string | null;
  currency: string;
  id: string;
  name: string;
  openedAt: string;
  openingBalance: number;
  type: string;
}

interface FakeTransaction {
  accountId: string;
  amount: number;
  bookedOn: string;
  declineReason: string | null;
  description: string;
  id: string;
  note: string | null;
  pendingStep: 'change' | 'delete' | 'record' | null;
  postedAmount: number;
  recordedAt: string;
  seenPending?: boolean;
  sequence: number;
  status: 'deleted' | 'pending' | 'posted' | 'rejected';
}

interface TransactionBody {
  accountId?: string;
  amount: number;
  bookedOn: string;
  description: string;
  note: string | null;
}

/**
 * In-memory Accounts and Transactions services behind the web API paths. Like the posting saga, a pending step is
 * applied (or declined, on a closed account) once a list has shown it pending, the next time the list is read.
 */
export function startFakeMoneyApi() {
  const accounts: FakeAccount[] = [];
  const transactions: FakeTransaction[] = [];
  let sequence = 0;

  const balanceOf = (account: FakeAccount) =>
    account.openingBalance +
    transactions
      .filter((transaction) => transaction.accountId === account.id && transaction.status !== 'rejected')
      .reduce((sum, transaction) => sum + transaction.postedAmount, 0);
  const accountDto = (account: FakeAccount) => ({ ...account, balance: balanceOf(account) });
  const transactionDto = ({ postedAmount: _posted, seenPending: _seen, sequence: _sequence, ...transaction }: FakeTransaction) => {
    void _posted;
    void _seen;
    void _sequence;
    return { ...transaction, currency: accounts.find((account) => account.id === transaction.accountId)?.currency };
  };
  const findAccount = (id: unknown) => accounts.find((account) => account.id === id);
  const findTransaction = (id: unknown) =>
    transactions.find((transaction) => transaction.id === id && transaction.status !== 'deleted');
  const notFound = () => HttpResponse.json({ status: 404 }, { status: 404 });
  const conflict = () => HttpResponse.json({ status: 409 }, { status: 409 });

  const settle = () => {
    for (const transaction of transactions.filter((candidate) => candidate.status === 'pending' && candidate.seenPending)) {
      const isClosed = findAccount(transaction.accountId)?.closedAt !== null;
      const steps = {
        change: () => Object.assign(transaction, { postedAmount: transaction.amount }),
        delete: () => Object.assign(transaction, { postedAmount: 0, status: 'deleted' }),
        record: () =>
          isClosed
            ? Object.assign(transaction, { declineReason: 'account_closed', status: 'rejected' })
            : Object.assign(transaction, { postedAmount: transaction.amount }),
      };
      const step = transaction.pendingStep ?? 'record';
      Object.assign(transaction, { pendingStep: null, seenPending: false, status: 'posted' });
      steps[step]();
    }

    for (const transaction of transactions.filter((candidate) => candidate.status === 'pending')) {
      transaction.seenPending = true;
    }
  };

  server.use(
    http.get('*/api/accounts/net-worth', () => {
      const currencies = [...new Set(accounts.filter((account) => !account.closedAt).map((account) => account.currency))];
      return HttpResponse.json(
        currencies.map((currency) => {
          const balances = accounts
            .filter((account) => !account.closedAt && account.currency === currency)
            .map(balanceOf);
          const assets = balances.filter((balance) => balance > 0).reduce((sum, balance) => sum + balance, 0);
          const liabilities = -balances.filter((balance) => balance < 0).reduce((sum, balance) => sum + balance, 0);
          return { assets, currency, liabilities, netWorth: assets - liabilities };
        }),
      );
    }),
    http.get('*/api/accounts', ({ request }) => {
      const includeClosed = new URL(request.url).searchParams.get('includeClosed') === 'true';
      return HttpResponse.json(accounts.filter((account) => includeClosed || !account.closedAt).map(accountDto));
    }),
    http.post('*/api/accounts', async ({ request }) => {
      const body = (await request.json()) as Omit<FakeAccount, 'closedAt' | 'id' | 'openedAt'>;
      const account = { ...body, closedAt: null, id: `account-${accounts.length + 1}`, openedAt: '2026-11-01T10:00:00Z' };
      accounts.push(account);
      return HttpResponse.json(accountDto(account), { status: 201 });
    }),
    http.get('*/api/accounts/:id', ({ params }) => {
      const account = findAccount(params.id);
      return account ? HttpResponse.json(accountDto(account)) : notFound();
    }),
    http.put('*/api/accounts/:id/name', async ({ params, request }) => {
      const account = findAccount(params.id);
      if (!account) {
        return notFound();
      }

      account.name = ((await request.json()) as { name: string }).name;
      return new HttpResponse(null, { status: 204 });
    }),
    http.post('*/api/accounts/:id/close', ({ params }) => {
      const account = findAccount(params.id);
      if (!account) {
        return notFound();
      }

      account.closedAt = '2026-11-20T10:00:00Z';
      return new HttpResponse(null, { status: 204 });
    }),
    http.get('*/api/accounts/:id/balances', ({ params }) => {
      const account = findAccount(params.id);
      if (!account) {
        return notFound();
      }

      const days = [...new Set(transactions.filter((t) => t.accountId === account.id).map((t) => t.bookedOn))].sort();
      let balance = account.openingBalance;
      return HttpResponse.json(
        days.map((date) => {
          balance += transactions
            .filter((t) => t.accountId === account.id && t.bookedOn === date && t.status !== 'rejected')
            .reduce((sum, t) => sum + t.postedAmount, 0);
          return { balance, date };
        }),
      );
    }),

    http.get('*/api/transactions', ({ request }) => {
      settle();
      const search = new URL(request.url).searchParams;
      const limit = Number(search.get('limit') ?? 50);
      const start = Number(search.get('cursor') ?? 0);
      const accountId = search.get('accountId');
      const matching = transactions
        .filter((t) => t.status !== 'deleted' && (!accountId || t.accountId === accountId))
        .filter((t) => (!search.get('from') || t.bookedOn >= search.get('from')!) && (!search.get('to') || t.bookedOn <= search.get('to')!))
        .sort((a, b) => b.bookedOn.localeCompare(a.bookedOn) || b.sequence - a.sequence);
      const items = matching.slice(start, start + limit);
      const nextCursor = start + limit < matching.length ? String(start + limit) : null;
      return HttpResponse.json({ items: items.map(transactionDto), nextCursor });
    }),
    http.post('*/api/transactions', async ({ request }) => {
      const body = (await request.json()) as TransactionBody;
      const account = findAccount(body.accountId);
      if (!account) {
        return notFound();
      }

      if (account.closedAt) {
        return conflict();
      }

      sequence += 1;
      const transaction: FakeTransaction = {
        ...body,
        accountId: account.id,
        declineReason: null,
        id: `transaction-${sequence}`,
        pendingStep: 'record',
        postedAmount: 0,
        recordedAt: '2026-11-28T10:00:00Z',
        sequence,
        status: 'pending',
      };
      transactions.push(transaction);
      return HttpResponse.json(transactionDto(transaction), { status: 201 });
    }),
    http.put('*/api/transactions/:id', async ({ params, request }) => {
      const transaction = findTransaction(params.id);
      if (!transaction) {
        return notFound();
      }

      if (transaction.status !== 'posted') {
        return conflict();
      }

      const body = (await request.json()) as TransactionBody;
      Object.assign(transaction, body, { declineReason: null, pendingStep: 'change', status: 'pending' });
      return new HttpResponse(null, { status: 202 });
    }),
    http.delete('*/api/transactions/:id', ({ params }) => {
      const transaction = findTransaction(params.id);
      if (!transaction) {
        return notFound();
      }

      if (transaction.status === 'pending') {
        return conflict();
      }

      if (transaction.status === 'rejected') {
        transaction.status = 'deleted';
        return new HttpResponse(null, { status: 204 });
      }

      Object.assign(transaction, { pendingStep: 'delete', status: 'pending' });
      return new HttpResponse(null, { status: 202 });
    }),
  );

  return {
    /** Adds an account directly, as if opened earlier. */
    addAccount(account: Partial<FakeAccount> & Pick<FakeAccount, 'name'>) {
      const created = {
        closedAt: null,
        currency: 'PLN',
        id: `account-${accounts.length + 1}`,
        openedAt: '2026-11-01T10:00:00Z',
        openingBalance: 0,
        type: 'checking',
        ...account,
      };
      accounts.push(created);
      return created;
    },
    /** Adds a posted transaction directly, or one Accounts rejected because the account was closed. */
    addTransaction(accountId: string, amount: number, bookedOn: string, description: string, rejected = false) {
      sequence += 1;
      transactions.push({
        accountId,
        amount,
        bookedOn,
        declineReason: rejected ? 'account_closed' : null,
        description,
        id: `transaction-${sequence}`,
        note: null,
        pendingStep: null,
        postedAmount: rejected ? 0 : amount,
        recordedAt: '2026-11-28T10:00:00Z',
        sequence,
        status: rejected ? 'rejected' : 'posted',
      });
    },
    closeAccount(id: string) {
      const account = findAccount(id);
      if (account) {
        account.closedAt = '2026-11-20T10:00:00Z';
      }
    },
  };
}
