import { HttpError } from '@/shared/api';

/** What to tell the admin when a role change fails; the select itself snaps back to the saved role. */
export function roleChangeErrorMessage(error: unknown): string {
  return error instanceof HttpError && error.status === 409
    ? 'At least one admin must remain. Make someone else an admin first.'
    : "The role couldn't be changed. Please try again.";
}
