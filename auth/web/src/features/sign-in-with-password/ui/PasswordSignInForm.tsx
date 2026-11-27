import { useMutation } from '@tanstack/react-query';

import { Button, StatusNotice, TextField } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

import { navigateToUrl } from '@/shared/lib';

import { signInWithPassword, type PasswordCredentials } from '../api/signInWithPassword';
import { signInErrorKey } from '../model/signInError';

interface PasswordSignInFormProps {
  requestId: string;
}

/** Email and password sign-in; on success the browser leaves for the app that asked for the sign-in. */
export function PasswordSignInForm({ requestId }: PasswordSignInFormProps) {
  const { t } = useTranslation(['features/sign-in-with-password', 'domain/authorization-request', 'common']);
  const mutation = useMutation({
    mutationFn: (credentials: PasswordCredentials) => signInWithPassword(requestId, credentials),
    onSuccess: (redirectUrl) => navigateToUrl(redirectUrl),
  });
  const isBusy = mutation.isPending || mutation.isSuccess;

  return (
    <form
      aria-label={t('formLabel')}
      className="grid gap-4"
      onSubmit={(event) => {
        event.preventDefault();
        const form = new FormData(event.currentTarget);
        mutation.mutate({ email: String(form.get('email') ?? ''), password: String(form.get('password') ?? '') });
      }}
    >
      <TextField autoComplete="username" label={t('common:fields.email')} name="email" required type="email" />
      <TextField
        autoComplete="current-password"
        label={t('common:fields.password')}
        name="password"
        required
        type="password"
      />
      {mutation.isError && <StatusNotice tone="error">{t(signInErrorKey(mutation.error))}</StatusNotice>}
      <Button disabled={isBusy} type="submit">
        {isBusy ? t('signingIn') : t('common:actions.signIn')}
      </Button>
    </form>
  );
}
