import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Button, Select, StatusNotice, TextField } from '@vaultly/ui';
import { useId, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { accountCurrencies, accountQueryKeys, accountTypes, type Account, type AccountType } from '@/domain/account';
import { useIntlLocale } from '@/shared/i18n';

import { openAccount } from '../api/openAccount';
import { readAccountDraft, type AccountDraft } from '../model/accountDraft';

interface OpenAccountFormProps {
  onOpened?: (account: Account) => void;
}

const emptyDraft: AccountDraft = { currency: 'PLN', name: '', openingBalance: '0', type: 'checking' };

export function OpenAccountForm({ onOpened }: OpenAccountFormProps) {
  const { t } = useTranslation(['features/account/open', 'domain/account', 'common']);
  const locale = useIntlLocale();
  const queryClient = useQueryClient();
  const [draft, setDraft] = useState(emptyDraft);
  const [problem, setProblem] = useState<string | null>(null);
  const typeId = useId();
  const currencyId = useId();
  const currencyNames = new Intl.DisplayNames(locale, { type: 'currency' });

  const mutation = useMutation({
    mutationFn: openAccount,
    onError: () => setProblem(t('failed')),
    onSuccess: async (account) => {
      setDraft(emptyDraft);
      await queryClient.invalidateQueries({ queryKey: accountQueryKeys.all });
      onOpened?.(account);
    },
  });

  const submit = (event: React.FormEvent) => {
    event.preventDefault();
    const result = readAccountDraft(draft);
    setProblem(result.ok ? null : t(`problems.${result.problem}`));
    if (result.ok) {
      mutation.mutate(result.values);
    }
  };

  return (
    <form aria-label={t('heading')} className="grid gap-4" noValidate onSubmit={submit}>
      <div className="grid gap-4 sm:grid-cols-2">
        <TextField
          disabled={mutation.isPending}
          label={t('name')}
          maxLength={100}
          onChange={(event) => setDraft({ ...draft, name: event.target.value })}
          required
          value={draft.name}
        />
        <div className="grid gap-1.5">
          <span className="text-xs font-bold tracking-label text-muted uppercase" id={typeId}>
            {t('type')}
          </span>
          <Select<AccountType>
            aria-labelledby={typeId}
            disabled={mutation.isPending}
            onValueChange={(type) => setDraft({ ...draft, type })}
            options={accountTypes.map((type) => ({ label: t(`domain/account:types.${type}`), value: type }))}
            value={draft.type}
          />
        </div>
        <div className="grid gap-1.5">
          <span className="text-xs font-bold tracking-label text-muted uppercase" id={currencyId}>
            {t('currency')}
          </span>
          <Select
            aria-labelledby={currencyId}
            disabled={mutation.isPending}
            onValueChange={(currency) => setDraft({ ...draft, currency })}
            options={accountCurrencies.map((code) => ({ label: `${code} · ${currencyNames.of(code) ?? code}`, value: code }))}
            value={draft.currency}
          />
        </div>
        <TextField
          disabled={mutation.isPending}
          inputMode="decimal"
          label={t('openingBalance')}
          onChange={(event) => setDraft({ ...draft, openingBalance: event.target.value })}
          value={draft.openingBalance}
        />
      </div>
      <p className="text-sm text-muted">{t('openingBalanceHint')}</p>

      {problem !== null && <StatusNotice tone="error">{problem}</StatusNotice>}

      <Button className="justify-self-start" disabled={mutation.isPending} type="submit">
        {mutation.isPending ? t('common:actions.saving') : t('submit')}
      </Button>
    </form>
  );
}
