import { useMutation, useQueryClient } from '@tanstack/react-query';

import { Select } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

import { consoleAdminQueryKeys } from '@/domain/console-admin';
import { userQueryKeys, type UserRole, type UserSummary } from '@/domain/user';

import { assignUserRole } from '../api/assignUserRole';
import { roleChangeErrorKey } from '../model/roleChangeError';

const roles: readonly UserRole[] = ['user', 'admin'];

interface UserRoleSelectProps {
  user: UserSummary;
}

/** Changes a user's role as soon as another one is picked. */
export function UserRoleSelect({ user }: UserRoleSelectProps) {
  const { t } = useTranslation('features/assign-user-role');
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
        aria-label={t('roleOf', { email: user.email })}
        className="w-36"
        disabled={mutation.isPending}
        onValueChange={(role) => mutation.mutate(role)}
        options={roles.map((role) => ({ label: t(`roles.${role}`), value: role }))}
        value={mutation.isPending ? mutation.variables : user.role}
      />
      {mutation.isError && (
        <p className="max-w-64 text-sm text-danger" role="alert">
          {t(roleChangeErrorKey(mutation.error))}
        </p>
      )}
    </div>
  );
}
