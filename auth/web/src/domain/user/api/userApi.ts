import { fetchJson } from '@/shared/api';

import type { UserListParams, UserPage } from '../model/types';
import { parseUserPage } from './parsers';

export const usersPageSize = 20;

/** User endpoints of the Identity admin API (same origin). */
export const userRoutes = {
  list: '/api/admin/users',
  role: (id: string) => `/api/admin/users/${encodeURIComponent(id)}/role`,
} as const;

export async function listUsers({ page, search }: UserListParams): Promise<UserPage> {
  const query = new URLSearchParams({ page: String(page), pageSize: String(usersPageSize) });
  if (search) {
    query.set('search', search);
  }

  return parseUserPage(await fetchJson<unknown>(`${userRoutes.list}?${query.toString()}`));
}
