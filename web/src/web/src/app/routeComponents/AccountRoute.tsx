import { getRouteApi } from '@tanstack/react-router';

import { AccountPage } from '@/pages/account';

const route = getRouteApi('/app/accounts/$accountId');

export function AccountRoute() {
  const { accountId } = route.useParams();
  return <AccountPage accountId={accountId} />;
}
