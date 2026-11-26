import { useQuery } from '@tanstack/react-query';
import { getRouteApi, Link } from '@tanstack/react-router';

import { Card, CenteredScreen, Eyebrow, StatusNotice, type StatusTone } from '@vaultly/ui';

import { authorizationRequestQueryOptions, resolveSignInState, type SignInNotice } from '@/domain/authorization-request';
import { RegistrationForm } from '@/features/register-with-password';

const registerRoute = getRouteApi('/register');

const notices: Record<SignInNotice, { text: string; tone: StatusTone }> = {
  invalid: { text: 'This sign-in link is no longer valid. Go back to the app and try again.', tone: 'error' },
  loadFailed: { text: "We couldn't load this page. Please try again.", tone: 'error' },
  loading: { text: 'Loading…', tone: 'info' },
  unavailable: { text: 'Creating an account with a password is not available.', tone: 'error' },
};

/** Creates an account with email and password while signing in to the app that sent the browser here. */
export function RegisterPage() {
  const { request: requestId } = registerRoute.useSearch();
  const query = useQuery({ ...authorizationRequestQueryOptions(requestId ?? ''), enabled: requestId !== undefined });
  const state = resolveSignInState(requestId !== undefined, query);
  const request = state.kind === 'ready' && state.request.passwordSignIn ? state.request : null;
  const notice = notices[state.kind === 'ready' ? 'unavailable' : state.kind];

  return (
    <CenteredScreen>
      <Card aria-labelledby="register-heading" size="hero">
        <Eyebrow>Vaultly</Eyebrow>
        <h1 className="text-3xl" id="register-heading">
          {request ? `Create your ${request.clientName} account` : 'Create an account'}
        </h1>
        {request !== null && (
          <>
            <RegistrationForm requestId={request.id} />
            <p className="text-center text-sm text-muted">
              Already have an account?{' '}
              <Link className="border-b border-line-strong pb-0.5 text-ink hover:border-accent" search={{ request: request.id }} to="/login">
                Sign in
              </Link>
            </p>
          </>
        )}
        {request === null && <StatusNotice tone={notice.tone}>{notice.text}</StatusNotice>}
      </Card>
    </CenteredScreen>
  );
}
