import { signInFailureOf, type SignInFailure } from '@/domain/authorization-request';

export type SignInErrorKey =
  | 'common:errors.tooManyAttempts'
  | 'domain/authorization-request:invalidLink'
  | 'errors.invalidCredentials'
  | 'errors.methodDisabled'
  | 'errors.unknown';

const keys: Partial<Record<SignInFailure, SignInErrorKey>> = {
  invalid_credentials: 'errors.invalidCredentials',
  rate_limited: 'common:errors.tooManyAttempts',
  request_expired: 'domain/authorization-request:invalidLink',
  sign_in_method_disabled: 'errors.methodDisabled',
};

/** Translation key of the message for a refused password sign-in. */
export function signInErrorKey(error: unknown): SignInErrorKey {
  return keys[signInFailureOf(error)] ?? 'errors.unknown';
}
