import { signInFailureOf, type SignInFailure } from '@/domain/authorization-request';

const messages: Partial<Record<SignInFailure, string>> = {
  invalid_credentials: 'Email or password is incorrect. After several failed attempts, password sign-in pauses for a few minutes.',
  rate_limited: 'Too many attempts. Wait a minute and try again.',
  request_expired: 'This sign-in link is no longer valid. Go back to the app and try again.',
  sign_in_method_disabled: 'Signing in with a password is switched off.',
};

const fallback = "We couldn't sign you in. Please try again.";

export function signInErrorMessage(error: unknown): string {
  return messages[signInFailureOf(error)] ?? fallback;
}
