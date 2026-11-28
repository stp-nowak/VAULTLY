import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Button, StatusNotice } from '@vaultly/ui';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import {
  draftOf,
  readDraft,
  TransactionFields,
  transactionQueryKeys,
  transactionRoutes,
  type Transaction,
  type TransactionValues,
} from '@/domain/transaction';
import { sendJson, statusOf } from '@/shared/api';

interface ChangeTransactionFormProps {
  onDone: () => void;
  transaction: Transaction;
}

/** Changes a posted transaction; a new amount or day is pending until Accounts applies it to the balance. */
export function ChangeTransactionForm({ onDone, transaction }: ChangeTransactionFormProps) {
  const { t } = useTranslation(['features/transaction/change', 'domain/transaction', 'common']);
  const queryClient = useQueryClient();
  const [draft, setDraft] = useState(() => draftOf(transaction));
  const [problem, setProblem] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: (values: TransactionValues) => sendJson<null>(transactionRoutes.transaction(transaction.id), 'PUT', values),
    onError: (error) => setProblem(t(statusOf(error) === 409 ? 'failed.busy' : 'failed.other')),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: transactionQueryKeys.all });
      onDone();
    },
  });

  const submit = (event: React.FormEvent) => {
    event.preventDefault();
    const result = readDraft(draft);
    setProblem(result.ok ? null : t(`domain/transaction:problems.${result.problem}`));
    if (result.ok) {
      mutation.mutate(result.values);
    }
  };

  return (
    <form aria-label={t('heading')} className="grid gap-4" noValidate onSubmit={submit}>
      <TransactionFields disabled={mutation.isPending} draft={draft} onChange={setDraft} />
      {problem !== null && <StatusNotice tone="error">{problem}</StatusNotice>}
      <div className="flex flex-wrap gap-3">
        <Button disabled={mutation.isPending} type="submit">
          {mutation.isPending ? t('common:actions.saving') : t('common:actions.save')}
        </Button>
        <Button disabled={mutation.isPending} onClick={onDone} variant="ghost">
          {t('common:actions.cancel')}
        </Button>
      </div>
    </form>
  );
}
