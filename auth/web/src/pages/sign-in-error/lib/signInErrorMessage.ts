/** Error codes the Identity API puts on `/error?code=` when sign-in fails on the server side. */
const signInErrorMessages = {
  access_denied: 'Sign-in was cancelled.',
  invalid_request: 'The sign-in request was invalid. Go back to the app and try again.',
  login_failed: 'Something went wrong while signing you in. Please try again.',
  request_expired: 'This sign-in link has expired. Go back to the app and try again.',
} as const satisfies Record<string, string>;

type SignInErrorCode = keyof typeof signInErrorMessages;

function isSignInErrorCode(code: string | undefined): code is SignInErrorCode {
  return code !== undefined && Object.hasOwn(signInErrorMessages, code);
}

/** User-facing message for a sign-in error code; unknown or missing codes fall back to `login_failed`. */
export function signInErrorMessage(code: string | undefined): string {
  return isSignInErrorCode(code) ? signInErrorMessages[code] : signInErrorMessages.login_failed;
}
