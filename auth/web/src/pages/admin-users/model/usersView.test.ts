import { describe, expect, it } from 'vitest';

import type { UserPage } from '@/domain/user';

import { resolveUsersView } from './usersView';

const user = {
  createdAt: '2026-11-20T08:15:00Z',
  email: 'ada@example.com',
  emailVerified: true,
  id: 'u-1',
  name: 'Ada',
  role: 'user' as const,
  signInMethods: [],
};

function page(overrides: Partial<UserPage> = {}): UserPage {
  return { items: [user], page: 1, pageSize: 20, total: 41, ...overrides };
}

describe('resolveUsersView', () => {
  it('shows the table with the page count', () => {
    expect(resolveUsersView({ data: page(), status: 'success' }, false)).toEqual({
      kind: 'table',
      page: page(),
      pageCount: 3,
    });
  });

  it.each([
    ['pending', 'loading'],
    ['error', 'loadFailed'],
  ] as const)('shows a notice while the query is %s without data', (status, kind) => {
    expect(resolveUsersView({ data: undefined, status }, false)).toEqual({ kind });
  });

  it('keeps the table when a refetch fails with data on screen', () => {
    expect(resolveUsersView({ data: page(), status: 'error' }, false)).toMatchObject({ kind: 'table' });
  });

  it.each([
    [false, 'empty'],
    [true, 'noMatches'],
  ] as const)('tells an empty list apart from a search without matches (searching: %s)', (isSearching, kind) => {
    expect(resolveUsersView({ data: page({ items: [], total: 0 }), status: 'success' }, isSearching)).toEqual({ kind });
  });
});
