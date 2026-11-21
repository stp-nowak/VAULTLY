import { http, HttpResponse } from 'msw';

/** Default Identity API handlers for tests: every authorization request is unknown. Override with `server.use(...)`. */
export const handlers = [http.get('*/api/authorization-requests/:id', () => new HttpResponse(null, { status: 404 }))];
