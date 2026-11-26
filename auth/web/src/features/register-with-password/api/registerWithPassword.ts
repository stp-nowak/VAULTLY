import { authorizationRequestRoutes, parseSignInResponse } from '@/domain/authorization-request';
import { fetchJson } from '@/shared/api';

export interface Registration {
  email: string;
  name: string;
  password: string;
}

/** Creates an account and signs it in for an authorization request; resolves to where the browser goes next. */
export async function registerWithPassword(requestId: string, registration: Registration): Promise<string> {
  return parseSignInResponse(
    await fetchJson<unknown>(authorizationRequestRoutes.registration(requestId), {
      body: JSON.stringify(registration),
      headers: { 'Content-Type': 'application/json' },
      method: 'POST',
    }),
  );
}
