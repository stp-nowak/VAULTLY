import { SessionSummary } from '@/domain/identity';
import { LogoutCard } from '@/features/logout';
import { AppShell } from '@/widgets/app-shell';

import styles from './DashboardPage.module.scss';

export function DashboardPage() {
  return (
    <AppShell>
      <div className={styles.contentStack}>
        <header className={styles.contentHero}>
          <p className={styles.eyebrow}>Authenticated view</p>
          <h2>Minimal shell, explicit auth states, and session controls.</h2>
          <p className={styles.lede}>
            This screen intentionally stays lean for issue #3: sidebar, settings, logout, and clear identity-session
            feedback.
          </p>
        </header>

        <SessionSummary />
        <LogoutCard />
      </div>
    </AppShell>
  );
}
