import { useQuery } from '@tanstack/react-query';

import { StatusNotice } from '@/shared/ui/StatusNotice';

import { sessionsQueryOptions } from '../api/identityQueries';
import { useAuth } from '../model/authContext';

import styles from './SessionSummary.module.scss';

export function SessionSummary() {
  const auth = useAuth();
  const sessionsQuery = useQuery(sessionsQueryOptions(auth.isAuthenticated ? auth.sessionId : null));

  return (
    <section className={styles.card}>
      <div className={styles.header}>
        <div>
          <p className={styles.eyebrow}>Identity status</p>
          <h2>Current session</h2>
        </div>
      </div>

      {sessionsQuery.isLoading ? <StatusNotice>Loading session details…</StatusNotice> : null}
      {sessionsQuery.isError ? (
        <StatusNotice tone="error">
          {sessionsQuery.error instanceof Error ? sessionsQuery.error.message : 'Unable to load session details.'}
        </StatusNotice>
      ) : null}

      {sessionsQuery.data?.length ? (
        <ul className={styles.sessionList}>
          {sessionsQuery.data.map((session) => (
            <li className={styles.sessionCard} key={session.id}>
              <p className={styles.sessionTitle}>{session.isCurrent ? 'Current browser session' : 'Vaultly session'}</p>
              <dl className={styles.sessionDetails}>
                <div>
                  <dt>Last used</dt>
                  <dd>{new Date(session.lastUsedAt).toLocaleString()}</dd>
                </div>
                <div>
                  <dt>Created</dt>
                  <dd>{new Date(session.createdAt).toLocaleString()}</dd>
                </div>
                <div>
                  <dt>Agent</dt>
                  <dd>{session.userAgent ?? 'Unavailable'}</dd>
                </div>
              </dl>
            </li>
          ))}
        </ul>
      ) : null}
    </section>
  );
}
