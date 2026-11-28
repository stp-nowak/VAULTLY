import { Select, TextField } from '@vaultly/ui';
import { useId } from 'react';
import { useTranslation } from 'react-i18next';

import { descriptionMaxLength, noteMaxLength, type TransactionDirection, type TransactionDraft } from '../model/draft';

interface TransactionFieldsProps {
  disabled?: boolean;
  draft: TransactionDraft;
  onChange: (draft: TransactionDraft) => void;
}

/** The fields shared by recording and changing a transaction; the form around them owns the account and submit. */
export function TransactionFields({ disabled, draft, onChange }: TransactionFieldsProps) {
  const { t } = useTranslation('domain/transaction');
  const directionId = useId();
  const set = <K extends keyof TransactionDraft>(key: K, value: TransactionDraft[K]) => onChange({ ...draft, [key]: value });
  const directions: { label: string; value: TransactionDirection }[] = [
    { label: t('fields.expense'), value: 'expense' },
    { label: t('fields.income'), value: 'income' },
  ];

  return (
    <div className="grid gap-4 sm:grid-cols-2">
      <div className="grid gap-1.5">
        <span className="text-xs font-bold tracking-label text-muted uppercase" id={directionId}>
          {t('fields.direction')}
        </span>
        <Select
          aria-labelledby={directionId}
          disabled={disabled}
          onValueChange={(value) => set('direction', value)}
          options={directions}
          value={draft.direction}
        />
      </div>
      <TextField
        disabled={disabled}
        inputMode="decimal"
        label={t('fields.amount')}
        onChange={(event) => set('amount', event.target.value)}
        placeholder={t('fields.amountPlaceholder')}
        required
        value={draft.amount}
      />
      <TextField
        disabled={disabled}
        label={t('fields.bookedOn')}
        onChange={(event) => set('bookedOn', event.target.value)}
        required
        type="date"
        value={draft.bookedOn}
      />
      <TextField
        disabled={disabled}
        label={t('fields.description')}
        maxLength={descriptionMaxLength}
        onChange={(event) => set('description', event.target.value)}
        required
        value={draft.description}
      />
      <TextField
        className="sm:col-span-2"
        disabled={disabled}
        label={t('fields.note')}
        maxLength={noteMaxLength}
        onChange={(event) => set('note', event.target.value)}
        value={draft.note}
      />
    </div>
  );
}
