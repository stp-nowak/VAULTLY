import { HttpError } from '@/shared/api';

/** Translation key of what to tell the admin when a method cannot be switched; the switch shows the saved state. */
export function toggleErrorKey(error: unknown): 'errors.lastMethod' | 'errors.unknown' {
  return error instanceof HttpError && error.status === 409 ? 'errors.lastMethod' : 'errors.unknown';
}
