import { LogoutCard } from '@/features/auth/components/LogoutCard';
import { SessionSummary } from '@/features/auth/components/SessionSummary';
import { AppShell } from '@/layouts/AppShell';

import styles from './AppRouteView.module.scss';

export function AppRouteView() {
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
