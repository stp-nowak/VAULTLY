import { Link } from '@tanstack/react-router';
import { BrandMark } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

interface SiteHeaderProps {
  /** Right-aligned actions, e.g. the log in button. */
  actions?: React.ReactNode;
}

/** Top bar of the public pages: brand on the left, actions on the right. */
export function SiteHeader({ actions }: SiteHeaderProps) {
  const { t } = useTranslation();

  return (
    <header className="sticky top-0 z-10 border-b border-line bg-sidebar backdrop-blur-lg">
      <div className="mx-auto flex max-w-300 items-center justify-between gap-4 px-6 py-3.5 lg:px-8">
        <Link className="flex items-center gap-3" to="/">
          <BrandMark />
          <span className="font-display text-2xl font-semibold tracking-display">{t('brand.name')}</span>
        </Link>
        {actions}
      </div>
    </header>
  );
}
