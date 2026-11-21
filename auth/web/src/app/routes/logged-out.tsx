import { createFileRoute } from '@tanstack/react-router';

import { LoggedOutPage } from '@/pages/logged-out';

export const Route = createFileRoute('/logged-out')({
  component: LoggedOutPage,
});
