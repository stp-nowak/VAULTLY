import { createRootRouteWithContext } from '@tanstack/react-router';

import { RootLayout } from '@/app/layouts/RootLayout';
import type { AppRouterContext } from '@/app/router';
import { RouteErrorPage } from '@/pages/route-error';

export const Route = createRootRouteWithContext<AppRouterContext>()({
  component: RootLayout,
  errorComponent: ({ error }) => <RouteErrorPage error={error} />,
  notFoundComponent: () => <RouteErrorPage error={new Error('This page does not exist.')} />,
});
