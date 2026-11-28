import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Button } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

import { transactionQueryKeys, transactionRoutes, type Transaction } from '@/domain/transaction';
import { fetchJson } from '@/shared/api';

interface DeleteTransactionButtonProps {
  onFailed: (message: string) => void;
  transaction: Transaction;
}

/** Deletes a transaction; a posted one is pending until Accounts takes it off the balance. */
export function DeleteTransactionButton({ onFailed, transaction }: DeleteTransactionButtonProps) {
  const { t } = useTranslation('features/transaction/delete');
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: () => fetchJson<null>(transactionRoutes.transaction(transaction.id), { method: 'DELETE' }),
    onError: () => onFailed(t('failed')),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: transactionQueryKeys.all }),
  });

  return (
    <Button
      aria-label={t('label', { description: transaction.description })}
      className="min-h-9 px-3.5 text-sm"
      disabled={mutation.isPending}
      onClick={() => mutation.mutate()}
      variant="ghost"
    >
      {t('delete')}
    </Button>
  );
}
