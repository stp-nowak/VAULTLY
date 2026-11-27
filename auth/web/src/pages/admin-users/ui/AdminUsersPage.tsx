import { useQuery } from '@tanstack/react-query';
import { getRouteApi, Link } from '@tanstack/react-router';

import { SearchField, StatusNotice, type StatusTone } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

import { usersQueryOptions } from '@/domain/user';

import { resolveUsersView, type UsersNotice } from '../model/usersView';
import { UsersTable } from './UsersTable';

const usersRoute = getRouteApi('/admin/users');

const noticeTones: Record<UsersNotice, StatusTone> = {
  empty: 'info',
  loadFailed: 'error',
  loading: 'info',
  noMatches: 'info',
};

const pageLink = 'rounded-full border border-line-strong px-4 py-2 text-sm aria-disabled:pointer-events-none aria-disabled:opacity-40';

/** Admin console: everyone who has an account, with their role, sign-in methods and join date. */
export function AdminUsersPage() {
  const { t } = useTranslation(['pages/admin-users', 'common']);
  const { page = 1, search } = usersRoute.useSearch();
  const navigate = usersRoute.useNavigate();
  const query = useQuery(usersQueryOptions({ page, search }));
  const view = resolveUsersView(query, search !== undefined);

  return (
    <div className="grid gap-8">
      <header className="flex flex-wrap items-end justify-between gap-6">
        <h1 className="text-4xl">{t('heading')}</h1>
        <SearchField
          className="w-full sm:w-80"
          defaultValue={search}
          key={search}
          label={t('common:actions.search')}
          onSearch={(term) => void navigate({ search: { search: term || undefined } })}
          placeholder={t('searchPlaceholder')}
          submitLabel={t('common:actions.search')}
        />
      </header>

      {view.kind === 'table' && (
        <section aria-label={t('listLabel')} className="grid gap-5">
          <UsersTable users={view.page.items} />
          <nav aria-label={t('pagesLabel')} className="flex items-center justify-between gap-4 text-sm text-muted">
            <span>{t('summary', { count: view.page.total, page: String(view.page.page), pageCount: String(view.pageCount) })}</span>
            <span className="flex gap-2">
              <Link aria-disabled={view.page.page <= 1} className={pageLink} search={{ page: view.page.page - 1, search }} to="/admin/users">
                {t('common:actions.previous')}
              </Link>
              <Link
                aria-disabled={view.page.page >= view.pageCount}
                className={pageLink}
                search={{ page: view.page.page + 1, search }}
                to="/admin/users"
              >
                {t('common:actions.next')}
              </Link>
            </span>
          </nav>
        </section>
      )}
      {view.kind !== 'table' && <StatusNotice tone={noticeTones[view.kind]}>{t(`notices.${view.kind}`)}</StatusNotice>}
    </div>
  );
}
