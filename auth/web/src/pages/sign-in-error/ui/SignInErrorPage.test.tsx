import { screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { renderRoute } from '@/shared/test';

import { SignInErrorPage } from './SignInErrorPage';

const loginFailed = 'Something went wrong while signing you in. Please try again.';

describe('SignInErrorPage', () => {
  it.each([
    ['login_failed', loginFailed],
    ['access_denied', 'Sign-in was cancelled.'],
    ['invalid_request', 'The sign-in request was invalid. Go back to the app and try again.'],
    ['request_expired', 'This sign-in link has expired. Go back to the app and try again.'],
    ['something_else', loginFailed],
    ['toString', loginFailed],
  ])('explains the error code %s', async (code, message) => {
    await renderRoute({ component: SignInErrorPage, initialEntry: `/error?code=${code}`, path: '/error' });

    expect(screen.getByRole('heading', { name: "Sign-in didn't complete" })).toBeInTheDocument();
    expect(screen.getByRole('alert')).toHaveTextContent(message);
  });

  it('falls back to the generic message without a code', async () => {
    await renderRoute({ component: SignInErrorPage, initialEntry: '/error', path: '/error' });

    expect(screen.getByRole('alert')).toHaveTextContent(loginFailed);
  });
});
