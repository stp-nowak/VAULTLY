import { useQuery } from '@tanstack/react-query';

import { Card, StatusNotice } from '@vaultly/ui';

import { signInMethodsQueryOptions } from '@/domain/sign-in-settings';
import { SignInMethodSwitch } from '@/features/toggle-sign-in-method';

import { resolveMethodStatus, type MethodStatus } from '../model/methodStatus';

const statusText: Record<MethodStatus, string> = {
  hidden: 'Switched off: not offered on the sign-in page.',
  notConfigured: 'Not set up on this server yet.',
  shown: 'Offered on the sign-in page.',
};

/** Admin console: which ways to sign in people are offered. */
export function AdminSignInMethodsPage() {
  const query = useQuery(signInMethodsQueryOptions());
  const notice = query.isError
    ? { text: "We couldn't load the sign-in methods. Please try again.", tone: 'error' as const }
    : { text: 'Loading sign-in methods…', tone: 'info' as const };

  return (
    <div className="grid gap-8">
      <header className="grid gap-2">
        <h1 className="text-4xl">Sign-in methods</h1>
        <p className="max-w-160 text-muted">
          Choose how people can sign in. Changes apply to the next sign-in; at least one method always stays on.
        </p>
      </header>

      {query.data !== undefined && (
        <Card aria-label="Sign-in methods" className="max-w-180 gap-0 p-0">
          <ul className="divide-y divide-line">
            {query.data.map((method) => (
              <li className="flex items-center justify-between gap-6 px-6 py-5" key={method.id}>
                <div className="grid gap-1">
                  <span className="font-bold">{method.name}</span>
                  <span className="text-sm text-muted">{statusText[resolveMethodStatus(method)]}</span>
                </div>
                <SignInMethodSwitch method={method} />
              </li>
            ))}
          </ul>
        </Card>
      )}
      {query.data === undefined && <StatusNotice tone={notice.tone}>{notice.text}</StatusNotice>}
    </div>
  );
}
