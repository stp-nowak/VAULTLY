import { useMutation, useQueryClient } from '@tanstack/react-query';

import { Select, type SelectOption } from '@vaultly/ui';

import { consoleAdminQueryKeys } from '@/domain/console-admin';
import { userQueryKeys, type UserRole, type UserSummary } from '@/domain/user';

import { assignUserRole } from '../api/assignUserRole';
import { roleChangeErrorMessage } from '../model/roleChangeError';

const roleOptions: readonly SelectOption<UserRole>[] = [
  { label: 'User', value: 'user' },
  { label: 'Admin', value: 'admin' },
];

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

  return (
    <div className="grid gap-2">
      <Select
        aria-label={`Role of ${user.email}`}
        className="w-36"
        disabled={mutation.isPending}
        onValueChange={(role) => mutation.mutate(role)}
        options={roleOptions}
        value={mutation.isPending ? mutation.variables : user.role}
      />
      {mutation.isError && (
        <p className="max-w-64 text-sm text-danger" role="alert">
          {roleChangeErrorMessage(mutation.error)}
        </p>
      )}
    </div>
  );
}
