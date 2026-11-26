import { Link } from '@tanstack/react-router';

import { Eyebrow } from '@vaultly/ui';

import { consoleRoutes, type ConsoleAdmin } from '@/domain/console-admin';

interface ConsoleShellProps {
  admin: ConsoleAdmin;
  children: React.ReactNode;
}

const navLink = 'rounded-full border border-transparent px-4 py-2 text-muted transition-colors hover:text-ink';

/** Frame of every admin console page: brand, navigation and the signed-in admin with a sign-out link. */
export function ConsoleShell({ admin, children }: ConsoleShellProps) {
  return (
    <div className="min-h-screen">
      <header className="sticky top-0 z-10 border-b border-line bg-sidebar backdrop-blur-lg">
        <div className="mx-auto flex max-w-300 flex-wrap items-center justify-between gap-x-8 gap-y-3 px-6 py-3.5 lg:px-8">
          <div className="flex flex-wrap items-center gap-x-8 gap-y-3">
            <p className="grid leading-tight">
              <Eyebrow>Vaultly</Eyebrow>
              <span className="font-display text-xl font-semibold tracking-display">Admin console</span>
            </p>
            <nav aria-label="Admin console">
              <Link activeProps={{ className: 'border-line-strong bg-accent-soft text-ink' }} className={navLink} to="/admin/users">
                Users
              </Link>
            </nav>
          </div>
          <div className="flex items-center gap-4 text-sm">
            <span className="text-muted">{admin.email}</span>
            <a className="border-b border-line-strong pb-0.5 hover:border-accent" href={consoleRoutes.signOut}>
              Sign out
            </a>
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-300 px-6 py-10 lg:px-8">{children}</main>
    </div>
  );
}
