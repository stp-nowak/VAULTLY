import { HttpError } from '@/shared/api';

/** Translation key of what to tell the admin when a role change fails; the select snaps back to the saved role. */
export function roleChangeErrorKey(error: unknown): 'errors.lastAdmin' | 'errors.unknown' {
  return error instanceof HttpError && error.status === 409 ? 'errors.lastAdmin' : 'errors.unknown';
}
