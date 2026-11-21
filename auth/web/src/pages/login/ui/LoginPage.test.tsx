import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { navigateToUrl } from '@/shared/lib';
import { renderRoute, server } from '@/shared/test';

import { LoginPage } from './LoginPage';

vi.mock('@/shared/lib', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/shared/lib')>()),
  navigateToUrl: vi.fn(),
}));

const invalidLinkMessage = 'This sign-in link is no longer valid. Go back to the app and try again.';

function authorizationRequest(overrides: Record<string, unknown> = {}) {
  return {
    clientName: 'Vaultly',
    id: '8f1c',
    providers: [{ id: 'google', name: 'Google', startUrl: '/google/authorize?request=8f1c' }],
    status: 'pending',
    ...overrides,
  };
}

function respondWith(body: ReturnType<typeof authorizationRequest>) {
  server.use(http.get('*/api/authorization-requests/:id', () => HttpResponse.json(body)));
}

function renderLogin(url: string) {
  return renderRoute({ component: LoginPage, initialEntry: url, path: '/login' });
}

describe('LoginPage', () => {
  beforeEach(() => {
    vi.mocked(navigateToUrl).mockClear();
  });

  it('offers the providers of a pending request and starts sign-in with a full-page navigation', async () => {
    respondWith(authorizationRequest());
    await renderLogin('/login?request=8f1c');

    expect(screen.getByRole('status')).toHaveTextContent('Loading…');
    expect(await screen.findByRole('heading', { name: 'Sign in to Vaultly' })).toBeInTheDocument();

    const button = screen.getByRole('button', { name: 'Continue with Google' });
    await userEvent.click(button);

    expect(navigateToUrl).toHaveBeenCalledWith('/google/authorize?request=8f1c');
    expect(button).toBeDisabled();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('keeps request ids that look like numbers as strings', async () => {
    const requestedIds: string[] = [];
    server.use(
      http.get('*/api/authorization-requests/:id', ({ params }) => {
        requestedIds.push(String(params.id));
        return HttpResponse.json(authorizationRequest({ id: params.id }));
      }),
    );
    await renderLogin('/login?request=8e10');

    expect(await screen.findByRole('button', { name: 'Continue with Google' })).toBeInTheDocument();
    expect(requestedIds).toEqual(['8e10']);
  });

  it.each(['expired', 'completed'])('shows the invalid link message for a %s request', async (status) => {
    respondWith(authorizationRequest({ status }));
    await renderLogin('/login?request=8f1c');

    expect(await screen.findByRole('alert')).toHaveTextContent(invalidLinkMessage);
    expect(screen.queryByRole('button')).not.toBeInTheDocument();
  });

  it('shows the invalid link message for an unknown request', async () => {
    await renderLogin('/login?request=unknown');

    expect(await screen.findByRole('alert')).toHaveTextContent(invalidLinkMessage);
    expect(screen.queryByRole('button')).not.toBeInTheDocument();
  });

  it('shows the invalid link message without calling the server when the request is missing', async () => {
    const onRequest = vi.fn();
    server.events.on('request:start', onRequest);
    await renderLogin('/login');

    expect(screen.getByRole('alert')).toHaveTextContent(invalidLinkMessage);
    expect(screen.queryByRole('button')).not.toBeInTheDocument();
    expect(onRequest).not.toHaveBeenCalled();
    server.events.removeAllListeners();
  });

  it('asks to try again when the sign-in page cannot be loaded', async () => {
    server.use(http.get('*/api/authorization-requests/:id', () => new HttpResponse(null, { status: 500 })));
    await renderLogin('/login?request=8f1c');

    expect(await screen.findByRole('alert')).toHaveTextContent("We couldn't load the sign-in page. Please try again.");
    expect(screen.queryByRole('button')).not.toBeInTheDocument();
  });
});
