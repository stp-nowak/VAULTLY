import { useState } from 'react';

import { useAuth } from '@/domain/identity';
import { Button } from '@/shared/ui/Button';
import { StatusNotice } from '@/shared/ui/StatusNotice';

import styles from './LogoutCard.module.scss';

export function LogoutCard() {
  const auth = useAuth();
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  return (
    <section className={styles.card}>
      <div className={styles.header}>
        <div>
          <p className={styles.eyebrow}>Settings</p>
          <h2>Session controls</h2>
        </div>
      </div>

      <div className={styles.settingsList}>
        <div className={styles.settingsRow}>
          <span>Signed in as</span>
          <strong>{auth.user?.email ?? 'Unknown user'}</strong>
        </div>
      </div>

      {errorMessage ? <StatusNotice tone="error">{errorMessage}</StatusNotice> : null}

      <Button
        disabled={isSubmitting}
        onClick={() => {
          setErrorMessage(null);
          setIsSubmitting(true);

          void auth
            .logout()
            .catch((error: unknown) => {
              setErrorMessage(error instanceof Error ? error.message : 'Logout failed.');
            })
            .finally(() => {
              setIsSubmitting(false);
            });
        }}
        variant="secondary"
      >
        {isSubmitting ? 'Signing out…' : 'Logout'}
      </Button>
    </section>
  );
}
