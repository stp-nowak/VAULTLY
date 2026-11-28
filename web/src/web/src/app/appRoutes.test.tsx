import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { navigateToUrl } from '@/shared/lib';
import { server } from '@/shared/test/msw/server';

import { renderApp } from './test/renderApp';

vi.mock('@/shared/lib', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/shared/lib')>()),
  navigateToUrl: vi.fn(),
}));

const landingHeading = { level: 1, name: 'Know where every coin goes.' };

function signIn() {
  server.use(
    http.get('*/api/auth/user', () =>
      HttpResponse.json({
        sessionId: 'session-1',
        user: { email: 'alex@example.com', id: 'user-1', name: 'Alex Kowalski' },
      }),
    ),
    http.get('*/api/auth/sessions', () => HttpResponse.json({ sessions: [] })),
  );
}

describe('app routes', () => {
  beforeEach(() => {
    vi.mocked(navigateToUrl).mockClear();
  });

  describe('without a session', () => {
    it('shows the landing page on / and does not start sign-in', async () => {
      const { router } = renderApp('/');

      expect(await screen.findByRole('heading', landingHeading)).toBeInTheDocument();
      expect(within(screen.getByRole('banner')).getByRole('button', { name: 'Log in' })).toBeInTheDocument();
      expect(router.state.location.pathname).toBe('/');
      expect(navigateToUrl).not.toHaveBeenCalled();
    });

    it('starts sign-in from the top bar and returns to the app', async () => {
      renderApp('/');

      await userEvent.click(await screen.findByRole('button', { name: 'Log in' }));

      expect(navigateToUrl).toHaveBeenCalledExactlyOnceWith('/api/auth/login?returnUrl=%2Fapp');
    });

    it('sends /app to the landing page and remembers where the user was going', async () => {
      const { router } = renderApp('/app?month=2026-11');

      await userEvent.click(await screen.findByRole('button', { name: 'Log in to your vault' }));

      expect(router.state.location.pathname).toBe('/');
      expect(navigateToUrl).toHaveBeenCalledExactlyOnceWith('/api/auth/login?returnUrl=%2Fapp%3Fmonth%3D2026-11');
    });

    it('ignores a return address outside the app', async () => {
      renderApp('/?returnUrl=https%3A%2F%2Fevil.example%2Fapp');

      await userEvent.click(await screen.findByRole('button', { name: 'Log in' }));

      expect(navigateToUrl).toHaveBeenCalledExactlyOnceWith('/api/auth/login?returnUrl=%2Fapp');
    });

    it('opens the landing page for /login without an error', async () => {
      const { router } = renderApp('/login');

      expect(await screen.findByRole('heading', landingHeading)).toBeInTheDocument();
      expect(router.state.location.pathname).toBe('/');
      expect(navigateToUrl).not.toHaveBeenCalled();
    });

    it('explains a failed sign-in on /login and lets the user try again', async () => {
      renderApp('/login?error=invalid_state&returnUrl=%2Fapp');

      expect(await screen.findByRole('alert')).toHaveTextContent(
        'The sign-in attempt expired or was not started from this browser.',
      );

      await userEvent.click(screen.getByRole('button', { name: 'Try again' }));

      expect(navigateToUrl).toHaveBeenCalledExactlyOnceWith('/api/auth/login?returnUrl=%2Fapp');
    });
  });

  describe('with a session', () => {
    beforeEach(signIn);

    it.each(['/', '/login', '/login?error=login_failed'])('opens the app instead of %s', async (entry) => {
      const { router } = renderApp(entry);

      expect(await screen.findByRole('heading', { name: 'Welcome back, Alex.' })).toBeInTheDocument();
      expect(router.state.location.pathname).toBe('/app');
      expect(screen.queryByRole('button', { name: 'Log in' })).not.toBeInTheDocument();
    });

    it('opens a page deep inside the app straight away', async () => {
      const { router } = renderApp('/app/settings');

      expect(await screen.findByRole('heading', { name: 'Your sign-in' })).toBeInTheDocument();
      expect(router.state.location.pathname).toBe('/app/settings');
    });
  });
});
