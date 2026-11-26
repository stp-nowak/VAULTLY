import { fetchJson, HttpError } from '@/shared/api';

import type { ConsoleAccess, ConsoleAdmin } from '../model/types';

/** Admin console endpoints of the Identity API (same origin). `signIn` and `signOut` are page navigations. */
export const consoleRoutes = {
  me: '/api/admin/me',
  signIn: '/api/admin/sign-in',
  signOut: '/logout',
} as const;

const accessByStatusCode: Partial<Record<number, 'forbidden' | 'signed-out'>> = {
  401: 'signed-out',
  403: 'forbidden',
};

function parseConsoleAdmin(value: unknown): ConsoleAdmin {
  const record = value && typeof value === 'object' ? (value as Record<string, unknown>) : {};
  const { email, id, name } = record;

  if (typeof id !== 'string' || typeof email !== 'string' || typeof name !== 'string') {
    throw new Error('Admin console response was invalid.');
  }

  return { email, id, name };
}

/** Who may use the admin console in this browser. Failures other than 401/403 throw. */
export async function getConsoleAccess(): Promise<ConsoleAccess> {
  try {
    return { admin: parseConsoleAdmin(await fetchJson<unknown>(consoleRoutes.me)), status: 'admin' };
  } catch (error) {
    const status = error instanceof HttpError ? accessByStatusCode[error.status] : undefined;
    if (status) {
      return { status };
    }

    throw error;
  }
}
