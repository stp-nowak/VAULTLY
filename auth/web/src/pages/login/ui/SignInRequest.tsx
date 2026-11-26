import { useQuery } from '@tanstack/react-query';

import { StatusNotice, type StatusTone } from '@vaultly/ui';

import { authorizationRequestQueryOptions, resolveSignInState, type SignInNotice } from '@/domain/authorization-request';

import { SignInOptions } from './SignInOptions';

const notices: Record<SignInNotice, { text: string; tone: StatusTone }> = {
  invalid: { text: 'This sign-in link is no longer valid. Go back to the app and try again.', tone: 'error' },
  loadFailed: { text: "We couldn't load the sign-in page. Please try again.", tone: 'error' },
  loading: { text: 'Loading…', tone: 'info' },
  unavailable: { text: 'Signing in is not available right now. Please try again later.', tone: 'error' },
};

interface SignInRequestProps {
  requestId: string | undefined;
}

/** Content of the sign-in card for one authorization request: the ways to sign in, or a notice why they are missing. */
export function SignInRequest({ requestId }: SignInRequestProps) {
  const query = useQuery({ ...authorizationRequestQueryOptions(requestId ?? ''), enabled: requestId !== undefined });
  const state = resolveSignInState(requestId !== undefined, query);

  const view =
    state.kind === 'ready'
      ? {
          body: <SignInOptions request={state.request} />,
          heading: `Sign in to ${state.request.clientName}`,
        }
      : {
          body: <StatusNotice tone={notices[state.kind].tone}>{notices[state.kind].text}</StatusNotice>,
          heading: 'Sign in',
        };

  return (
    <>
      <h1 className="text-3xl" id="sign-in-heading">
        {view.heading}
      </h1>
      {view.body}
    </>
  );
}
