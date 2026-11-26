import { useSuspenseQuery } from '@tanstack/react-query';
import { Outlet } from '@tanstack/react-router';
import { useEffect } from 'react';

import { Card, CenteredScreen, Eyebrow, StatusNotice } from '@vaultly/ui';

import { consoleAccessQueryOptions, consoleRoutes, type ConsoleAccessStatus } from '@/domain/console-admin';
import { navigateToUrl } from '@/shared/lib';
import { ConsoleShell } from '@/widgets/console-shell';

interface AccessNotice {
  heading: string;
  showSignOut: boolean;
  text: string;
}

const notices: Record<Exclude<ConsoleAccessStatus, 'admin'>, AccessNotice> = {
  forbidden: {
    heading: 'No access',
    showSignOut: true,
    text: "Your account can't use the admin console. Ask an admin to give you the admin role, or sign out and use another account.",
  },
  'signed-out': {
    heading: 'Signing you in…',
    showSignOut: false,
    text: 'Taking you to the sign-in page.',
  },
};

/**
 * Gate of the admin console (`/admin/*`): admins get the console frame, other signed-in users a "no access" page,
 * and a browser without a session is sent to sign in.
 */
export function AdminConsoleLayout() {
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
            <Eyebrow>Vaultly Admin</Eyebrow>
            <h1 className="text-3xl" id="console-access-heading">
              {notices[access.status].heading}
            </h1>
            <StatusNotice>{notices[access.status].text}</StatusNotice>
            {notices[access.status].showSignOut && (
              <a className="justify-self-start border-b border-line-strong pb-0.5 hover:border-accent" href={consoleRoutes.signOut}>
                Sign out
              </a>
            )}
          </Card>
        </CenteredScreen>
      )}
    </>
  );
}
