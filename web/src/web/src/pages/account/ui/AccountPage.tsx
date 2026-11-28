import { useQuery } from '@tanstack/react-query';
import { Link } from '@tanstack/react-router';
import { Button, Card, Eyebrow, StatusNotice } from '@vaultly/ui';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { accountQueryOptions, AccountTypeName, balanceHistoryQueryOptions, type Account } from '@/domain/account';
import { CloseAccountButton } from '@/features/account/close';
import { RecordTransactionButton } from '@/features/transaction/record';
import { RenameAccountForm } from '@/features/account/rename';
import { statusOf } from '@/shared/api';
import { useIntlLocale } from '@/shared/i18n';
import { formatMoney, today } from '@/shared/lib';
import { TransactionList } from '@/widgets/transaction-list';

import { seriesOf } from '../model/balanceChart';
import { BalanceChart } from './BalanceChart';

interface AccountPageProps {
  accountId: string;
}

export function AccountPage({ accountId }: AccountPageProps) {
  const { t } = useTranslation('pages/account');
  const accountQuery = useQuery(accountQueryOptions(accountId));
  const failure = statusOf(accountQuery.error) === 404 ? t('notFound') : t('loadFailed');

  return (
    <div className="grid gap-6">
      <Link className="justify-self-start text-sm text-muted hover:text-ink" to="/app/accounts">
        {t('back')}
      </Link>
      {accountQuery.isPending && <StatusNotice>{t('loading')}</StatusNotice>}
      {accountQuery.isError && <StatusNotice tone="error">{failure}</StatusNotice>}
      {accountQuery.data && <AccountDetails account={accountQuery.data} />}
    </div>
  );
}

function AccountDetails({ account }: { account: Account }) {
  const { t } = useTranslation('pages/account');
  const locale = useIntlLocale();
  const [isRenaming, setIsRenaming] = useState(false);
  const historyQuery = useQuery(balanceHistoryQueryOptions(account.id));
  const isOpen = account.closedAt === null;
  const opening = { balance: account.openingBalance, date: account.openedAt.slice(0, 10) };
  const series = seriesOf(historyQuery.data ?? [], opening, today());

  return (
    <>
      <header className="grid gap-3.5 pb-2">
        <Eyebrow>
          <AccountTypeName account={account} />
        </Eyebrow>
        <h2 className="text-[clamp(2rem,4vw,3rem)] leading-tight">{account.name}</h2>
        <p className="font-display text-4xl tabular-nums">{formatMoney(account.balance, account.currency, locale)}</p>
        <p className="text-muted">
          {t('openingBalance', { amount: formatMoney(account.openingBalance, account.currency, locale) })}
        </p>
        {isOpen && !isRenaming && (
          <div className="flex flex-wrap items-start gap-3">
            <RecordTransactionButton accountId={account.id} accounts={[account]} />
            <Button onClick={() => setIsRenaming(true)} variant="secondary">
              {t('rename')}
            </Button>
            <CloseAccountButton account={account} />
          </div>
        )}
        {isRenaming && <RenameAccountForm account={account} onDone={() => setIsRenaming(false)} />}
      </header>

      <Card aria-labelledby="balance-history-heading">
        <h3 className="text-2xl" id="balance-history-heading">
          {t('history.heading')}
        </h3>
        {historyQuery.isPending && <StatusNotice>{t('history.loading')}</StatusNotice>}
        {historyQuery.isError && <StatusNotice tone="error">{t('history.loadFailed')}</StatusNotice>}
        {historyQuery.isSuccess && <BalanceChart currency={account.currency} series={series} />}
      </Card>

      <Card aria-labelledby="account-transactions-heading">
        <h3 className="text-2xl" id="account-transactions-heading">
          {t('transactions')}
        </h3>
        <TransactionList accounts={[account]} filters={{ accountId: account.id }} />
      </Card>
    </>
  );
}
