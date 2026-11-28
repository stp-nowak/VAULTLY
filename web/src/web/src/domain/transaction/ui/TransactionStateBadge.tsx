import { Badge } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

import { stateOf } from '../model/state';
import type { Transaction } from '../model/types';

interface TransactionStateBadgeProps {
  transaction: Transaction;
}

/** Pending, rejected or undone, with why; nothing for a posted transaction. */
export function TransactionStateBadge({ transaction }: TransactionStateBadgeProps) {
  const { t } = useTranslation('domain/transaction');
  const state = stateOf(transaction);
  const reason = 'reason' in state && state.reason ? t(`reasons.${state.reason}`) : '';
  const labels = {
    deleted: () => t('states.deleted'),
    pending: () => t(`states.pending.${'step' in state ? state.step : 'record'}`),
    posted: () => '',
    rejected: () => t('states.rejected', { reason }),
    undone: () => t('states.undone', { reason }),
  } satisfies Record<typeof state.kind, () => string>;
  const label = labels[state.kind]();

  return label !== '' && <Badge tone={state.kind === 'pending' ? 'accent' : 'neutral'}>{label}</Badge>;
}
