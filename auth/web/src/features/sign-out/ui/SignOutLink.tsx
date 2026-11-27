import { cn } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

import { consoleRoutes } from '@/domain/console-admin';

interface SignOutLinkProps {
  className?: string;
}

/** Ends the Identity service session: a page navigation to its logout endpoint. */
export function SignOutLink({ className }: SignOutLinkProps) {
  const { t } = useTranslation('features/sign-out');

  return (
    <a
      className={cn('justify-self-start border-b border-line-strong pb-0.5 hover:border-accent', className)}
      href={consoleRoutes.signOut}
    >
      {t('signOut')}
    </a>
  );
}
