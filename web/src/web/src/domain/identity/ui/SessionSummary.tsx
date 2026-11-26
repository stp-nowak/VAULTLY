import { useQuery } from '@tanstack/react-query';
import { Card, Eyebrow, InfoTile, StatusNotice } from '@vaultly/ui';

import { sessionsQueryOptions } from '../api/identityQueries';
import { useAuth } from '../model/authContext';

export function SessionSummary() {
  const auth = useAuth();
  const sessionsQuery = useQuery(sessionsQueryOptions(auth.isAuthenticated ? auth.sessionId : null));

  return (
    <Card aria-labelledby="sessions-heading">
      <div>
        <Eyebrow>Security</Eyebrow>
        <h2 className="text-2xl" id="sessions-heading">
          Active sessions
        </h2>
      </div>

      {sessionsQuery.isLoading && <StatusNotice>Loading your sessions…</StatusNotice>}
      {sessionsQuery.isError && (
        <StatusNotice tone="error">
          {sessionsQuery.error instanceof Error ? sessionsQuery.error.message : 'Unable to load your sessions.'}
        </StatusNotice>
      )}

      {sessionsQuery.data && sessionsQuery.data.length > 0 && (
        <ul className="grid gap-3.5">
          {sessionsQuery.data.map((session) => (
            <li className="rounded-[1.25rem] border border-line bg-surface p-4" key={session.id}>
              <p className="mb-3 font-bold">{session.isCurrent ? 'This browser' : 'Other device'}</p>
              <dl className="grid grid-cols-[repeat(auto-fit,minmax(12rem,1fr))] gap-3">
                <InfoTile label="Last used" value={new Date(session.lastUsedAt).toLocaleString()} />
                <InfoTile label="Signed in" value={new Date(session.createdAt).toLocaleString()} />
                <InfoTile label="Device" value={session.userAgent ?? 'Unknown'} />
              </dl>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}
