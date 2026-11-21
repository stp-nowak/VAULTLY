import { fetchJson, HttpError } from '@/shared/api';

import type { AuthorizationRequest } from '../model/types';
import { parseAuthorizationRequest } from './parsers';

/** Authorization request endpoints of the Identity API (auth/src), same origin. */
export const authorizationRequestRoutes = {
  byId: (id: string) => `/api/authorization-requests/${encodeURIComponent(id)}`,
} as const;

/**
 * Loads an authorization request. Returns `null` when the Identity API does not know the id (404), which the UI
 * treats like an expired link. Other failures throw.
 */
export async function getAuthorizationRequest(id: string): Promise<AuthorizationRequest | null> {
  try {
    return parseAuthorizationRequest(await fetchJson<unknown>(authorizationRequestRoutes.byId(id)));
  } catch (error) {
    if (error instanceof HttpError && error.status === 404) {
      return null;
    }

    throw error;
  }
}
