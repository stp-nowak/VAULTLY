import { useMutation, useQueryClient } from '@tanstack/react-query';

import { Select } from '@vaultly/ui';

import { consoleAdminQueryKeys } from '@/domain/console-admin';
import { isUserRole, userQueryKeys, type UserRole, type UserSummary } from '@/domain/user';

import { assignUserRole } from '../api/assignUserRole';
import { roleChangeErrorMessage } from '../model/roleChangeError';

const roleLabels: Record<UserRole, string> = {
  admin: 'Admin',
  user: 'User',
};

interface UserRoleSelectProps {
  user: UserSummary;
}

/** Changes a user's role as soon as another one is picked. */
export function UserRoleSelect({ user }: UserRoleSelectProps) {
  const queryClient = useQueryClient();
  const mutation = useMutation({
    mutationFn: (role: UserRole) => assignUserRole(user.id, role),
    // The signed-in admin may have changed their own role, so their access is re-checked too.
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: userQueryKeys.all }),
        queryClient.invalidateQueries({ queryKey: consoleAdminQueryKeys.all }),
      ]),
  });
  const shownRole = mutation.isPending ? mutation.variables : user.role;

  return (
    <div className="grid gap-2">
      <Select
        aria-label={`Role of ${user.email}`}
        className="w-36"
        disabled={mutation.isPending}
        onChange={(event) => {
          if (isUserRole(event.target.value)) {
            mutation.mutate(event.target.value);
          }
        }}
        value={shownRole}
      >
        {Object.entries(roleLabels).map(([role, label]) => (
          <option key={role} value={role}>
            {label}
          </option>
        ))}
      </Select>
      {mutation.isError ? (
        <p className="max-w-64 text-sm text-danger" role="alert">
          {roleChangeErrorMessage(mutation.error)}
        </p>
      ) : null}
    </div>
  );
}
