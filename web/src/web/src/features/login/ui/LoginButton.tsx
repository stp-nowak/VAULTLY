import { Button, type ButtonProps } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

import { useAuth } from '@/domain/identity';

interface LoginButtonProps extends Omit<ButtonProps, 'onClick' | 'type'> {
  /** Local path to open after sign-in. */
  returnUrl?: string;
}

const defaultReturnUrl = '/app';

/** Starts sign-in: a full-page navigation that comes back to `returnUrl` once the user is signed in. */
export function LoginButton({ children, returnUrl = defaultReturnUrl, ...props }: LoginButtonProps) {
  const auth = useAuth();
  const { t } = useTranslation('features/login');

  return (
    <Button onClick={() => auth.login(returnUrl)} {...props}>
      {children ?? t('logIn')}
    </Button>
  );
}
