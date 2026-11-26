import { queryOptions } from '@tanstack/react-query';

import { listSignInMethods } from './signInSettingsApi';

export const signInSettingsQueryKeys = {
  all: ['sign-in-settings'] as const,
};

export function signInMethodsQueryOptions() {
  return queryOptions({
    queryFn: listSignInMethods,
    queryKey: signInSettingsQueryKeys.all,
  });
}
