import { useQuery } from '@tanstack/react-query';
import { Link } from '@tanstack/react-router';
import { Card, Eyebrow, Select, StatusNotice, TextField } from '@vaultly/ui';
import { useId } from 'react';
import { useTranslation } from 'react-i18next';

import { accountsQueryOptions } from '@/domain/account';
import type { TransactionFilters } from '@/domain/transaction';
import { RecordTransactionButton } from '@/features/transaction/record';
import { TransactionList } from '@/widgets/transaction-list';

interface TransactionsPageProps {
  filters: TransactionFilters;
  onFiltersChange: (filters: TransactionFilters) => void;
}

const allAccounts = '';

export function TransactionsPage({ filters, onFiltersChange }: TransactionsPageProps) {
  const { t } = useTranslation('pages/transactions');
  const accountLabelId = useId();
  const allAccountsQuery = useQuery(accountsQueryOptions(true));
  const accounts = allAccountsQuery.data ?? [];
  const openAccounts = accounts.filter((account) => account.closedAt === null);
  const change = (key: keyof TransactionFilters, value: string) => onFiltersChange({ ...filters, [key]: value || undefined });

  return (
    <div className="grid gap-6">
      <header className="grid gap-3.5 pt-5 pb-2">
        <Eyebrow>{t('eyebrow')}</Eyebrow>
        <h2 className="text-[clamp(2rem,4vw,3rem)] leading-tight">{t('heading')}</h2>
        <p className="text-muted">{t('intro')}</p>
        {openAccounts.length > 0 && <RecordTransactionButton accounts={openAccounts} className="justify-self-start" />}
        {allAccountsQuery.isSuccess && openAccounts.length === 0 && (
          <StatusNotice>
            {t('noAccounts')}{' '}
            <Link className="underline" to="/app/accounts">
              {t('openAccount')}
            </Link>
          </StatusNotice>
        )}
      </header>

      <Card aria-labelledby="transactions-heading">
        <h3 className="text-2xl" id="transactions-heading">
          {t('list')}
        </h3>
        <div className="grid gap-4 sm:grid-cols-3" role="search">
          <div className="grid gap-1.5">
            <span className="text-xs font-bold tracking-label text-muted uppercase" id={accountLabelId}>
              {t('filters.account')}
            </span>
            <Select
              aria-labelledby={accountLabelId}
              onValueChange={(accountId) => change('accountId', accountId)}
              options={[
                { label: t('filters.allAccounts'), value: allAccounts },
                ...accounts.map((account) => ({ label: account.name, value: account.id })),
              ]}
              value={filters.accountId ?? allAccounts}
            />
          </div>
          <TextField
            label={t('filters.from')}
            max={filters.to}
            onChange={(event) => change('from', event.target.value)}
            type="date"
            value={filters.from ?? ''}
          />
          <TextField
            label={t('filters.to')}
            min={filters.from}
            onChange={(event) => change('to', event.target.value)}
            type="date"
            value={filters.to ?? ''}
          />
        </div>
        <TransactionList accounts={accounts} filters={filters} />
      </Card>
    </div>
  );
}
