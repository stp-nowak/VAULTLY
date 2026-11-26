import { signInFailureOf, type SignInFailure } from '@/domain/authorization-request';

/** Fewest characters the Identity service accepts in a password. */
export const minPasswordLength = 12;

const messages: Partial<Record<SignInFailure, string>> = {
  email_taken: 'An account with this email already exists. Sign in instead.',
  invalid_input: 'Check your name and email address.',
  rate_limited: 'Too many attempts. Wait a minute and try again.',
  request_expired: 'This sign-in link is no longer valid. Go back to the app and try again.',
  sign_in_method_disabled: 'Creating an account with a password is switched off.',
  weak_password: `Use ${minPasswordLength} to 128 characters for your password.`,
};

const fallback = "We couldn't create your account. Please try again.";

export function registrationErrorMessage(error: unknown): string {
  return messages[signInFailureOf(error)] ?? fallback;
}
