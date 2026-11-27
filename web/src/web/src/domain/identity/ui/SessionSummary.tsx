import { useQuery } from '@tanstack/react-query';
import { Card, Eyebrow, InfoTile, StatusNotice } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

import { useIntlLocale } from '@/shared/i18n';

import { sessionsQueryOptions } from '../api/identityQueries';
import { useAuth } from '../model/authContext';

export function SessionSummary() {
  const auth = useAuth();
  const { t } = useTranslation(['domain/identity', 'common']);
  const locale = useIntlLocale();
  const sessionsQuery = useQuery(sessionsQueryOptions(auth.isAuthenticated ? auth.sessionId : null));

  return (
    <Card aria-labelledby="sessions-heading">
      <div>
        <Eyebrow>{t('sessions.eyebrow')}</Eyebrow>
        <h2 className="text-2xl" id="sessions-heading">
          {t('sessions.heading')}
        </h2>
      </div>

      {sessionsQuery.isLoading && <StatusNotice>{t('sessions.loading')}</StatusNotice>}
      {sessionsQuery.isError && <StatusNotice tone="error">{t('sessions.loadFailed')}</StatusNotice>}

      {sessionsQuery.data && sessionsQuery.data.length > 0 && (
        <ul className="grid gap-3.5">
          {sessionsQuery.data.map((session) => (
            <li className="rounded-[1.25rem] border border-line bg-surface p-4" key={session.id}>
              <p className="mb-3 font-bold">{session.isCurrent ? t('sessions.thisBrowser') : t('sessions.otherDevice')}</p>
              <dl className="grid grid-cols-[repeat(auto-fit,minmax(12rem,1fr))] gap-3">
                <InfoTile label={t('sessions.lastUsed')} value={new Date(session.lastUsedAt).toLocaleString(locale)} />
                <InfoTile label={t('sessions.signedIn')} value={new Date(session.createdAt).toLocaleString(locale)} />
                <InfoTile label={t('sessions.device')} value={session.userAgent ?? t('common:states.unknown')} />
              </dl>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}
