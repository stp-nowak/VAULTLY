import { Button, Dialog } from '@vaultly/ui';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import type { Transaction } from '@/domain/transaction';

import { ChangeTransactionForm } from './ChangeTransactionForm';

interface ChangeTransactionButtonProps {
  className?: string;
  transaction: Transaction;
}

/** Opens the form for changing a posted transaction in a dialog. */
export function ChangeTransactionButton({ className, transaction }: ChangeTransactionButtonProps) {
  const { t } = useTranslation(['features/transaction/change', 'common']);
  const [isOpen, setIsOpen] = useState(false);

  return (
    <>
      <Button
        aria-label={t('openLabel', { description: transaction.description })}
        className={className}
        onClick={() => setIsOpen(true)}
        variant="ghost"
      >
        {t('open')}
      </Button>
      <Dialog closeLabel={t('common:actions.close')} onClose={() => setIsOpen(false)} open={isOpen} title={t('heading')}>
        <ChangeTransactionForm onDone={() => setIsOpen(false)} transaction={transaction} />
      </Dialog>
    </>
  );
}
