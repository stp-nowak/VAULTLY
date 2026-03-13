import { Button } from '@/components/ui/Button';
import { StatusNotice } from '@/components/ui/StatusNotice';
import { useAuth } from '@/features/auth/context/useAuth';
import { useIdentityDiscovery } from '@/features/auth/hooks/useIdentityDiscovery';
import { appConfig } from '@/services/config';

import styles from './LoginPanel.module.scss';

export function LoginPanel() {
  const auth = useAuth();
  const discoveryQuery = useIdentityDiscovery();

  const disabled = auth.status === 'checking' || discoveryQuery.isLoading || !discoveryQuery.data;

  return (
    <main className={styles.screen}>
      <section className={styles.card}>
        <p className={styles.eyebrow}>Vaultly authentication shell</p>
        <h1>Sign in through the identity service.</h1>
        <p className={styles.lede}>
          This frontend uses the discovery document, PKCE, short-lived access tokens, and cookie-backed refresh
          sessions described in the Vaultly auth flow.
        </p>

        <dl className={styles.configGrid}>
          <div>
            <dt>Identity host</dt>
            <dd>{appConfig.identityBaseUrl || 'Missing IDENTITY_BASE_URL'}</dd>
          </div>
          <div>
            <dt>Callback path</dt>
            <dd>{appConfig.callbackPath}</dd>
          </div>
        </dl>

        {auth.errorMessage ? <StatusNotice tone="error">{auth.errorMessage}</StatusNotice> : null}
        {discoveryQuery.isError ? (
          <StatusNotice tone="error">
            {discoveryQuery.error instanceof Error
              ? discoveryQuery.error.message
              : 'Unable to load identity discovery metadata.'}
          </StatusNotice>
        ) : null}
        {discoveryQuery.isLoading ? <StatusNotice>Loading identity discovery metadata…</StatusNotice> : null}

        <Button
          disabled={disabled}
          onClick={() => {
            if (!discoveryQuery.data) {
              return;
            }

            void auth.beginLogin(discoveryQuery.data);
          }}
        >
          Continue with identity
        </Button>
      </section>
    </main>
  );
}
