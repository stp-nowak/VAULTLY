import { Card, CenteredScreen, Eyebrow, StatusNotice } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

import { toSignInError } from '../model/signInError';
import { LoginButton } from './LoginButton';

interface LoginPanelProps {
  error?: string;
  returnUrl?: string;
}

export function LoginPanel({ error, returnUrl }: LoginPanelProps) {
  const { t } = useTranslation(['features/login', 'common']);

  return (
    <CenteredScreen>
      <Card size="hero">
        <Eyebrow>{t('common:brand.name')}</Eyebrow>
        <h1 className="text-3xl">{t('failedHeading')}</h1>
        <StatusNotice tone="error">{t(`errors.${toSignInError(error)}`)}</StatusNotice>
        <LoginButton returnUrl={returnUrl}>{t('common:actions.tryAgain')}</LoginButton>
      </Card>
    </CenteredScreen>
  );
}
