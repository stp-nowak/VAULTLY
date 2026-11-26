import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';

import { server } from '@/shared/test';

import { listUsers, usersPageSize } from './userApi';

const user = {
  createdAt: '2026-11-20T08:15:00Z',
  email: 'ada@example.com',
  emailVerified: true,
  id: 'u-1',
  name: 'Ada Lovelace',
  role: 'admin',
  signInMethods: ['google'],
};

describe('user api', () => {
  it('requests the page, page size and search and parses the users', async () => {
    let requestedUrl: URL | undefined;
    server.use(
      http.get('*/api/admin/users', ({ request }) => {
        requestedUrl = new URL(request.url);
        return HttpResponse.json({ items: [user], page: 2, pageSize: usersPageSize, total: 21 });
      }),
    );

    const page = await listUsers({ page: 2, search: 'ada' });

    expect(Object.fromEntries(requestedUrl!.searchParams)).toEqual({
      page: '2',
      pageSize: String(usersPageSize),
      search: 'ada',
    });
    expect(page).toEqual({ items: [user], page: 2, pageSize: usersPageSize, total: 21 });
  });

  it('omits an empty search', async () => {
    let search: string | null = 'not requested';
    server.use(
      http.get('*/api/admin/users', ({ request }) => {
        search = new URL(request.url).searchParams.get('search');
        return HttpResponse.json({ items: [], page: 1, pageSize: usersPageSize, total: 0 });
      }),
    );

    await listUsers({ page: 1 });

    expect(search).toBeNull();
  });

  it('rejects a user with an unknown role', async () => {
    server.use(
      http.get('*/api/admin/users', () =>
        HttpResponse.json({ items: [{ ...user, role: 'owner' }], page: 1, pageSize: usersPageSize, total: 1 }),
      ),
    );

    await expect(listUsers({ page: 1 })).rejects.toThrow('User list response was invalid.');
  });
});
