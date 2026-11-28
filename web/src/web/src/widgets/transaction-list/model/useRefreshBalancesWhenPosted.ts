import { useQueryClient, type InfiniteData } from '@tanstack/react-query';
import { useEffect, useRef } from 'react';

import { accountQueryKeys } from '@/domain/account';
import { hasPending, type TransactionPage } from '@/domain/transaction';

/** Balances change only once Accounts applied a step, so they are refreshed when the last pending one settles. */
export function useRefreshBalancesWhenPosted(data: InfiniteData<TransactionPage> | undefined) {
  const queryClient = useQueryClient();
  const isPending = hasPending(data);
  const wasPending = useRef(isPending);

  useEffect(() => {
    if (wasPending.current && !isPending) {
      void queryClient.invalidateQueries({ queryKey: accountQueryKeys.all });
    }

    wasPending.current = isPending;
  }, [isPending, queryClient]);
}
