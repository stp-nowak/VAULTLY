import { useInfiniteQuery } from '@tanstack/react-query';
import { Button, cn, StatusNotice, Table } from '@vaultly/ui';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import type { Account } from '@/domain/account';
import { TransactionStateBadge, transactionsQueryOptions, type Transaction, type TransactionFilters } from '@/domain/transaction';
import { ChangeTransactionButton } from '@/features/transaction/change';
import { DeleteTransactionButton } from '@/features/transaction/delete';
import { useIntlLocale } from '@/shared/i18n';
import { formatDay, formatMoney } from '@/shared/lib';

import { listStateOf } from '../model/listState';
import { useRefreshBalancesWhenPosted } from '../model/useRefreshBalancesWhenPosted';

interface TransactionListProps {
  /** The user's accounts, for their names; the account column is left out when the list shows one account. */
  accounts: Account[];
  filters: TransactionFilters;
}

const actionButton = 'min-h-9 px-3.5 text-sm';

// Below md each row becomes a card: description and amount on top, day and account under them, then the actions.
const cardsOnPhones = cn(
  'relative max-md:block max-md:[&_thead]:sr-only max-md:[&_tbody]:grid max-md:[&_tbody]:gap-3',
  'max-md:[&_tbody_tr]:grid max-md:[&_tbody_tr]:grid-cols-[minmax(0,1fr)_auto] max-md:[&_tbody_tr]:gap-x-4',
  'max-md:[&_tbody_tr]:gap-y-1.5 max-md:[&_tbody_tr]:rounded-2xl max-md:[&_tbody_tr]:border max-md:[&_tbody_tr]:border-line',
  'max-md:[&_tbody_tr]:bg-surface max-md:[&_tbody_tr]:p-4 max-md:[&_td]:border-0 max-md:[&_td]:p-0',
);

export function TransactionList({ accounts, filters }: TransactionListProps) {
  const { t } = useTranslation(['widgets/transaction-list', 'common']);
  const locale = useIntlLocale();
  const query = useInfiniteQuery(transactionsQueryOptions(filters));
  const [failure, setFailure] = useState<string | null>(null);
  const transactions = query.data?.pages.flatMap((page) => page.items) ?? [];
  const showAccount = filters.accountId === undefined;
  const accountName = (transaction: Transaction) =>
    accounts.find((account) => account.id === transaction.accountId)?.name ?? t('common:states.unknown');
  const notices = { empty: t('empty'), error: t('loadFailed'), list: null, loading: t('loading') };
  const state = listStateOf(query.status, transactions.length);

  useRefreshBalancesWhenPosted(query.data);

  return (
    <div className="grid min-w-0 gap-4">
      {notices[state] !== null && <StatusNotice tone={state === 'error' ? 'error' : 'info'}>{notices[state]}</StatusNotice>}
      {failure !== null && <StatusNotice tone="error">{failure}</StatusNotice>}

      {state === 'list' && (
        <Table aria-label={t('label')} className={cardsOnPhones}>
          <thead>
            <tr>
              <th scope="col">{t('columns.day')}</th>
              <th scope="col">{t('columns.description')}</th>
              {showAccount && <th scope="col">{t('columns.account')}</th>}
              <th className="text-right" scope="col">
                {t('columns.amount')}
              </th>
              <th scope="col">
                <span className="sr-only">{t('columns.actions')}</span>
              </th>
            </tr>
          </thead>
          <tbody>
            {transactions.map((transaction) => (
              <tr className={cn(transaction.status === 'rejected' && 'text-muted')} key={transaction.id}>
                <td className="whitespace-nowrap max-md:order-3 max-md:text-sm max-md:text-muted">{formatDay(transaction.bookedOn, locale)}</td>
                <td className="max-md:order-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <span>{transaction.description}</span>
                    <TransactionStateBadge transaction={transaction} />
                  </div>
                  {transaction.note !== null && <p className="mt-1 text-sm text-muted">{transaction.note}</p>}
                </td>
                {showAccount && <td className="max-md:order-4 max-md:text-right max-md:text-sm max-md:text-muted">{accountName(transaction)}</td>}
                <td
                  className={cn(
                    'text-right whitespace-nowrap tabular-nums max-md:order-2',
                    transaction.amount > 0 && 'text-success',
                    transaction.status === 'rejected' && 'line-through',
                  )}
                >
                  {formatMoney(transaction.amount, transaction.currency, locale)}
                </td>
                <td className="max-md:order-5 max-md:col-span-2">
                  <div className="flex justify-end gap-2 max-md:justify-start">
                    {transaction.status === 'posted' && (
                      <ChangeTransactionButton className={actionButton} transaction={transaction} />
                    )}
                    {transaction.status !== 'pending' && (
                      <DeleteTransactionButton onFailed={setFailure} transaction={transaction} />
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </Table>
      )}

      {query.hasNextPage && (
        <Button
          className="justify-self-center"
          disabled={query.isFetchingNextPage}
          onClick={() => void query.fetchNextPage()}
          variant="secondary"
        >
          {query.isFetchingNextPage ? t('loadingMore') : t('loadMore')}
        </Button>
      )}
    </div>
  );
}
