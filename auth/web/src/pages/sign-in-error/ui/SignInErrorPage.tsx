import { getRouteApi } from '@tanstack/react-router';

import { Card, CenteredScreen, Eyebrow, StatusNotice } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

import { toSignInErrorCode } from '../lib/signInErrorCode';

const errorRoute = getRouteApi('/error');

/** Shown when sign-in fails on the server side; `?code=` (validated by `app/routes/error.tsx`) selects the message. */
export function SignInErrorPage() {
  const { code } = errorRoute.useSearch();
  const { t } = useTranslation(['pages/sign-in-error', 'common']);

  return (
    <CenteredScreen>
      <Card aria-labelledby="sign-in-error-heading" size="hero">
        <Eyebrow>{t('common:brand.name')}</Eyebrow>
        <h1 className="text-3xl" id="sign-in-error-heading">
          {t('heading')}
        </h1>
        <StatusNotice tone="error">{t(`errors.${toSignInErrorCode(code)}`)}</StatusNotice>
      </Card>
    </CenteredScreen>
  );
}
