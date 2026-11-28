import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';

import type { UserSummary } from '@/domain/user';
import { pickOption, server } from '@/shared/test';

import { UserRoleSelect } from './UserRoleSelect';

const grace: UserSummary = {
  createdAt: '2026-11-20T08:15:00Z',
  email: 'grace@example.com',
  emailVerified: true,
  id: 'u-2',
  name: 'Grace Hopper',
  role: 'user',
  signInMethods: [],
};

function renderSelect(user = grace) {
  const queryClient = new QueryClient({ defaultOptions: { mutations: { retry: false }, queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <UserRoleSelect user={user} />
    </QueryClientProvider>,
  );
}

describe('UserRoleSelect', () => {
  it('saves the picked role', async () => {
    let body: unknown;
    server.use(
      http.put('*/api/admin/users/:id/role', async ({ params, request }) => {
        body = { id: params.id, ...((await request.json()) as object) };
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderSelect();

    await pickOption(screen.getByRole('combobox', { name: 'Role of grace@example.com' }), 'Admin');

    await expect.poll(() => body).toEqual({ id: 'u-2', role: 'admin' });
  });

  it('explains why the last admin cannot be demoted and shows the saved role again', async () => {
    server.use(
      http.put('*/api/admin/users/:id/role', () => HttpResponse.json({ error: 'last_admin' }, { status: 409 })),
    );
    renderSelect({ ...grace, role: 'admin' });
    const select = screen.getByRole('combobox', { name: 'Role of grace@example.com' });

    await pickOption(select, 'User');

    expect(await screen.findByRole('alert')).toHaveTextContent('At least one admin must remain.');
    expect(select).toHaveTextContent('Admin');
  });

  it('shows a generic message for other failures', async () => {
    server.use(http.put('*/api/admin/users/:id/role', () => new HttpResponse(null, { status: 500 })));
    renderSelect();

    await pickOption(screen.getByRole('combobox'), 'Admin');

    expect(await screen.findByRole('alert')).toHaveTextContent("The role couldn't be changed. Please try again.");
  });
});
