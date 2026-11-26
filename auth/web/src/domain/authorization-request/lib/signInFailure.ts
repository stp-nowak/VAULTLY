import { HttpError } from '@/shared/api';

/** Why a password sign-in or registration was refused, as the Identity API reports it. */
export type SignInFailure =
  | 'email_taken'
  | 'invalid_credentials'
  | 'invalid_input'
  | 'rate_limited'
  | 'request_expired'
  | 'sign_in_method_disabled'
  | 'unknown'
  | 'weak_password';

const knownFailures: readonly SignInFailure[] = [
  'email_taken',
  'invalid_credentials',
  'invalid_input',
  'request_expired',
  'sign_in_method_disabled',
  'weak_password',
];

function isKnownFailure(value: unknown): value is SignInFailure {
  return typeof value === 'string' && (knownFailures as readonly string[]).includes(value);
}

/** Reads the failure code of a rejected password sign-in or registration; anything unexpected is `unknown`. */
export function signInFailureOf(error: unknown): SignInFailure {
  if (!(error instanceof HttpError)) {
    return 'unknown';
  }

  if (error.status === 429) {
    return 'rate_limited';
  }

  const code = error.payload && typeof error.payload === 'object' ? (error.payload as Record<string, unknown>).error : undefined;
  return isKnownFailure(code) ? code : 'unknown';
}
