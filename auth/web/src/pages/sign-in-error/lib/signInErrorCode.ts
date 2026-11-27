/** Error codes the Identity API puts on `/error?code=` when sign-in fails on the server side. */
const signInErrorCodes = ['access_denied', 'invalid_request', 'login_failed', 'request_expired'] as const;

export type SignInErrorCode = (typeof signInErrorCodes)[number];

/** Known sign-in error code; unknown or missing codes fall back to `login_failed`. */
export function toSignInErrorCode(code: string | undefined): SignInErrorCode {
  return signInErrorCodes.find((known) => known === code) ?? 'login_failed';
}
