import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Button, StatusNotice } from '@vaultly/ui';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { accountQueryKeys, accountRoutes, type Account } from '@/domain/account';
import { fetchJson } from '@/shared/api';

interface CloseAccountButtonProps {
  account: Account;
}

/** Closes the account after the user confirms; a closed account takes no new transactions. */
export function CloseAccountButton({ account }: CloseAccountButtonProps) {
  const { t } = useTranslation(['features/account/close', 'common']);
  const queryClient = useQueryClient();
  const [isConfirming, setIsConfirming] = useState(false);

  const mutation = useMutation({
    mutationFn: () => fetchJson<null>(accountRoutes.close(account.id), { method: 'POST' }),
    onSuccess: async () => {
      setIsConfirming(false);
      await queryClient.invalidateQueries({ queryKey: accountQueryKeys.all });
    },
  });

  return (
    <div className="grid gap-3">
      {!isConfirming && (
        <Button className="justify-self-start" onClick={() => setIsConfirming(true)} variant="ghost">
          {t('close')}
        </Button>
      )}
      {isConfirming && (
        <div className="grid gap-3 rounded-2xl border border-danger-line bg-danger-soft p-4">
          <p>{t('confirmText', { name: account.name })}</p>
          <div className="flex flex-wrap gap-3">
            <Button disabled={mutation.isPending} onClick={() => mutation.mutate()} variant="secondary">
              {t('confirm')}
            </Button>
            <Button disabled={mutation.isPending} onClick={() => setIsConfirming(false)} variant="ghost">
              {t('common:actions.cancel')}
            </Button>
          </div>
        </div>
      )}
      {mutation.isError && <StatusNotice tone="error">{t('failed')}</StatusNotice>}
    </div>
  );
}
