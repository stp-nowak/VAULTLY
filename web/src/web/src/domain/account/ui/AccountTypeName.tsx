import { useTranslation } from 'react-i18next';

import type { Account } from '../model/types';

interface AccountTypeNameProps {
  account: Pick<Account, 'closedAt' | 'type'>;
}

/** The account's type, and that it is closed when it is. */
export function AccountTypeName({ account }: AccountTypeNameProps) {
  const { t } = useTranslation('domain/account');
  const type = t(`types.${account.type}`);

  return <>{account.closedAt ? `${type} · ${t('closed')}` : type}</>;
}
