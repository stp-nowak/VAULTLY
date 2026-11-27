import { Card, CenteredScreen, Eyebrow } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

/** Final page after signing out of the Identity service. */
export function LoggedOutPage() {
  const { t } = useTranslation(['pages/logged-out', 'common']);

  return (
    <CenteredScreen>
      <Card aria-labelledby="logged-out-heading" size="hero">
        <Eyebrow>{t('common:brand.name')}</Eyebrow>
        <h1 className="text-3xl" id="logged-out-heading">
          {t('heading')}
        </h1>
        <p className="text-muted">{t('body')}</p>
      </Card>
    </CenteredScreen>
  );
}
