import { QueryClientProvider, type QueryClient } from '@tanstack/react-query';
import { RouterProvider } from '@tanstack/react-router';
import { useEffect } from 'react';

import { AuthProvider, useAuth } from '@/domain/identity';

import { queryClient as appQueryClient } from './providers/queryClient';
import { router as appRouter, type AppRouter } from './router';

interface AppProps {
  queryClient?: QueryClient;
  router?: AppRouter;
}

function AuthenticatedRouter({ queryClient, router }: Required<AppProps>) {
  const auth = useAuth();

  useEffect(() => {
    void router.invalidate();
  }, [auth.errorMessage, auth.isAuthenticated, auth.sessionId, auth.status, router]);

  return <RouterProvider router={router} context={{ auth, queryClient }} />;
}

export function App({ queryClient = appQueryClient, router = appRouter }: AppProps) {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <AuthenticatedRouter queryClient={queryClient} router={router} />
      </AuthProvider>
    </QueryClientProvider>
  );
}
