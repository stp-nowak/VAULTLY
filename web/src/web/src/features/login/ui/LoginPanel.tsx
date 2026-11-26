import { Card, CenteredScreen, Eyebrow, StatusNotice } from '@vaultly/ui';

import { LoginButton } from './LoginButton';

/** Error codes the web API puts on `/login?error=` when the OAuth callback fails. */
const errorMessages: Record<string, string> = {
  invalid_state: 'The sign-in attempt expired or was not started from this browser.',
  login_failed: 'Something went wrong while signing you in.',
};

interface LoginPanelProps {
  error?: string;
  returnUrl?: string;
}

export function LoginPanel({ error = 'login_failed', returnUrl }: LoginPanelProps) {
  return (
    <CenteredScreen>
      <Card size="hero">
        <Eyebrow>Vaultly</Eyebrow>
        <h1 className="text-3xl">Sign-in didn&apos;t complete.</h1>
        <StatusNotice tone="error">{errorMessages[error] ?? errorMessages.login_failed}</StatusNotice>
        <LoginButton returnUrl={returnUrl}>Try again</LoginButton>
      </Card>
    </CenteredScreen>
  );
}
