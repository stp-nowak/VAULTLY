import { http, HttpResponse } from 'msw';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { server } from '@/shared/test';

import { getAuthorizationRequest } from './authorizationRequestApi';

const pendingRequest = {
  clientName: 'Vaultly',
  id: '8f1c',
  providers: [{ id: 'google', name: 'Google', startUrl: '/google/authorize?request=8f1c' }],
  status: 'pending',
};

describe('authorization request api', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('loads the request from the same origin with same-origin credentials', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify(pendingRequest), { headers: { 'Content-Type': 'application/json' }, status: 200 }),
    );
    vi.stubGlobal('fetch', fetchMock);

    await getAuthorizationRequest('8f1c');

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toBe('/api/authorization-requests/8f1c');
    expect(init.credentials).toBe('same-origin');
  });

  it('encodes the request id into the path', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 404 }));
    vi.stubGlobal('fetch', fetchMock);

    await getAuthorizationRequest('a/b?c');

    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/authorization-requests/a%2Fb%3Fc');
  });

  it('parses a pending request with its providers', async () => {
    server.use(http.get('*/api/authorization-requests/:id', () => HttpResponse.json(pendingRequest)));

    await expect(getAuthorizationRequest('8f1c')).resolves.toEqual(pendingRequest);
  });

  it('parses expired and completed requests', async () => {
    server.use(
      http.get('*/api/authorization-requests/:id', ({ params }) =>
        HttpResponse.json({ ...pendingRequest, id: params.id, providers: [], status: params.id }),
      ),
    );

    await expect(getAuthorizationRequest('expired')).resolves.toMatchObject({ status: 'expired' });
    await expect(getAuthorizationRequest('completed')).resolves.toMatchObject({ status: 'completed' });
  });

  it('returns null for an unknown request (404), even with an empty JSON body', async () => {
    server.use(
      http.get(
        '*/api/authorization-requests/:id',
        () => new HttpResponse('', { headers: { 'Content-Type': 'application/json' }, status: 404 }),
      ),
    );

    await expect(getAuthorizationRequest('unknown')).resolves.toBeNull();
  });

  it('throws for server errors', async () => {
    server.use(http.get('*/api/authorization-requests/:id', () => new HttpResponse(null, { status: 503 })));

    await expect(getAuthorizationRequest('8f1c')).rejects.toMatchObject({ name: 'HttpError', status: 503 });
  });

  it('rejects an unknown status', async () => {
    server.use(
      http.get('*/api/authorization-requests/:id', () => HttpResponse.json({ ...pendingRequest, status: 'weird' })),
    );

    await expect(getAuthorizationRequest('8f1c')).rejects.toThrow('Authorization request response was invalid.');
  });

  it.each(['javascript:alert(1)', 'https://evil.example/authorize', '//evil.example/authorize'])(
    'rejects a provider start URL that leaves the origin: %s',
    async (startUrl) => {
      server.use(
        http.get('*/api/authorization-requests/:id', () =>
          HttpResponse.json({ ...pendingRequest, providers: [{ id: 'google', name: 'Google', startUrl }] }),
        ),
      );

      await expect(getAuthorizationRequest('8f1c')).rejects.toThrow('Authorization request contained an invalid provider.');
    },
  );
});
