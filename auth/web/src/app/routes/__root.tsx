import { createRootRouteWithContext } from '@tanstack/react-router';

import { RootLayout } from '@/app/layouts/RootLayout';
import type { AppRouterContext } from '@/app/router';
import { RouteErrorPage } from '@/pages/route-error';

export const Route = createRootRouteWithContext<AppRouterContext>()({
  component: RootLayout,
  errorComponent: () => <RouteErrorPage />,
  notFoundComponent: () => <RouteErrorPage message="This page does not exist." />,
});
