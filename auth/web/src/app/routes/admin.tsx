import { createFileRoute } from '@tanstack/react-router';

import { consoleAccessQueryOptions } from '@/domain/console-admin';
import { AdminConsoleLayout } from '@/pages/admin-console';

export const Route = createFileRoute('/admin')({
  loader: ({ context }) => context.queryClient.ensureQueryData(consoleAccessQueryOptions()),
  component: AdminConsoleLayout,
});
