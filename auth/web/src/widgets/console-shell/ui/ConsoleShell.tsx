import { Link } from '@tanstack/react-router';

import { BrandMark, Eyebrow } from '@vaultly/ui';

import { consoleRoutes, type ConsoleAdmin } from '@/domain/console-admin';

interface ConsoleShellProps {
  admin: ConsoleAdmin;
  children: React.ReactNode;
}

const navLink =
  'rounded-xl border border-transparent px-3 py-2 text-muted transition-colors hover:bg-surface-raised hover:text-ink lg:px-4 lg:py-2.5';

/**
 * Frame of every admin console page: a sidebar with the brand, navigation and the signed-in admin. On narrow
 * screens it is a single bar above the content.
 */
export function ConsoleShell({ admin, children }: ConsoleShellProps) {
  return (
    <div className="grid min-h-screen grid-cols-1 grid-rows-[auto_1fr] lg:grid-cols-[16rem_minmax(0,1fr)] lg:grid-rows-1">
      <aside className="flex items-center gap-4 border-b border-line bg-sidebar px-4 py-3 backdrop-blur-lg lg:sticky lg:top-0 lg:h-screen lg:flex-col lg:items-stretch lg:gap-8 lg:border-r lg:border-b-0 lg:p-6">
        <div className="flex items-center gap-3">
          <BrandMark />
          <p className="grid leading-tight">
            <Eyebrow className="hidden lg:block">Vaultly</Eyebrow>
            <span className="font-display text-lg font-semibold tracking-display whitespace-nowrap lg:text-xl">
              Admin console
            </span>
          </p>
        </div>

        <nav aria-label="Admin console" className="flex gap-1.5 lg:grid">
          <Link activeProps={{ className: 'border-line-strong bg-accent-soft text-ink' }} className={navLink} to="/admin/users">
            Users
          </Link>
        </nav>

        <div className="ml-auto grid gap-2 text-sm lg:mt-auto lg:ml-0 lg:border-t lg:border-line lg:pt-5">
          <span className="hidden break-all text-muted lg:block">{admin.email}</span>
          <a
            className="justify-self-start border-b border-line-strong pb-0.5 whitespace-nowrap hover:border-accent"
            href={consoleRoutes.signOut}
          >
            Sign out
          </a>
        </div>
      </aside>

      <main className="min-w-0 px-6 py-10 lg:px-10">
        <div className="max-w-300">{children}</div>
      </main>
    </div>
  );
}
