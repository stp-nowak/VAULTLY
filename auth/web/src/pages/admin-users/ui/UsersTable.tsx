import { Badge, Table } from '@vaultly/ui';

import type { UserSummary } from '@/domain/user';
import { UserRoleSelect } from '@/features/assign-user-role';

const joinedFormat = new Intl.DateTimeFormat('en-GB', { dateStyle: 'medium' });

interface UsersTableProps {
  users: UserSummary[];
}

export function UsersTable({ users }: UsersTableProps) {
  return (
    <Table aria-label="Users">
      <thead>
        <tr>
          <th scope="col">User</th>
          <th scope="col">Role</th>
          <th scope="col">Signs in with</th>
          <th scope="col">Joined</th>
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
                {user.signInMethods.length === 0 && <span className="text-sm text-muted">Not signed in yet</span>}
              </div>
            </td>
            <td className="text-sm whitespace-nowrap text-muted">{joinedFormat.format(new Date(user.createdAt))}</td>
          </tr>
        ))}
      </tbody>
    </Table>
  );
}
