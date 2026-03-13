import { createRootRouteWithContext } from '@tanstack/react-router';

import { RouteErrorView } from '@/components/RouteErrorView';
import type { AppRouterContext } from '@/router';
import { RootRouteView } from '@/route-components/RootRouteView';

export const Route = createRootRouteWithContext<AppRouterContext>()({
  component: RootRouteView,
  errorComponent: ({ error }) => <RouteErrorView error={error} />,
  notFoundComponent: () => <RouteErrorView error={new Error('This page does not exist.')} />,
});
