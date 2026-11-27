import { useQuery } from '@tanstack/react-query';

import { StatusNotice, type StatusTone } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

import { authorizationRequestQueryOptions, resolveSignInState, type SignInNotice } from '@/domain/authorization-request';

import { SignInOptions } from './SignInOptions';

type NoticeKey =
  | 'common:states.loading'
  | 'domain/authorization-request:invalidLink'
  | 'notices.loadFailed'
  | 'notices.unavailable';

const notices: Record<SignInNotice, { key: NoticeKey; tone: StatusTone }> = {
  invalid: { key: 'domain/authorization-request:invalidLink', tone: 'error' },
  loadFailed: { key: 'notices.loadFailed', tone: 'error' },
  loading: { key: 'common:states.loading', tone: 'info' },
  unavailable: { key: 'notices.unavailable', tone: 'error' },
};

interface SignInRequestProps {
  requestId: string | undefined;
}

/** Content of the sign-in card for one authorization request: the ways to sign in, or a notice why they are missing. */
export function SignInRequest({ requestId }: SignInRequestProps) {
  const { t } = useTranslation(['pages/login', 'domain/authorization-request', 'common']);
  const query = useQuery({ ...authorizationRequestQueryOptions(requestId ?? ''), enabled: requestId !== undefined });
  const state = resolveSignInState(requestId !== undefined, query);

  const view =
    state.kind === 'ready'
      ? {
          body: <SignInOptions request={state.request} />,
          heading: t('heading', { client: state.request.clientName }),
        }
      : {
          body: <StatusNotice tone={notices[state.kind].tone}>{t(notices[state.kind].key)}</StatusNotice>,
          heading: t('common:actions.signIn'),
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
