import { afterEach, describe, expect, it, vi } from 'vitest';

import { buildLoginUrl, getCurrentUser, listSessions } from './identityApi';

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), { headers: { 'Content-Type': 'application/json' }, status });
}

describe('identity api', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('loads the current user with the session cookie and CSRF header', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      jsonResponse({ sessionId: 'session-1', user: { email: 'hello@vaultly.test', id: 'user-1', name: 'Vaultly User' } }),
    );
    vi.stubGlobal('fetch', fetchMock);

    const current = await getCurrentUser();

    expect(current).toEqual({ sessionId: 'session-1', user: { email: 'hello@vaultly.test', id: 'user-1', name: 'Vaultly User' } });
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toBe('/api/auth/user');
    expect(init.credentials).toBe('same-origin');
    expect(new Headers(init.headers).get('X-CSRF')).toBe('1');
  });

  it('returns null when there is no session', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 401 })));

    await expect(getCurrentUser()).resolves.toBeNull();
  });

  it('returns null for a 401 that declares JSON but has an empty body', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(new Response('', { headers: { 'Content-Type': 'application/json' }, status: 401 })),
    );

    await expect(getCurrentUser()).resolves.toBeNull();
  });

  it('parses the sessions list', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        jsonResponse({
          sessions: [
            { createdAt: '2026-11-21T10:00:00Z', id: 's-1', isCurrent: true, isRevoked: false, lastUsedAt: '2026-11-21T11:00:00Z' },
          ],
        }),
      ),
    );

    const sessions = await listSessions();

    expect(sessions).toHaveLength(1);
    expect(sessions[0]).toMatchObject({ id: 's-1', isCurrent: true, userAgent: null });
  });

  it('builds a login URL that never contains tokens, only the local return path', () => {
    expect(buildLoginUrl('/app/budgets?tab=1')).toBe('/api/auth/login?returnUrl=%2Fapp%2Fbudgets%3Ftab%3D1');
    expect(buildLoginUrl()).toBe('/api/auth/login');
  });
});
