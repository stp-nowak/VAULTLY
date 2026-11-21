import { createFileRoute } from '@tanstack/react-router';

import { SignInErrorPage } from '@/pages/sign-in-error';
import { readSearchString } from '@/shared/lib';

interface ErrorSearch {
  code?: string;
}

export const Route = createFileRoute('/error')({
  validateSearch: (search): ErrorSearch => ({ code: readSearchString(search.code) }),
  component: SignInErrorPage,
});
