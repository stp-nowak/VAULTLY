import { useMutation } from '@tanstack/react-query';

import { Button, StatusNotice, TextField } from '@vaultly/ui';

import { navigateToUrl } from '@/shared/lib';

import { registerWithPassword, type Registration } from '../api/registerWithPassword';
import { minPasswordLength, registrationErrorMessage } from '../model/registrationError';

interface RegistrationFormProps {
  requestId: string;
}

/** Creates an account with name, email and password; on success the browser leaves for the app. */
export function RegistrationForm({ requestId }: RegistrationFormProps) {
  const mutation = useMutation({
    mutationFn: (registration: Registration) => registerWithPassword(requestId, registration),
    onSuccess: (redirectUrl) => navigateToUrl(redirectUrl),
  });
  const isBusy = mutation.isPending || mutation.isSuccess;

  return (
    <form
      aria-label="Create an account"
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
      <TextField autoComplete="name" label="Name" name="name" required />
      <TextField autoComplete="email" label="Email" name="email" required type="email" />
      <TextField
        autoComplete="new-password"
        label="Password"
        maxLength={128}
        minLength={minPasswordLength}
        name="password"
        required
        type="password"
      />
      <p className="-mt-2 text-sm text-muted">At least {minPasswordLength} characters.</p>
      {mutation.isError && <StatusNotice tone="error">{registrationErrorMessage(mutation.error)}</StatusNotice>}
      <Button disabled={isBusy} type="submit">
        {isBusy ? 'Creating your account…' : 'Create account'}
      </Button>
    </form>
  );
}
