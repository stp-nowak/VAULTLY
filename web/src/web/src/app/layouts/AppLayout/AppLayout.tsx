import { Outlet } from '@tanstack/react-router';

import { AppShell } from '@/widgets/app-shell';

/** Every signed-in screen: the app shell around the current page. */
export function AppLayout() {
  return (
    <AppShell>
      <Outlet />
    </AppShell>
  );
}
