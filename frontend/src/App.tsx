import { QueryClientProvider } from '@tanstack/react-query';
import { RouterProvider } from '@tanstack/react-router';
import { useEffect } from 'react';

import { AuthProvider } from '@/features/auth/context/AuthProvider';
import { useAuth } from '@/features/auth/context/useAuth';
import { queryClient } from '@/lib/queryClient';
import { router } from '@/router';

function AppRouter() {
  const auth = useAuth();

  useEffect(() => {
    void router.invalidate();
  }, [auth.errorMessage, auth.isAuthenticated, auth.sessionId, auth.status]);

  return <RouterProvider router={router} context={{ auth, queryClient }} />;
}

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <AppRouter />
      </AuthProvider>
    </QueryClientProvider>
  );
}
