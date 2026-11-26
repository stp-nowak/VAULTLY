import { useMutation } from '@tanstack/react-query';

import { Button, StatusNotice, TextField } from '@vaultly/ui';

import { navigateToUrl } from '@/shared/lib';

import { signInWithPassword, type PasswordCredentials } from '../api/signInWithPassword';
import { signInErrorMessage } from '../model/signInError';

interface PasswordSignInFormProps {
  requestId: string;
}

/** Email and password sign-in; on success the browser leaves for the app that asked for the sign-in. */
export function PasswordSignInForm({ requestId }: PasswordSignInFormProps) {
  const mutation = useMutation({
    mutationFn: (credentials: PasswordCredentials) => signInWithPassword(requestId, credentials),
    onSuccess: (redirectUrl) => navigateToUrl(redirectUrl),
  });
  const isBusy = mutation.isPending || mutation.isSuccess;

  return (
    <form
      aria-label="Sign in with email"
      className="grid gap-4"
      onSubmit={(event) => {
        event.preventDefault();
        const form = new FormData(event.currentTarget);
        mutation.mutate({ email: String(form.get('email') ?? ''), password: String(form.get('password') ?? '') });
      }}
    >
      <TextField autoComplete="username" label="Email" name="email" required type="email" />
      <TextField autoComplete="current-password" label="Password" name="password" required type="password" />
      {mutation.isError && <StatusNotice tone="error">{signInErrorMessage(mutation.error)}</StatusNotice>}
      <Button disabled={isBusy} type="submit">
        {isBusy ? 'Signing in…' : 'Sign in'}
      </Button>
    </form>
  );
}
