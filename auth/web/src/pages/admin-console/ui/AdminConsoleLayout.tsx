import { useSuspenseQuery } from '@tanstack/react-query';
import { Outlet } from '@tanstack/react-router';
import { useEffect } from 'react';

import { Card, CenteredScreen, Eyebrow, StatusNotice } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

import { consoleAccessQueryOptions, consoleRoutes, type ConsoleAccessStatus } from '@/domain/console-admin';
import { SignOutLink } from '@/features/sign-out';
import { navigateToUrl } from '@/shared/lib';
import { ConsoleShell } from '@/widgets/console-shell';

interface AccessNotice {
  key: 'forbidden' | 'signedOut';
  showSignOut: boolean;
}

const notices: Record<Exclude<ConsoleAccessStatus, 'admin'>, AccessNotice> = {
  forbidden: { key: 'forbidden', showSignOut: true },
  'signed-out': { key: 'signedOut', showSignOut: false },
};

/**
 * Gate of the admin console (`/admin/*`): admins get the console frame, other signed-in users a "no access" page,
 * and a browser without a session is sent to sign in.
 */
export function AdminConsoleLayout() {
  const { t } = useTranslation('pages/admin-console');
  const { data: access } = useSuspenseQuery(consoleAccessQueryOptions());

  useEffect(() => {
    if (access.status === 'signed-out') {
      navigateToUrl(consoleRoutes.signIn);
    }
  }, [access.status]);

  return (
    <>
      {access.status === 'admin' && (
        <ConsoleShell admin={access.admin}>
          <Outlet />
        </ConsoleShell>
      )}
      {access.status !== 'admin' && (
        <CenteredScreen>
          <Card aria-labelledby="console-access-heading" size="hero">
            <Eyebrow>{t('eyebrow')}</Eyebrow>
            <h1 className="text-3xl" id="console-access-heading">
              {t(`notices.${notices[access.status].key}.heading`)}
            </h1>
            <StatusNotice>{t(`notices.${notices[access.status].key}.text`)}</StatusNotice>
            {notices[access.status].showSignOut && <SignOutLink />}
          </Card>
        </CenteredScreen>
      )}
    </>
  );
}
