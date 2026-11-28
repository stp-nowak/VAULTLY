import { useQuery } from '@tanstack/react-query';
import { Link } from '@tanstack/react-router';
import { Card, cn, StatusNotice, Switch } from '@vaultly/ui';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { accountsQueryOptions, AccountTypeName } from '@/domain/account';
import { OpenAccountButton } from '@/features/account/open';
import { useIntlLocale } from '@/shared/i18n';
import { formatMoney } from '@/shared/lib';

interface AccountListProps {
  /** Shows only the first accounts and a link to all of them; without it every account and the closed ones on request. */
  limit?: number;
}

/** The user's accounts with their balances, each linking to its page, and opening a new one. */
export function AccountList({ limit }: AccountListProps) {
  const { t } = useTranslation('widgets/account-list');
  const locale = useIntlLocale();
  const [showClosed, setShowClosed] = useState(false);
  const accountsQuery = useQuery(accountsQueryOptions(showClosed));
  const all = accountsQuery.data ?? [];
  const accounts = limit === undefined ? all : all.slice(0, limit);
  const hasMore = accounts.length < all.length;

  return (
    <Card aria-labelledby="accounts-heading">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <h3 className="text-2xl" id="accounts-heading">
          {t('heading')}
        </h3>
        {limit === undefined && (
          <label className="flex items-center gap-3 text-sm text-muted">
            {t('showClosed')}
            <Switch aria-label={t('showClosed')} checked={showClosed} onCheckedChange={setShowClosed} />
          </label>
        )}
        {hasMore && (
          <Link className="text-sm text-accent hover:underline" to="/app/accounts">
            {t('viewAll', { count: all.length })}
          </Link>
        )}
      </div>

      {accountsQuery.isPending && <StatusNotice>{t('loading')}</StatusNotice>}
      {accountsQuery.isError && <StatusNotice tone="error">{t('loadFailed')}</StatusNotice>}
      {accountsQuery.isSuccess && all.length === 0 && <StatusNotice>{t('empty')}</StatusNotice>}

      {accounts.length > 0 && (
        <ul className="grid gap-3">
          {accounts.map((account) => (
            <li key={account.id}>
              <Link
                className={cn(
                  'flex items-center justify-between gap-4 rounded-2xl border border-line bg-surface px-4 py-3.5',
                  'transition-colors hover:border-line-strong hover:bg-surface-raised',
                  account.closedAt !== null && 'text-muted',
                )}
                params={{ accountId: account.id }}
                to="/app/accounts/$accountId"
              >
                <span className="grid gap-0.5">
                  <span className="font-bold">{account.name}</span>
                  <span className="text-xs tracking-label text-muted uppercase">
                    <AccountTypeName account={account} />
                  </span>
                </span>
                <span className={cn('font-display text-xl tabular-nums', account.balance < 0 && 'text-danger')}>
                  {formatMoney(account.balance, account.currency, locale)}
                </span>
              </Link>
            </li>
          ))}
        </ul>
      )}

      <OpenAccountButton className="justify-self-start" />
    </Card>
  );
}
