import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Button, Select, StatusNotice } from '@vaultly/ui';
import { useId, useState } from 'react';
import { useTranslation } from 'react-i18next';

import type { Account } from '@/domain/account';
import {
  emptyDraft,
  readDraft,
  TransactionFields,
  transactionQueryKeys,
  transactionRoutes,
  type TransactionValues,
} from '@/domain/transaction';
import { sendJson, statusOf } from '@/shared/api';
import { today } from '@/shared/lib';

interface RecordTransactionFormProps {
  /** Open accounts the user can pick; the first is preselected unless `accountId` is given. */
  accounts: Account[];
  accountId?: string;
  onRecorded?: () => void;
}

const failureByStatus: Record<number, 'accountClosed' | 'accountUnknown'> = { 404: 'accountUnknown', 409: 'accountClosed' };

export function RecordTransactionForm({ accountId, accounts, onRecorded }: RecordTransactionFormProps) {
  const { t } = useTranslation(['features/transaction/record', 'domain/transaction', 'common']);
  const queryClient = useQueryClient();
  const accountLabelId = useId();
  const [draft, setDraft] = useState(() => emptyDraft(today()));
  const [pickedAccountId, setPickedAccountId] = useState(accountId ?? accounts[0]?.id ?? '');
  const [problem, setProblem] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: (values: TransactionValues) =>
      sendJson<unknown>(transactionRoutes.transactions, 'POST', { accountId: pickedAccountId, ...values }),
    onError: (error) => setProblem(t(`failed.${failureByStatus[statusOf(error) ?? 0] ?? 'other'}`)),
    onSuccess: async () => {
      setDraft(emptyDraft(draft.bookedOn));
      await queryClient.invalidateQueries({ queryKey: transactionQueryKeys.all });
      onRecorded?.();
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
      {accountId === undefined && (
        <div className="grid gap-1.5">
          <span className="text-xs font-bold tracking-label text-muted uppercase" id={accountLabelId}>
            {t('account')}
          </span>
          <Select
            aria-labelledby={accountLabelId}
            disabled={mutation.isPending}
            onValueChange={setPickedAccountId}
            options={accounts.map((account) => ({ label: `${account.name} · ${account.currency}`, value: account.id }))}
            value={pickedAccountId}
          />
        </div>
      )}
      <TransactionFields disabled={mutation.isPending} draft={draft} onChange={setDraft} />

      {problem !== null && <StatusNotice tone="error">{problem}</StatusNotice>}

      <Button className="justify-self-start" disabled={mutation.isPending} type="submit">
        {mutation.isPending ? t('common:actions.saving') : t('submit')}
      </Button>
    </form>
  );
}
