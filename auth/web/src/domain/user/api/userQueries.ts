import { keepPreviousData, queryOptions } from '@tanstack/react-query';

import type { UserListParams } from '../model/types';
import { listUsers } from './userApi';

export const userQueryKeys = {
  all: ['user'] as const,
  list: (params: UserListParams) => ['user', 'list', params] as const,
};

/** One page of users; keeps showing the previous page while the next one loads. */
export function usersQueryOptions(params: UserListParams) {
  return queryOptions({
    placeholderData: keepPreviousData,
    queryFn: () => listUsers(params),
    queryKey: userQueryKeys.list(params),
  });
}
