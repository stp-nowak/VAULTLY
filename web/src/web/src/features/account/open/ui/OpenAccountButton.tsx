import { Button, Dialog } from '@vaultly/ui';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { OpenAccountForm } from './OpenAccountForm';

interface OpenAccountButtonProps {
  className?: string;
}

/** Opens the form for a new account in a dialog, which closes once the account is open. */
export function OpenAccountButton({ className }: OpenAccountButtonProps) {
  const { t } = useTranslation(['features/account/open', 'common']);
  const [isOpen, setIsOpen] = useState(false);

  return (
    <>
      <Button className={className} onClick={() => setIsOpen(true)} variant="secondary">
        {t('heading')}
      </Button>
      <Dialog closeLabel={t('common:actions.close')} onClose={() => setIsOpen(false)} open={isOpen} title={t('heading')}>
        <OpenAccountForm onOpened={() => setIsOpen(false)} />
      </Dialog>
    </>
  );
}
