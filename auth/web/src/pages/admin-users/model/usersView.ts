import type { QueryStatus } from '@tanstack/react-query';

import type { UserPage } from '@/domain/user';

/** Why the user table cannot be shown. */
export type UsersNotice = 'empty' | 'loadFailed' | 'loading' | 'noMatches';

export type UsersView = { kind: 'table'; page: UserPage; pageCount: number } | { kind: UsersNotice };

/**
 * Derives what the users page shows from the users query. Data already on screen (e.g. the previous page while the
 * next one loads) keeps the table; a loaded page without users is "empty", or "no matches" when a search is active.
 */
export function resolveUsersView(query: { data: UserPage | undefined; status: QueryStatus }, isSearching: boolean): UsersView {
  const page = query.data;

  if (!page) {
    return { kind: query.status === 'error' ? 'loadFailed' : 'loading' };
  }

  return page.items.length > 0
    ? { kind: 'table', page, pageCount: Math.max(1, Math.ceil(page.total / page.pageSize)) }
    : { kind: isSearching ? 'noMatches' : 'empty' };
}
