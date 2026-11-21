import { useQuery } from '@tanstack/react-query';

import { StatusNotice, type StatusTone } from '@vaultly/ui';

import { authorizationRequestQueryOptions } from '@/domain/authorization-request';
import { ProviderButtons } from '@/features/sign-in-with-provider';

import { resolveSignInState, type SignInNotice } from '../model/signInState';

const notices: Record<SignInNotice, { text: string; tone: StatusTone }> = {
  invalid: { text: 'This sign-in link is no longer valid. Go back to the app and try again.', tone: 'error' },
  loadFailed: { text: "We couldn't load the sign-in page. Please try again.", tone: 'error' },
  loading: { text: 'Loading…', tone: 'info' },
};

interface SignInRequestProps {
  requestId: string | undefined;
}

/** Content of the sign-in card for one authorization request: the provider choice, or a notice why it is missing. */
export function SignInRequest({ requestId }: SignInRequestProps) {
  const query = useQuery({ ...authorizationRequestQueryOptions(requestId ?? ''), enabled: requestId !== undefined });
  const state = resolveSignInState(requestId !== undefined, query);

  const view =
    state.kind === 'ready'
      ? {
          body: <ProviderButtons providers={state.request.providers} />,
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
