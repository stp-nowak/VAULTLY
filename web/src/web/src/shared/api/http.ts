/**
 * HTTP client for the web API (same origin). The browser authenticates only with the HttpOnly session cookie;
 * tokens never reach JavaScript.
 */

/** Anti-forgery header required by the web API on every `/api` call (see CsrfHeaderMiddleware). */
export const CSRF_HEADER = { name: 'X-CSRF', value: '1' } as const;

export class HttpError extends Error {
  public readonly payload: unknown;
  public readonly status: number;

  public constructor(status: number, message: string, payload: unknown) {
    super(message);
    this.name = 'HttpError';
    this.payload = payload;
    this.status = status;
  }
}

function isJsonResponse(contentType: string | null): boolean {
  return contentType?.toLowerCase().includes('application/json') ?? false;
}

async function parseResponseBody(response: Response): Promise<unknown> {
  if (response.status === 204) {
    return null;
  }

  const contentType = response.headers.get('content-type');
  if (isJsonResponse(contentType)) {
    return response.json();
  }

  const text = await response.text();
  return text || null;
}

function extractErrorMessage(payload: unknown, fallback: string): string {
  if (payload && typeof payload === 'object') {
    const record = payload as Record<string, unknown>;
    if (typeof record.error === 'string' && record.error.trim()) {
      return record.error;
    }

    if (typeof record.message === 'string' && record.message.trim()) {
      return record.message;
    }
  }

  if (typeof payload === 'string' && payload.trim()) {
    return payload;
  }

  return fallback;
}

/**
 * Calls the web API with the session cookie and the CSRF header, and parses the JSON response.
 * Throws {@link HttpError} for non-2xx responses.
 */
export async function fetchJson<T>(input: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers);
  headers.set(CSRF_HEADER.name, CSRF_HEADER.value);

  const response = await fetch(input, { ...init, credentials: 'same-origin', headers });
  const payload = await parseResponseBody(response);

  if (!response.ok) {
    throw new HttpError(
      response.status,
      extractErrorMessage(payload, `Request failed with status ${response.status}.`),
      payload,
    );
  }

  return payload as T;
}
