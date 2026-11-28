import { Eyebrow } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

import { useAuth } from '@/domain/identity';
import { AccountList } from '@/widgets/account-list';
import { NetWorthSummary } from '@/widgets/net-worth';

/** How many accounts and currencies the overview shows before linking to all of them. */
const overviewLimit = 3;

export function OverviewPage() {
  const auth = useAuth();
  const { t } = useTranslation('pages/overview');
  const firstName = auth.user?.name.split(' ')[0];

  return (
    <div className="grid gap-6">
      <header className="grid gap-3.5 pt-5 pb-2">
        <Eyebrow>{t('eyebrow')}</Eyebrow>
        <h2 className="text-[clamp(2rem,4vw,3rem)] leading-tight">
          {firstName ? t('welcomeNamed', { name: firstName }) : t('welcome')}
        </h2>
        <p className="text-muted">{t('intro')}</p>
      </header>

      <NetWorthSummary limit={overviewLimit} />
      <AccountList limit={overviewLimit} />
    </div>
  );
}
