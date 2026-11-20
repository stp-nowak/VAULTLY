import { getRouteApi } from '@tanstack/react-router';

import { LoginPanel } from '@/features/login';

const loginRoute = getRouteApi('/login');

/**
 * Login page. Reads `error`/`returnUrl` validated by `app/routes/login.tsx` and composes the login feature.
 */
export function LoginPage() {
  const { error, returnUrl } = loginRoute.useSearch();
  return <LoginPanel error={error} returnUrl={returnUrl} />;
}
