import { Link } from '@tanstack/react-router';
import { cn, Eyebrow } from '@vaultly/ui';
import { Trans, useTranslation } from 'react-i18next';

interface AppShellProps {
  children: React.ReactNode;
}

const navItem = 'rounded-full border border-line px-4 py-3.5';

export function AppShell({ children }: AppShellProps) {
  const { t } = useTranslation(['widgets/app-shell', 'common']);

  return (
    <div className="grid min-h-screen grid-cols-1 lg:grid-cols-[minmax(18rem,22rem)_minmax(0,1fr)]">
      <aside className="flex flex-col gap-8 border-b border-line bg-sidebar p-6 backdrop-blur-lg lg:border-r lg:border-b-0 lg:px-6 lg:py-8">
        <div className="grid gap-4">
          <Eyebrow className="text-base tracking-[0.14em]">{t('common:brand.name')}</Eyebrow>
          <h1 className="text-[clamp(2rem,5vw,3.5rem)] leading-tight">
            <Trans components={{ em: <span /> }} i18nKey="common:brand.slogan" t={t} />
          </h1>
        </div>

        <nav aria-label={t('nav.label')} className="grid gap-3">
          <Link
            activeProps={{ 'aria-current': 'page', className: 'border-line-strong bg-accent-soft' }}
            className={navItem}
            to="/app"
          >
            {t('nav.settings')}
          </Link>
          <span className={cn(navItem, 'text-muted')}>{t('nav.transactions')}</span>
          <span className={cn(navItem, 'text-muted')}>{t('nav.budgets')}</span>
        </nav>
      </aside>

      <main className="p-6 lg:p-8">{children}</main>
    </div>
  );
}
