import { signInFailureOf, type SignInFailure } from '@/domain/authorization-request';

/** Fewest characters the Identity service accepts in a password. */
export const minPasswordLength = 12;

export type RegistrationErrorKey =
  | 'common:errors.tooManyAttempts'
  | 'domain/authorization-request:invalidLink'
  | 'errors.emailTaken'
  | 'errors.invalidInput'
  | 'errors.methodDisabled'
  | 'errors.unknown'
  | 'errors.weakPassword';

const keys: Partial<Record<SignInFailure, RegistrationErrorKey>> = {
  email_taken: 'errors.emailTaken',
  invalid_input: 'errors.invalidInput',
  rate_limited: 'common:errors.tooManyAttempts',
  request_expired: 'domain/authorization-request:invalidLink',
  sign_in_method_disabled: 'errors.methodDisabled',
  weak_password: 'errors.weakPassword',
};

/** Translation key of the message for a refused registration; messages may use `{{min}}` (`minPasswordLength`). */
export function registrationErrorKey(error: unknown): RegistrationErrorKey {
  return keys[signInFailureOf(error)] ?? 'errors.unknown';
}
