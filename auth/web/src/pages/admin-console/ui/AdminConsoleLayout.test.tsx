import { screen } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { navigateToUrl } from '@/shared/lib';
import { renderRoute, server } from '@/shared/test';

import { AdminConsoleLayout } from './AdminConsoleLayout';

vi.mock('@/shared/lib', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/shared/lib')>()),
  navigateToUrl: vi.fn(),
}));

function renderConsole() {
  return renderRoute({ component: AdminConsoleLayout, initialEntry: '/admin', path: '/admin' });
}

describe('AdminConsoleLayout', () => {
  beforeEach(() => {
    vi.mocked(navigateToUrl).mockClear();
  });

  it('shows the console frame to an admin', async () => {
    server.use(
      http.get('*/api/admin/me', () =>
        HttpResponse.json({ email: 'admin@example.com', id: 'u-1', name: 'Ada Admin', role: 'admin' }),
      ),
    );

    await renderConsole();

    expect(await screen.findByText('admin@example.com')).toBeInTheDocument();
    expect(screen.getByRole('navigation', { name: 'Admin console' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Sign out' })).toHaveAttribute('href', '/logout');
    expect(navigateToUrl).not.toHaveBeenCalled();
  });

  it('tells a signed-in user without the admin role that they have no access', async () => {
    server.use(http.get('*/api/admin/me', () => new HttpResponse(null, { status: 403 })));

    await renderConsole();

    expect(await screen.findByRole('heading', { name: 'No access' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Sign out' })).toHaveAttribute('href', '/logout');
    expect(screen.queryByRole('navigation', { name: 'Admin console' })).not.toBeInTheDocument();
  });

  it('sends a browser without a session to sign in', async () => {
    server.use(http.get('*/api/admin/me', () => new HttpResponse(null, { status: 401 })));

    await renderConsole();

    expect(await screen.findByRole('heading', { name: 'Signing you in…' })).toBeInTheDocument();
    expect(navigateToUrl).toHaveBeenCalledExactlyOnceWith('/api/admin/sign-in');
  });
});
