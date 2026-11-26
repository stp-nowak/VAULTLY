import { fetchJson } from '@/shared/api';

import type { SignInMethod } from '../model/types';

/** Sign-in settings endpoints of the Identity admin API (same origin). */
export const signInSettingsRoutes = {
  list: '/api/admin/sign-in-settings',
  method: (id: string) => `/api/admin/sign-in-settings/methods/${encodeURIComponent(id)}`,
} as const;

const invalidResponse = 'Sign-in settings response was invalid.';

function parseMethod(value: unknown): SignInMethod {
  const record = value && typeof value === 'object' ? (value as Record<string, unknown>) : {};
  const { id, isConfigured, isEnabled, name } = record;

  if (typeof id !== 'string' || typeof name !== 'string' || typeof isConfigured !== 'boolean' || typeof isEnabled !== 'boolean') {
    throw new Error(invalidResponse);
  }

  return { id, isConfigured, isEnabled, name };
}

export async function listSignInMethods(): Promise<SignInMethod[]> {
  const payload = await fetchJson<unknown>(signInSettingsRoutes.list);
  const methods = payload && typeof payload === 'object' ? (payload as Record<string, unknown>).methods : undefined;

  if (!Array.isArray(methods)) {
    throw new Error(invalidResponse);
  }

  return methods.map(parseMethod);
}
