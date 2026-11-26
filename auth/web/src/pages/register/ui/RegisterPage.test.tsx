import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { navigateToUrl } from '@/shared/lib';
import { renderRoute, server } from '@/shared/test';

import { RegisterPage } from './RegisterPage';

vi.mock('@/shared/lib', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/shared/lib')>()),
  navigateToUrl: vi.fn(),
}));

function respondWith(overrides: Record<string, unknown> = {}) {
  server.use(
    http.get('*/api/authorization-requests/:id', () =>
      HttpResponse.json({ clientName: 'Vaultly', id: '8f1c', passwordSignIn: true, providers: [], status: 'pending', ...overrides }),
    ),
  );
}

async function fillIn() {
  await userEvent.type(await screen.findByLabelText('Name'), 'Ada Lovelace');
  await userEvent.type(screen.getByLabelText('Email'), 'ada@example.com');
  await userEvent.type(screen.getByLabelText('Password'), 'correct horse battery staple');
  await userEvent.click(screen.getByRole('button', { name: 'Create account' }));
}

function renderRegister() {
  return renderRoute({ component: RegisterPage, initialEntry: '/register?request=8f1c', path: '/register' });
}

describe('RegisterPage', () => {
  beforeEach(() => {
    vi.mocked(navigateToUrl).mockClear();
  });

  it('creates the account and leaves for the app', async () => {
    let sent: unknown;
    respondWith();
    server.use(
      http.post('*/api/authorization-requests/:id/registration', async ({ request }) => {
        sent = await request.json();
        return HttpResponse.json({ redirectUrl: 'https://app.test/api/auth/callback?code=c&state=s' });
      }),
    );
    await renderRegister();

    expect(await screen.findByRole('heading', { name: 'Create your Vaultly account' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Sign in' })).toHaveAttribute('href', '/login?request=8f1c');

    await fillIn();

    await vi.waitFor(() => expect(navigateToUrl).toHaveBeenCalledWith('https://app.test/api/auth/callback?code=c&state=s'));
    expect(sent).toEqual({ email: 'ada@example.com', name: 'Ada Lovelace', password: 'correct horse battery staple' });
  });

  it('suggests signing in when the email already has an account', async () => {
    respondWith();
    server.use(
      http.post('*/api/authorization-requests/:id/registration', () => HttpResponse.json({ error: 'email_taken' }, { status: 409 })),
    );
    await renderRegister();

    await fillIn();

    expect(await screen.findByRole('alert')).toHaveTextContent('An account with this email already exists. Sign in instead.');
    expect(navigateToUrl).not.toHaveBeenCalled();
  });

  it('says registering is unavailable when password sign-in is switched off', async () => {
    respondWith({ passwordSignIn: false, providers: [{ id: 'google', name: 'Google', startUrl: '/google/authorize?request=8f1c' }] });
    await renderRegister();

    expect(await screen.findByRole('alert')).toHaveTextContent('Creating an account with a password is not available.');
    expect(screen.queryByRole('form')).not.toBeInTheDocument();
  });
});
