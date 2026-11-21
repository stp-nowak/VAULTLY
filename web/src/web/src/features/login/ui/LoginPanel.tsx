import { Button, Card, CenteredScreen, Eyebrow, StatusNotice } from '@vaultly/ui';

import { useAuth } from '@/domain/identity';

/** Error codes the web API puts on `/login?error=` when the OAuth callback fails. */
const loginErrorMessages: Record<string, string> = {
  invalid_state: 'The sign-in attempt expired or was not started from this browser. Please try again.',
  login_failed: 'Sign-in did not complete. Please try again.',
};

interface LoginPanelProps {
  error?: string;
  returnUrl?: string;
}

export function LoginPanel({ error, returnUrl }: LoginPanelProps) {
  const auth = useAuth();
  const callbackError = error ? (loginErrorMessages[error] ?? loginErrorMessages.login_failed) : null;

  return (
    <CenteredScreen>
      <Card size="hero">
        <Eyebrow>Vaultly</Eyebrow>
        <h1 className="text-3xl">Sign in to continue.</h1>

        {callbackError ? <StatusNotice tone="error">{callbackError}</StatusNotice> : null}
        {auth.errorMessage ? <StatusNotice tone="error">{auth.errorMessage}</StatusNotice> : null}

        <Button disabled={auth.status === 'checking'} onClick={() => auth.login(returnUrl)}>
          Continue with Google
        </Button>
      </Card>
    </CenteredScreen>
  );
}
