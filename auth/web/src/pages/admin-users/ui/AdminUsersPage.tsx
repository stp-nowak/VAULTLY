import { useQuery } from '@tanstack/react-query';
import { getRouteApi, Link } from '@tanstack/react-router';

import { SearchField, StatusNotice, type StatusTone } from '@vaultly/ui';

import { usersQueryOptions } from '@/domain/user';

import { resolveUsersView, type UsersNotice } from '../model/usersView';
import { UsersTable } from './UsersTable';

const usersRoute = getRouteApi('/admin/users');

const notices: Record<UsersNotice, { text: string; tone: StatusTone }> = {
  empty: { text: 'Nobody has signed in yet.', tone: 'info' },
  loadFailed: { text: "We couldn't load the users. Please try again.", tone: 'error' },
  loading: { text: 'Loading users…', tone: 'info' },
  noMatches: { text: 'No user matches this search.', tone: 'info' },
};

const pageLink = 'rounded-full border border-line-strong px-4 py-2 text-sm aria-disabled:pointer-events-none aria-disabled:opacity-40';

/** Admin console: everyone who has an account, with their role, sign-in methods and join date. */
export function AdminUsersPage() {
  const { page = 1, search } = usersRoute.useSearch();
  const navigate = usersRoute.useNavigate();
  const query = useQuery(usersQueryOptions({ page, search }));
  const view = resolveUsersView(query, search !== undefined);

  return (
    <div className="grid gap-8">
      <header className="flex flex-wrap items-end justify-between gap-6">
        <h1 className="text-4xl">Users</h1>
        <SearchField
          className="w-full sm:w-80"
          defaultValue={search}
          key={search}
          label="Search"
          onSearch={(term) => void navigate({ search: { search: term || undefined } })}
          placeholder="Email or name"
        />
      </header>

      {view.kind === 'table' ? (
        <section aria-label="User list" className="grid gap-5">
          <UsersTable users={view.page.items} />
          <nav aria-label="Pages" className="flex items-center justify-between gap-4 text-sm text-muted">
            <span>
              {view.page.total} {view.page.total === 1 ? 'user' : 'users'} · page {view.page.page} of {view.pageCount}
            </span>
            <span className="flex gap-2">
              <Link aria-disabled={view.page.page <= 1} className={pageLink} search={{ page: view.page.page - 1, search }} to="/admin/users">
                Previous
              </Link>
              <Link
                aria-disabled={view.page.page >= view.pageCount}
                className={pageLink}
                search={{ page: view.page.page + 1, search }}
                to="/admin/users"
              >
                Next
              </Link>
            </span>
          </nav>
        </section>
      ) : (
        <StatusNotice tone={notices[view.kind].tone}>{notices[view.kind].text}</StatusNotice>
      )}
    </div>
  );
}
