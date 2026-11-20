import { clsx } from 'clsx';
import { Link } from '@tanstack/react-router';

import styles from './AppShell.module.scss';

interface AppShellProps {
  children: React.ReactNode;
}

export function AppShell({ children }: AppShellProps) {
  return (
    <div className={styles.shell}>
      <aside className={styles.sidebar}>
        <div className={styles.brandBlock}>
          <p className={styles.kicker}>Vaultly</p>
          <h1 className={styles.title}>Know where every zloty goes.</h1>
          <p className={styles.copy}>
            The first authenticated screen stays intentionally minimal while the auth foundation settles.
          </p>
        </div>

        <nav aria-label="Primary" className={styles.nav}>
          <Link className={clsx(styles.navLink, styles.navLinkActive)} to="/app">
            Settings shell
          </Link>
          <span className={clsx(styles.navLink, styles.navLinkMuted)}>Transactions (soon)</span>
          <span className={clsx(styles.navLink, styles.navLinkMuted)}>Budgets (soon)</span>
        </nav>
      </aside>

      <main className={styles.content}>{children}</main>
    </div>
  );
}
