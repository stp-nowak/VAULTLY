import { getRouteApi } from '@tanstack/react-router';

import { Card, CenteredScreen, Eyebrow } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

import { SignInRequest } from './SignInRequest';

const loginRoute = getRouteApi('/login');

/**
 * Sign-in page of the Identity service. A client app sends the browser here with `?request=<id>` (validated by
 * `app/routes/login.tsx`); the page shows the providers the user can continue with.
 */
export function LoginPage() {
  const { request } = loginRoute.useSearch();
  const { t } = useTranslation();

  return (
    <CenteredScreen>
      <Card aria-labelledby="sign-in-heading" size="hero">
        <Eyebrow>{t('brand.name')}</Eyebrow>
        <SignInRequest requestId={request} />
      </Card>
    </CenteredScreen>
  );
}
