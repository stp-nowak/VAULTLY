import { createFileRoute } from '@tanstack/react-router';

import { usersQueryOptions } from '@/domain/user';
import { AdminUsersPage } from '@/pages/admin-users';
import { readSearchString } from '@/shared/lib';

interface UsersSearch {
  /** Missing means the first page. */
  page?: number;
  search?: string;
}

function readPage(value: unknown): number | undefined {
  const page = Number(readSearchString(value));
  return Number.isInteger(page) && page > 1 ? page : undefined;
}

export const Route = createFileRoute('/admin/users')({
  validateSearch: (search): UsersSearch => ({
    page: readPage(search.page),
    search: readSearchString(search.search)?.trim() || undefined,
  }),
  loaderDeps: ({ search }) => ({ page: search.page ?? 1, search: search.search }),
  loader: ({ context, deps }) => {
    void context.queryClient.prefetchQuery(usersQueryOptions(deps));
  },
  component: AdminUsersPage,
});
