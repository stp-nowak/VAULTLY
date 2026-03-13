import { createFileRoute } from '@tanstack/react-router';
import { CallbackRouteView } from '@/route-components/CallbackRouteView';

interface CallbackSearch {
  code?: string;
  error?: string;
  state?: string;
}

export const Route = createFileRoute('/callback')({
  component: CallbackRouteView,
  validateSearch: (search): CallbackSearch => ({
    code: typeof search.code === 'string' ? search.code : undefined,
    error: typeof search.error === 'string' ? search.error : undefined,
    state: typeof search.state === 'string' ? search.state : undefined,
  }),
});
