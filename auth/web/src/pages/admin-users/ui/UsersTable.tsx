import { Badge, Table } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

import type { UserSummary } from '@/domain/user';
import { UserRoleSelect } from '@/features/assign-user-role';
import { useIntlLocale } from '@/shared/i18n';

interface UsersTableProps {
  users: UserSummary[];
}

export function UsersTable({ users }: UsersTableProps) {
  const { t } = useTranslation('pages/admin-users');
  const joinedFormat = new Intl.DateTimeFormat(useIntlLocale(), { dateStyle: 'medium' });

  return (
    <Table aria-label={t('table.label')}>
      <thead>
        <tr>
          <th scope="col">{t('table.user')}</th>
          <th scope="col">{t('table.role')}</th>
          <th scope="col">{t('table.signsInWith')}</th>
          <th scope="col">{t('table.joined')}</th>
        </tr>
      </thead>
      <tbody>
        {users.map((user) => (
          <tr key={user.id}>
            <td>
              <p className="font-bold">{user.name}</p>
              <p className="text-sm text-muted">{user.email}</p>
            </td>
            <td>
              <UserRoleSelect user={user} />
            </td>
            <td>
              <div className="flex flex-wrap gap-1.5">
                {user.signInMethods.map((method) => (
                  <Badge key={method}>{method}</Badge>
                ))}
                {user.signInMethods.length === 0 && <span className="text-sm text-muted">{t('table.notSignedIn')}</span>}
              </div>
            </td>
            <td className="text-sm whitespace-nowrap text-muted">{joinedFormat.format(new Date(user.createdAt))}</td>
          </tr>
        ))}
      </tbody>
    </Table>
  );
}
