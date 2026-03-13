import { useEffect, useRef, useState } from 'react';
import { useNavigate, useSearch } from '@tanstack/react-router';

import { StatusNotice } from '@/components/ui/StatusNotice';
import { useAuth } from '@/features/auth/context/useAuth';

import styles from './CallbackRouteView.module.scss';

export function CallbackRouteView() {
  const auth = useAuth();
  const navigate = useNavigate();
  const search = useSearch({ from: '/callback' });
  const { code, error, state } = search;
  const immediateError =
    error || (!code || !state ? 'The identity service did not return the expected authorization code and state.' : null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const didStart = useRef(false);

  useEffect(() => {
    if (didStart.current) {
      return;
    }

    didStart.current = true;

    if (immediateError || !code || !state) {
      return;
    }

    void auth
      .completeLogin({ code, state })
      .then(() => navigate({ replace: true, to: '/app' }))
      .catch((loginError: unknown) => {
        setErrorMessage(loginError instanceof Error ? loginError.message : 'Unable to complete the login flow.');
      });
  }, [auth, code, immediateError, navigate, state]);

  return (
    <main className={styles.screen}>
      <section className={styles.card}>
        <p className={styles.eyebrow}>Redirected authentication</p>
        <h1>Finalizing your session.</h1>
        {immediateError || errorMessage ? (
          <StatusNotice tone="error">{immediateError ?? errorMessage}</StatusNotice>
        ) : (
          <StatusNotice>Exchanging the returned authorization code for a Vaultly access token…</StatusNotice>
        )}
      </section>
    </main>
  );
}
