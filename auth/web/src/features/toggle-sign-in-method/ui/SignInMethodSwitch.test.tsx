import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';

import type { SignInMethod } from '@/domain/sign-in-settings';
import { server } from '@/shared/test';

import { SignInMethodSwitch } from './SignInMethodSwitch';

const google: SignInMethod = { id: 'google', isConfigured: true, isEnabled: true, name: 'Google' };

function renderSwitch(method = google) {
  const queryClient = new QueryClient({ defaultOptions: { mutations: { retry: false }, queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <SignInMethodSwitch method={method} />
    </QueryClientProvider>,
  );
}

describe('SignInMethodSwitch', () => {
  it('saves the new state', async () => {
    let saved: unknown;
    server.use(
      http.put('*/api/admin/sign-in-settings/methods/:id', async ({ params, request }) => {
        saved = { id: params.id, ...((await request.json()) as object) };
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderSwitch();

    await userEvent.click(screen.getByRole('switch', { name: 'Google' }));

    await expect.poll(() => saved).toEqual({ enabled: false, id: 'google' });
  });

  it('explains why the last method cannot be switched off and shows it on again', async () => {
    server.use(
      http.put('*/api/admin/sign-in-settings/methods/:id', () =>
        HttpResponse.json({ error: 'last_sign_in_method' }, { status: 409 }),
      ),
    );
    renderSwitch();
    const toggle = screen.getByRole('switch', { name: 'Google' });

    await userEvent.click(toggle);

    expect(await screen.findByRole('alert')).toHaveTextContent('At least one way to sign in must stay on.');
    expect(toggle).toBeChecked();
  });

  it('shows a method this server is not set up for as off and cannot switch it', () => {
    renderSwitch({ id: 'password', isConfigured: false, isEnabled: true, name: 'Email and password' });

    const toggle = screen.getByRole('switch', { name: 'Email and password' });
    expect(toggle).toBeDisabled();
    expect(toggle).not.toBeChecked();
  });
});
