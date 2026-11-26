import { HttpError } from '@/shared/api';

/** What to tell the admin when a sign-in method cannot be switched; the switch shows the saved state again. */
export function toggleErrorMessage(error: unknown): string {
  return error instanceof HttpError && error.status === 409
    ? 'At least one way to sign in must stay on. Turn on another method first.'
    : "The setting couldn't be changed. Please try again.";
}
