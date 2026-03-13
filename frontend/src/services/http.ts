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

export async function fetchJson<T>(input: RequestInfo | URL, init?: RequestInit): Promise<T> {
  const response = await fetch(input, init);
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
