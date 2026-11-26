import { authorizationRequestRoutes, parseSignInResponse } from '@/domain/authorization-request';
import { fetchJson } from '@/shared/api';

export interface PasswordCredentials {
  email: string;
  password: string;
}

/** Signs in for an authorization request; resolves to where the browser goes next. */
export async function signInWithPassword(requestId: string, credentials: PasswordCredentials): Promise<string> {
  return parseSignInResponse(
    await fetchJson<unknown>(authorizationRequestRoutes.passwordSignIn(requestId), {
      body: JSON.stringify(credentials),
      headers: { 'Content-Type': 'application/json' },
      method: 'POST',
    }),
  );
}
