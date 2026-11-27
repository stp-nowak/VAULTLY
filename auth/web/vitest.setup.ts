import '@testing-library/jest-dom/vitest';

import { afterAll, afterEach, beforeAll } from 'vitest';

import { server } from '@/shared/test/msw/server';

beforeAll(async () => {
  server.listen();
  // Loaded after the test file's `vi.mock` calls, so the slices it pulls in see the mocks; jsdom only.
  if (typeof document !== 'undefined') {
    await import('@/app/providers/i18n');
  }
});
afterEach(() => server.resetHandlers());
afterAll(() => server.close());
