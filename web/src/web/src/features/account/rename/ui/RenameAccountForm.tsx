import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Button, StatusNotice, TextField } from '@vaultly/ui';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { accountQueryKeys, accountRoutes, type Account } from '@/domain/account';
import { sendJson } from '@/shared/api';

interface RenameAccountFormProps {
  account: Account;
  onDone: () => void;
}

const nameMaxLength = 100;

export function RenameAccountForm({ account, onDone }: RenameAccountFormProps) {
  const { t } = useTranslation(['features/account/rename', 'common']);
  const queryClient = useQueryClient();
  const [name, setName] = useState(account.name);
  const [problem, setProblem] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: (newName: string) => sendJson<null>(accountRoutes.name(account.id), 'PUT', { name: newName }),
    onError: () => setProblem(t('failed')),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: accountQueryKeys.all });
      onDone();
    },
  });

  const submit = (event: React.FormEvent) => {
    event.preventDefault();
    const trimmed = name.trim();
    const isValid = trimmed.length > 0 && trimmed.length <= nameMaxLength;
    setProblem(isValid ? null : t('invalid'));
    if (isValid) {
      mutation.mutate(trimmed);
    }
  };

  return (
    <form aria-label={t('heading')} className="grid gap-3" noValidate onSubmit={submit}>
      <TextField
        disabled={mutation.isPending}
        label={t('name')}
        maxLength={nameMaxLength}
        onChange={(event) => setName(event.target.value)}
        value={name}
      />
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
