import { fetchJson, HttpError } from '@/shared/api';

import type { AuthSession, CurrentUser } from '../model/types';
import { parseCurrentUser, parseSessionsResponse } from './parsers';

/** Auth endpoints of the web API (web/src/api, feature Auth). */
export const authRoutes = {
  login: '/api/auth/login',
  logout: '/api/auth/logout',
  sessions: '/api/auth/sessions',
  user: '/api/auth/user',
} as const;

/**
 * URL that starts the login flow. It is a full-page navigation: the web API redirects to the Identity service and,
 * after the callback, back to `returnUrl` with the session cookie set.
 */
export function buildLoginUrl(returnUrl?: string): string {
  return returnUrl ? `${authRoutes.login}?returnUrl=${encodeURIComponent(returnUrl)}` : authRoutes.login;
}

/** Returns the signed-in user, or `null` when there is no active session (401). */
export async function getCurrentUser(): Promise<CurrentUser | null> {
  try {
    return parseCurrentUser(await fetchJson<unknown>(authRoutes.user));
  } catch (error) {
    if (error instanceof HttpError && error.status === 401) {
      return null;
    }

    throw error;
  }
}

/** Lists the user's Identity sessions. */
export async function listSessions(): Promise<AuthSession[]> {
  return parseSessionsResponse(await fetchJson<unknown>(authRoutes.sessions));
}

/** Ends the session on the server and clears the session cookie. */
export async function logout(): Promise<void> {
  await fetchJson<null>(authRoutes.logout, { method: 'POST' });
}
