import { useMutation } from '@tanstack/react-query';

import { Button, StatusNotice, TextField } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

import { navigateToUrl } from '@/shared/lib';

import { registerWithPassword, type Registration } from '../api/registerWithPassword';
import { minPasswordLength, registrationErrorKey } from '../model/registrationError';

interface RegistrationFormProps {
  requestId: string;
}

/** Creates an account with name, email and password; on success the browser leaves for the app. */
export function RegistrationForm({ requestId }: RegistrationFormProps) {
  const { t } = useTranslation(['features/register-with-password', 'domain/authorization-request', 'common']);
  const mutation = useMutation({
    mutationFn: (registration: Registration) => registerWithPassword(requestId, registration),
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
        mutation.mutate({
          email: String(form.get('email') ?? ''),
          name: String(form.get('name') ?? ''),
          password: String(form.get('password') ?? ''),
        });
      }}
    >
      <TextField autoComplete="name" label={t('common:fields.name')} name="name" required />
      <TextField autoComplete="email" label={t('common:fields.email')} name="email" required type="email" />
      <TextField
        autoComplete="new-password"
        label={t('common:fields.password')}
        maxLength={128}
        minLength={minPasswordLength}
        name="password"
        required
        type="password"
      />
      <p className="-mt-2 text-sm text-muted">{t('passwordHint', { min: String(minPasswordLength) })}</p>
      {mutation.isError && (
        <StatusNotice tone="error">{t(registrationErrorKey(mutation.error), { min: String(minPasswordLength) })}</StatusNotice>
      )}
      <Button disabled={isBusy} type="submit">
        {isBusy ? t('creating') : t('submit')}
      </Button>
    </form>
  );
}
