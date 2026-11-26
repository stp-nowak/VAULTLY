import { signInSettingsRoutes } from '@/domain/sign-in-settings';
import { fetchJson } from '@/shared/api';

/** Switches a sign-in method on or off. Rejects with an `HttpError` (409 when it is the last usable method). */
export async function setSignInMethodEnabled(id: string, enabled: boolean): Promise<void> {
  await fetchJson<unknown>(signInSettingsRoutes.method(id), {
    body: JSON.stringify({ enabled }),
    headers: { 'Content-Type': 'application/json' },
    method: 'PUT',
  });
}
