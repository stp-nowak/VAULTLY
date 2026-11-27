/** Error codes the web API puts on `/login?error=` when the OAuth callback fails. */
const signInErrors = ['invalid_state', 'login_failed'] as const;

export type SignInError = (typeof signInErrors)[number];

export function toSignInError(code: string | undefined): SignInError {
  return signInErrors.find((known) => known === code) ?? 'login_failed';
}
