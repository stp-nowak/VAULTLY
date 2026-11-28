import { Link } from '@tanstack/react-router';
import { BrandMark, cn } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

import { AccountsIcon } from './icons/AccountsIcon';
import { BudgetsIcon } from './icons/BudgetsIcon';
import { OverviewIcon } from './icons/OverviewIcon';
import { TransactionsIcon } from './icons/TransactionsIcon';
import { UserMenu } from './UserMenu';

interface AppShellProps {
  children: React.ReactNode;
}

/** Id of the element that scrolls the page content; the router scrolls it back to the top on navigation. */
export const appContentId = 'app-content';

const navLinks = [
  { exact: true, Icon: OverviewIcon, name: 'overview', to: '/app' },
  { exact: false, Icon: AccountsIcon, name: 'accounts', to: '/app/accounts' },
  { exact: false, Icon: TransactionsIcon, name: 'transactions', to: '/app/transactions' },
] as const;

const navItem = 'group flex flex-col items-center gap-1 rounded-2xl px-1 py-1.5 text-xs text-muted';
const navIcon = 'grid size-11 place-items-center rounded-xl transition-colors';

/**
 * Signed-in frame filling the window: a rail of icons (a bar at the bottom on phones) and the top bar with the user
 * menu stay put, only the page content between them scrolls.
 */
export function AppShell({ children }: AppShellProps) {
  const { t } = useTranslation(['widgets/app-shell', 'common']);

  return (
    <div className="flex h-dvh flex-col lg:flex-row">
      <aside className="order-last shrink-0 border-t border-line bg-sidebar backdrop-blur-lg lg:order-first lg:w-26 lg:border-t-0 lg:border-r">
        <div className="flex items-center lg:h-full lg:flex-col lg:gap-8 lg:py-6">
          <Link aria-label={t('common:brand.name')} className="hidden lg:block" to="/app">
            <BrandMark className="size-12" />
          </Link>
          <nav aria-label={t('nav.label')} className="flex flex-1 justify-around py-1.5 lg:flex-col lg:justify-start lg:gap-3 lg:py-0">
            {navLinks.map(({ exact, Icon, name, to }) => (
              <Link activeOptions={{ exact }} className={cn(navItem, 'hover:text-ink aria-[current=page]:text-ink')} key={to} to={to}>
                <span className={cn(navIcon, 'group-hover:bg-surface-raised group-aria-[current=page]:bg-accent-soft group-aria-[current=page]:text-accent')}>
                  <Icon />
                </span>
                {t(`nav.${name}`)}
              </Link>
            ))}
            <span aria-disabled className={cn(navItem, 'opacity-50')} title={t('nav.soon')}>
              <span className={navIcon}>
                <BudgetsIcon />
              </span>
              {t('nav.budgets')}
            </span>
          </nav>
        </div>
      </aside>

      <div className="flex min-h-0 min-w-0 flex-1 flex-col">
        <header className="flex shrink-0 items-center justify-between gap-4 border-b border-line px-6 py-3 lg:justify-end lg:border-b-0 lg:px-8 lg:pt-5">
          <Link className="flex items-center gap-3 lg:hidden" to="/app">
            <BrandMark />
            <span className="text-sm font-bold tracking-[0.14em] text-accent uppercase">{t('common:brand.name')}</span>
          </Link>
          <h1 className="sr-only">{t('common:brand.name')}</h1>
          <UserMenu />
        </header>
        <main className="min-h-0 flex-1 overflow-y-auto" id={appContentId}>
          <div className="mx-auto w-full max-w-7xl px-6 pb-8 lg:px-8">{children}</div>
        </main>
      </div>
    </div>
  );
}
