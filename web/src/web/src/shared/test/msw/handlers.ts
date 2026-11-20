import { http, HttpResponse } from 'msw';

/** Default web API handlers for tests: no active session. Override per test with `server.use(...)`. */
export const handlers = [
  http.get('*/api/auth/user', () => new HttpResponse(null, { status: 401 })),
  http.get('*/api/auth/sessions', () => new HttpResponse(null, { status: 401 })),
  http.post('*/api/auth/logout', () => new HttpResponse(null, { status: 204 })),
];
