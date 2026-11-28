import { Eyebrow } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

import { SessionSummary } from '@/domain/identity';

export function SettingsPage() {
  const { t } = useTranslation('pages/settings');

  return (
    <div className="grid gap-6">
      <header className="grid gap-3.5 pt-5 pb-2">
        <Eyebrow>{t('eyebrow')}</Eyebrow>
        <h2 className="text-[clamp(2rem,4vw,3rem)] leading-tight">{t('heading')}</h2>
        <p className="text-muted">{t('intro')}</p>
      </header>

      <SessionSummary />
    </div>
  );
}
