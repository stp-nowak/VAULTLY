import { Card, CenteredScreen, Eyebrow, StatusNotice } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

export type RouteErrorKind = 'notFound' | 'unexpected';

interface RouteErrorPageProps {
  kind: RouteErrorKind;
}

/** Fallback page for unexpected route errors and unknown URLs. Raw error messages are never shown. */
export function RouteErrorPage({ kind }: RouteErrorPageProps) {
  const { t } = useTranslation(['pages/route-error', 'common']);

  return (
    <CenteredScreen>
      <Card aria-labelledby="route-error-heading" size="hero">
        <Eyebrow>{t('common:brand.name')}</Eyebrow>
        <h1 className="text-3xl" id="route-error-heading">
          {t('common:errors.unexpected')}
        </h1>
        <StatusNotice tone="error">{t(`messages.${kind}`)}</StatusNotice>
      </Card>
    </CenteredScreen>
  );
}
