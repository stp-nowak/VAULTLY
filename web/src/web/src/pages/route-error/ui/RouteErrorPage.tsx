import { Card, CenteredScreen, Eyebrow, StatusNotice } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

export type RouteErrorKind = 'notFound' | 'unexpected';

interface RouteErrorPageProps {
  kind: RouteErrorKind;
}

export function RouteErrorPage({ kind }: RouteErrorPageProps) {
  const { t } = useTranslation(['pages/route-error', 'common']);

  return (
    <CenteredScreen>
      <Card size="hero">
        <Eyebrow>{t('common:brand.name')}</Eyebrow>
        <h1 className="text-3xl">{t('common:errors.unexpected')}</h1>
        <StatusNotice tone="error">{t(`messages.${kind}`)}</StatusNotice>
      </Card>
    </CenteredScreen>
  );
}
