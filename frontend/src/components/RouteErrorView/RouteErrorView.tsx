import { StatusNotice } from '@/components/ui/StatusNotice';

import styles from './RouteErrorView.module.scss';

export function RouteErrorView({ error }: { error: unknown }) {
  const message = error instanceof Error ? error.message : 'An unexpected routing error occurred.';

  return (
    <main className={styles.screen}>
      <section className={styles.card}>
        <p className={styles.eyebrow}>Route error</p>
        <h1>Something interrupted the flow.</h1>
        <StatusNotice tone="error">{message}</StatusNotice>
      </section>
    </main>
  );
}
