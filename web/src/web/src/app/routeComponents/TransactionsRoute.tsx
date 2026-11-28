import { getRouteApi } from '@tanstack/react-router';

import { TransactionsPage } from '@/pages/transactions';

const route = getRouteApi('/app/transactions');

export function TransactionsRoute() {
  const filters = route.useSearch();
  const navigate = route.useNavigate();

  return <TransactionsPage filters={filters} onFiltersChange={(search) => void navigate({ replace: true, search })} />;
}
