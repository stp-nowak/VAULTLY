import { createFileRoute, redirect } from '@tanstack/react-router';

// The Identity service has no home page: sign-in always starts from a client app with `/login?request=<id>`.
export const Route = createFileRoute('/')({
  beforeLoad: () => {
    throw redirect({ to: '/logged-out' });
  },
});
