import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';

import { renderRoute, server } from '@/shared/test';

import { AdminUsersPage } from './AdminUsersPage';

const ada = {
  createdAt: '2026-11-20T08:15:00Z',
  email: 'ada@example.com',
  emailVerified: true,
  id: 'u-1',
  name: 'Ada Lovelace',
  role: 'admin',
  signInMethods: ['google'],
};

const grace = { ...ada, email: 'grace@example.com', id: 'u-2', name: 'Grace Hopper', role: 'user', signInMethods: [] };

function respondWithUsers(items: unknown[], onRequest?: (url: URL) => void) {
  server.use(
    http.get('*/api/admin/users', ({ request }) => {
      onRequest?.(new URL(request.url));
      return HttpResponse.json({ items, page: 1, pageSize: 20, total: items.length });
    }),
  );
}

function renderUsers(url = '/admin/users') {
  return renderRoute({ component: AdminUsersPage, initialEntry: url, path: '/admin/users' });
}

describe('AdminUsersPage', () => {
  it('lists users with their role, sign-in methods and join date', async () => {
    respondWithUsers([ada, grace]);

    await renderUsers();

    const table = await screen.findByRole('table', { name: 'Users' });
    const [, adaRow, graceRow] = within(table).getAllByRole('row');
    expect(within(adaRow!).getByText('Ada Lovelace')).toBeInTheDocument();
    expect(within(adaRow!).getByRole('combobox', { name: 'Role of ada@example.com' })).toHaveValue('admin');
    expect(within(adaRow!).getByText('google')).toBeInTheDocument();
    expect(within(adaRow!).getByText('20 Nov 2026')).toBeInTheDocument();
    expect(within(graceRow!).getByText('Not signed in yet')).toBeInTheDocument();
    expect(screen.getByText('2 users · page 1 of 1')).toBeInTheDocument();
  });

  it('searches by putting the term into the URL', async () => {
    const searches: (string | null)[] = [];
    respondWithUsers([ada], (url) => searches.push(url.searchParams.get('search')));
    const { router } = await renderUsers();

    await userEvent.type(await screen.findByRole('searchbox', { name: 'Search' }), '  ada ');
    await userEvent.click(screen.getByRole('button', { name: 'Search' }));

    await screen.findByRole('table');
    expect(router.state.location.search).toMatchObject({ search: 'ada' });
    expect(searches).toContain('ada');
  });

  it('explains an empty search result', async () => {
    respondWithUsers([]);

    await renderUsers('/admin/users?search=nobody');

    expect(await screen.findByText('No user matches this search.')).toBeInTheDocument();
  });

  it('reports a failed load without technical details', async () => {
    server.use(http.get('*/api/admin/users', () => new HttpResponse(null, { status: 503 })));

    await renderUsers();

    expect(await screen.findByRole('alert')).toHaveTextContent("We couldn't load the users. Please try again.");
  });
});
