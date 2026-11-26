import { useMutation, useQueryClient } from '@tanstack/react-query';

import { Switch } from '@vaultly/ui';

import { signInSettingsQueryKeys, type SignInMethod } from '@/domain/sign-in-settings';

import { setSignInMethodEnabled } from '../api/setSignInMethodEnabled';
import { toggleErrorMessage } from '../model/toggleError';

interface SignInMethodSwitchProps {
  method: SignInMethod;
}

/** Switches a sign-in method on or off right away. Methods this deployment is not set up for cannot be switched. */
export function SignInMethodSwitch({ method }: SignInMethodSwitchProps) {
  const queryClient = useQueryClient();
  const mutation = useMutation({
    mutationFn: (enabled: boolean) => setSignInMethodEnabled(method.id, enabled),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: signInSettingsQueryKeys.all }),
  });

  return (
    <div className="grid justify-items-end gap-2">
      <Switch
        aria-label={method.name}
        checked={method.isConfigured && (mutation.isPending ? mutation.variables : method.isEnabled)}
        disabled={!method.isConfigured || mutation.isPending}
        onCheckedChange={(enabled) => mutation.mutate(enabled)}
      />
      {mutation.isError ? (
        <p className="max-w-64 text-right text-sm text-danger" role="alert">
          {toggleErrorMessage(mutation.error)}
        </p>
      ) : null}
    </div>
  );
}
