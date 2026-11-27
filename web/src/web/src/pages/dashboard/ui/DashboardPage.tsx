import { Eyebrow } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

import { SessionSummary, useAuth } from '@/domain/identity';
import { LogoutCard } from '@/features/logout';
import { AppShell } from '@/widgets/app-shell';

export function DashboardPage() {
  const auth = useAuth();
  const { t } = useTranslation('pages/dashboard');
  const firstName = auth.user?.name.split(' ')[0];

  return (
    <AppShell>
      <div className="grid max-w-240 gap-6">
        <header className="grid gap-3.5 pt-5 pb-2">
          <Eyebrow>{t('eyebrow')}</Eyebrow>
          <h2 className="text-[clamp(2rem,4vw,3rem)] leading-tight">
            {firstName ? t('welcomeNamed', { name: firstName }) : t('welcome')}
          </h2>
          <p className="text-muted">{t('intro')}</p>
        </header>

        <SessionSummary />
        <LogoutCard />
      </div>
    </AppShell>
  );
}
