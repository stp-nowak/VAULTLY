import { Button, Dialog } from '@vaultly/ui';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import type { Account } from '@/domain/account';

import { RecordTransactionForm } from './RecordTransactionForm';

interface RecordTransactionButtonProps {
  /** Open accounts the user can pick; with `accountId` the transaction goes to that one. */
  accounts: Account[];
  accountId?: string;
  className?: string;
}

/** Opens the form for a new transaction in a dialog, which closes once the transaction is recorded. */
export function RecordTransactionButton({ accountId, accounts, className }: RecordTransactionButtonProps) {
  const { t } = useTranslation(['features/transaction/record', 'common']);
  const [isOpen, setIsOpen] = useState(false);

  return (
    <>
      <Button className={className} onClick={() => setIsOpen(true)}>
        {t('open')}
      </Button>
      <Dialog closeLabel={t('common:actions.close')} onClose={() => setIsOpen(false)} open={isOpen} title={t('heading')}>
        <RecordTransactionForm accountId={accountId} accounts={accounts} onRecorded={() => setIsOpen(false)} />
      </Dialog>
    </>
  );
}
