import { getRouteApi } from '@tanstack/react-router';

import { Card, CenteredScreen, Eyebrow, StatusNotice } from '@vaultly/ui';

import { signInErrorMessage } from '../lib/signInErrorMessage';

const errorRoute = getRouteApi('/error');

/** Shown when sign-in fails on the server side; `?code=` (validated by `app/routes/error.tsx`) selects the message. */
export function SignInErrorPage() {
  const { code } = errorRoute.useSearch();

  return (
    <CenteredScreen>
      <Card aria-labelledby="sign-in-error-heading" size="hero">
        <Eyebrow>Vaultly</Eyebrow>
        <h1 className="text-3xl" id="sign-in-error-heading">
          Sign-in didn&apos;t complete
        </h1>
        <StatusNotice tone="error">{signInErrorMessage(code)}</StatusNotice>
      </Card>
    </CenteredScreen>
  );
}
