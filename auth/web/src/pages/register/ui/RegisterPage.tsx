import { useQuery } from '@tanstack/react-query';
import { getRouteApi, Link } from '@tanstack/react-router';

import { Card, CenteredScreen, Eyebrow, StatusNotice, type StatusTone } from '@vaultly/ui';
import { Trans, useTranslation } from 'react-i18next';

import { authorizationRequestQueryOptions, resolveSignInState, type SignInNotice } from '@/domain/authorization-request';
import { RegistrationForm } from '@/features/register-with-password';

const registerRoute = getRouteApi('/register');

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

/** Creates an account with email and password while signing in to the app that sent the browser here. */
export function RegisterPage() {
  const { t } = useTranslation(['pages/register', 'domain/authorization-request', 'common']);
  const { request: requestId } = registerRoute.useSearch();
  const query = useQuery({ ...authorizationRequestQueryOptions(requestId ?? ''), enabled: requestId !== undefined });
  const state = resolveSignInState(requestId !== undefined, query);
  const request = state.kind === 'ready' && state.request.passwordSignIn ? state.request : null;
  const notice = notices[state.kind === 'ready' ? 'unavailable' : state.kind];

  return (
    <CenteredScreen>
      <Card aria-labelledby="register-heading" size="hero">
        <Eyebrow>{t('common:brand.name')}</Eyebrow>
        <h1 className="text-3xl" id="register-heading">
          {request ? t('heading', { client: request.clientName }) : t('headingWithoutClient')}
        </h1>
        {request !== null && (
          <>
            <RegistrationForm requestId={request.id} />
            <p className="text-center text-sm text-muted">
              <Trans
                components={{
                  action: (
                    <Link
                      className="border-b border-line-strong pb-0.5 text-ink hover:border-accent"
                      search={{ request: request.id }}
                      to="/login"
                    />
                  ),
                }}
                i18nKey="haveAccount"
                t={t}
              />
            </p>
          </>
        )}
        {request === null && <StatusNotice tone={notice.tone}>{t(notice.key)}</StatusNotice>}
      </Card>
    </CenteredScreen>
  );
}
