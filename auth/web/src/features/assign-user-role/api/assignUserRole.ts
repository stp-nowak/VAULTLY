import { userRoutes, type UserRole } from '@/domain/user';
import { fetchJson } from '@/shared/api';

/** Gives a user another role. Rejects with an `HttpError` (409 when it would remove the last admin). */
export async function assignUserRole(id: string, role: UserRole): Promise<void> {
  await fetchJson<unknown>(userRoutes.role(id), {
    body: JSON.stringify({ role }),
    headers: { 'Content-Type': 'application/json' },
    method: 'PUT',
  });
}
