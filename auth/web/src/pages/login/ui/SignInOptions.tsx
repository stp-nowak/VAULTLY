import { Link } from '@tanstack/react-router';
import { Trans, useTranslation } from 'react-i18next';

import type { AuthorizationRequest } from '@/domain/authorization-request';
import { ProviderButtons } from '@/features/sign-in-with-provider';
import { PasswordSignInForm } from '@/features/sign-in-with-password';

interface SignInOptionsProps {
  request: AuthorizationRequest;
}

/** The ways to sign in a request offers: providers, then email and password with a link to create an account. */
export function SignInOptions({ request }: SignInOptionsProps) {
  const { t } = useTranslation('pages/login');
  const hasProviders = request.providers.length > 0;

  return (
    <div className="grid gap-5">
      {hasProviders && <ProviderButtons providers={request.providers} />}
      {hasProviders && request.passwordSignIn && (
        <p className="flex items-center gap-3 text-xs tracking-label text-muted uppercase before:h-px before:flex-1 before:bg-line after:h-px after:flex-1 after:bg-line">
          {t('or')}
        </p>
      )}
      {request.passwordSignIn && (
        <>
          <PasswordSignInForm requestId={request.id} />
          <p className="text-center text-sm text-muted">
            <Trans
              components={{
                action: (
                  <Link
                    className="border-b border-line-strong pb-0.5 text-ink hover:border-accent"
                    search={{ request: request.id }}
                    to="/register"
                  />
                ),
              }}
              i18nKey="newHere"
              t={t}
            />
          </p>
        </>
      )}
    </div>
  );
}
