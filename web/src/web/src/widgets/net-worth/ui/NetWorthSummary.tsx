import { useQuery } from '@tanstack/react-query';
import { Link } from '@tanstack/react-router';
import { Card, InfoTile, StatusNotice } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

import { netWorthQueryOptions } from '@/domain/account';
import { useIntlLocale } from '@/shared/i18n';
import { formatMoney } from '@/shared/lib';

interface NetWorthSummaryProps {
  /** Shows only the first currencies and a link to all of them. */
  limit?: number;
}

/** Net worth, assets and liabilities in each currency of the open accounts. */
export function NetWorthSummary({ limit }: NetWorthSummaryProps) {
  const { t } = useTranslation('widgets/net-worth');
  const locale = useIntlLocale();
  const query = useQuery(netWorthQueryOptions());
  const all = query.data ?? [];
  const currencies = limit === undefined ? all : all.slice(0, limit);

  return (
    <Card aria-labelledby="net-worth-heading">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <h3 className="text-2xl" id="net-worth-heading">
          {t('heading')}
        </h3>
        {currencies.length < all.length && (
          <Link className="text-sm text-accent hover:underline" to="/app/net-worth">
            {t('viewAll', { count: all.length })}
          </Link>
        )}
      </div>
      {query.isPending && <StatusNotice>{t('loading')}</StatusNotice>}
      {query.isError && <StatusNotice tone="error">{t('loadFailed')}</StatusNotice>}
      {query.isSuccess && all.length === 0 && <StatusNotice>{t('empty')}</StatusNotice>}
      {currencies.map((worth) => (
        <dl className="grid gap-3 sm:grid-cols-3" key={worth.currency}>
          <InfoTile
            label={t('netWorth', { currency: worth.currency })}
            value={<strong className="font-display text-2xl">{formatMoney(worth.netWorth, worth.currency, locale)}</strong>}
          />
          <InfoTile label={t('assets')} value={formatMoney(worth.assets, worth.currency, locale)} />
          <InfoTile label={t('liabilities')} value={formatMoney(worth.liabilities, worth.currency, locale)} />
        </dl>
      ))}
    </Card>
  );
}
