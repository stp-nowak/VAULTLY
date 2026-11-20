import { useAuth } from '@/domain/identity';
import { Button } from '@/shared/ui/Button';
import { StatusNotice } from '@/shared/ui/StatusNotice';

import styles from './LoginPanel.module.scss';

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
    <main className={styles.screen}>
      <section className={styles.card}>
        <p className={styles.eyebrow}>Vaultly</p>
        <h1>Sign in to continue.</h1>
        <p className={styles.lede}>
          Sign-in is handled by the Vaultly web API. Your browser only keeps a secure, HttpOnly session cookie — access
          tokens never reach this page.
        </p>

        {callbackError ? <StatusNotice tone="error">{callbackError}</StatusNotice> : null}
        {auth.errorMessage ? <StatusNotice tone="error">{auth.errorMessage}</StatusNotice> : null}

        <Button disabled={auth.status === 'checking'} onClick={() => auth.login(returnUrl)}>
          Continue with Google
        </Button>
      </section>
    </main>
  );
}
