/**
 * HTTP client for the Identity API (auth/src). The API serves this SPA, so every call is same-origin and carries the
 * browser's first-party cookies; the SPA itself never handles credentials.
 */

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

  // Read as text first: error responses (e.g. 404) may declare JSON but carry an empty body.
  const text = await response.text();
  if (!text) {
    return null;
  }

  return isJsonResponse(response.headers.get('content-type')) ? (JSON.parse(text) as unknown) : text;
}

function extractErrorMessage(payload: unknown, fallback: string): string {
  if (payload && typeof payload === 'object') {
    const record = payload as Record<string, unknown>;
    for (const key of ['error', 'message', 'title']) {
      const value = record[key];
      if (typeof value === 'string' && value.trim()) {
        return value;
      }
    }
  }

  if (typeof payload === 'string' && payload.trim()) {
    return payload;
  }

  return fallback;
}

/**
 * Calls the Identity API on the same origin and parses the JSON response.
 * Throws {@link HttpError} for non-2xx responses.
 */
export async function fetchJson<T>(input: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers);
  headers.set('Accept', 'application/json');

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
