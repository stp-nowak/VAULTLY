import { useState } from 'react';

import { Button, Card, Eyebrow, InfoTile, StatusNotice } from '@vaultly/ui';

import { useAuth } from '@/domain/identity';

export function LogoutCard() {
  const auth = useAuth();
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  return (
    <Card aria-labelledby="account-heading">
      <div>
        <Eyebrow>Account</Eyebrow>
        <h2 className="text-2xl" id="account-heading">
          Your account
        </h2>
      </div>

      <dl className="grid gap-3.5">
        <InfoTile label="Signed in as" value={<strong>{auth.user?.email ?? 'Unknown user'}</strong>} />
      </dl>

      {errorMessage ? <StatusNotice tone="error">{errorMessage}</StatusNotice> : null}

      <Button
        disabled={isSubmitting}
        onClick={() => {
          setErrorMessage(null);
          setIsSubmitting(true);

          void auth
            .logout()
            .catch((error: unknown) => {
              setErrorMessage(error instanceof Error ? error.message : 'Logout failed.');
            })
            .finally(() => {
              setIsSubmitting(false);
            });
        }}
        variant="secondary"
      >
        {isSubmitting ? 'Signing out…' : 'Sign out'}
      </Button>
    </Card>
  );
}
