import { screen, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';

import { renderRoute, server } from '@/shared/test';

import { AdminSignInMethodsPage } from './AdminSignInMethodsPage';

function renderPage() {
  return renderRoute({ component: AdminSignInMethodsPage, initialEntry: '/admin/sign-in-methods', path: '/admin/sign-in-methods' });
}

describe('AdminSignInMethodsPage', () => {
  it('lists every method with how it appears to people signing in', async () => {
    let csrf: string | null = null;
    server.use(
      http.get('*/api/admin/sign-in-settings', ({ request }) => {
        csrf = request.headers.get('X-CSRF');
        return HttpResponse.json({
          methods: [
            { id: 'google', isConfigured: true, isEnabled: true, name: 'Google' },
            { id: 'github', isConfigured: true, isEnabled: false, name: 'GitHub' },
            { id: 'password', isConfigured: false, isEnabled: true, name: 'Email and password' },
          ],
        });
      }),
    );

    await renderPage();

    const list = await screen.findByRole('region', { name: 'Sign-in methods' });
    const [google, github, password] = within(list).getAllByRole('listitem');
    expect(google).toHaveTextContent('Offered on the sign-in page.');
    expect(within(google!).getByRole('switch', { name: 'Google' })).toBeChecked();
    expect(github).toHaveTextContent('Switched off: not offered on the sign-in page.');
    expect(within(github!).getByRole('switch', { name: 'GitHub' })).not.toBeChecked();
    expect(password).toHaveTextContent('Not set up on this server yet.');
    expect(within(password!).getByRole('switch', { name: 'Email and password' })).toBeDisabled();
    expect(csrf).toBe('1');
  });

  it('reports a failed load without technical details', async () => {
    server.use(http.get('*/api/admin/sign-in-settings', () => new HttpResponse(null, { status: 503 })));

    await renderPage();

    expect(await screen.findByRole('alert')).toHaveTextContent("We couldn't load the sign-in methods. Please try again.");
  });
});
