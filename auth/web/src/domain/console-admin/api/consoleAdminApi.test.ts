import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';

import { server } from '@/shared/test';

import { getConsoleAccess } from './consoleAdminApi';

const admin = { email: 'admin@example.com', id: 'u-1', name: 'Ada Admin', role: 'admin' };

describe('console admin api', () => {
  it('returns the admin and sends the CSRF header', async () => {
    let csrfHeader: string | null = null;
    server.use(
      http.get('*/api/admin/me', ({ request }) => {
        csrfHeader = request.headers.get('X-CSRF');
        return HttpResponse.json(admin);
      }),
    );

    await expect(getConsoleAccess()).resolves.toEqual({
      admin: { email: 'admin@example.com', id: 'u-1', name: 'Ada Admin' },
      status: 'admin',
    });
    expect(csrfHeader).toBe('1');
  });

  it.each([
    [401, 'signed-out'],
    [403, 'forbidden'],
  ])('maps HTTP %i to %s', async (status, expected) => {
    server.use(http.get('*/api/admin/me', () => new HttpResponse(null, { status })));

    await expect(getConsoleAccess()).resolves.toEqual({ status: expected });
  });

  it('throws for server errors', async () => {
    server.use(http.get('*/api/admin/me', () => new HttpResponse(null, { status: 503 })));

    await expect(getConsoleAccess()).rejects.toMatchObject({ name: 'HttpError', status: 503 });
  });

  it('rejects an invalid admin payload', async () => {
    server.use(http.get('*/api/admin/me', () => HttpResponse.json({ id: 'u-1' })));

    await expect(getConsoleAccess()).rejects.toThrow('Admin console response was invalid.');
  });
});
