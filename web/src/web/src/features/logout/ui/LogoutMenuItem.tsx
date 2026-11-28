import { LogOutIcon } from '@vaultly/ui';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { useAuth } from '@/domain/identity';

interface LogoutMenuItemProps {
  className?: string;
}

/** Menu item that ends the session here and at the Identity service. */
export function LogoutMenuItem({ className }: LogoutMenuItemProps) {
  const auth = useAuth();
  const { t } = useTranslation('features/logout');
  const [hasFailed, setHasFailed] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const logout = () => {
    setHasFailed(false);
    setIsSubmitting(true);
    void auth
      .logout()
      .catch(() => setHasFailed(true))
      .finally(() => setIsSubmitting(false));
  };

  return (
    <>
      <button className={className} disabled={isSubmitting} onClick={logout} role="menuitem" type="button">
        <LogOutIcon className="size-5 text-muted" />
        {isSubmitting ? t('signingOut') : t('signOut')}
      </button>
      {hasFailed && (
        <p className="px-3 pb-2 text-sm text-danger" role="alert">
          {t('failed')}
        </p>
      )}
    </>
  );
}
