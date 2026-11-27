import { useQuery } from '@tanstack/react-query';

import { Card, StatusNotice } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

import { signInMethodsQueryOptions } from '@/domain/sign-in-settings';
import { SignInMethodSwitch } from '@/features/toggle-sign-in-method';

import { resolveMethodStatus } from '../model/methodStatus';

/** Admin console: which ways to sign in people are offered. */
export function AdminSignInMethodsPage() {
  const { t } = useTranslation('pages/admin-sign-in-methods');
  const query = useQuery(signInMethodsQueryOptions());
  const notice = query.isError
    ? { key: 'loadFailed' as const, tone: 'error' as const }
    : { key: 'loading' as const, tone: 'info' as const };

  return (
    <div className="grid gap-8">
      <header className="grid gap-2">
        <h1 className="text-4xl">{t('heading')}</h1>
        <p className="max-w-160 text-muted">{t('intro')}</p>
      </header>

      {query.data !== undefined && (
        <Card aria-label={t('heading')} className="max-w-180 gap-0 p-0">
          <ul className="divide-y divide-line">
            {query.data.map((method) => (
              <li className="flex items-center justify-between gap-6 px-6 py-5" key={method.id}>
                <div className="grid gap-1">
                  <span className="font-bold">{method.name}</span>
                  <span className="text-sm text-muted">{t(`status.${resolveMethodStatus(method)}`)}</span>
                </div>
                <SignInMethodSwitch method={method} />
              </li>
            ))}
          </ul>
        </Card>
      )}
      {query.data === undefined && <StatusNotice tone={notice.tone}>{t(notice.key)}</StatusNotice>}
    </div>
  );
}
