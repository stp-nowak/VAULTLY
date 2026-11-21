import type { AuthorizationRequest, AuthorizationRequestStatus, IdentityProvider } from '../model/types';

const statuses: readonly AuthorizationRequestStatus[] = ['pending', 'expired', 'completed'];

function asRecord(value: unknown, message: string): Record<string, unknown> {
  if (!value || typeof value !== 'object' || Array.isArray(value)) {
    throw new Error(message);
  }

  return value as Record<string, unknown>;
}

function requireString(record: Record<string, unknown>, key: string, message: string): string {
  const value = record[key];
  if (typeof value !== 'string' || !value.trim()) {
    throw new Error(message);
  }

  return value;
}

function isStatus(value: unknown): value is AuthorizationRequestStatus {
  return typeof value === 'string' && (statuses as readonly string[]).includes(value);
}

/** Only same-origin paths are accepted, so the page can never navigate to an injected `javascript:` or foreign URL. */
function isSameOriginPath(url: string): boolean {
  return url.startsWith('/') && !url.startsWith('//') && !url.startsWith('/\\');
}

function parseProvider(value: unknown): IdentityProvider {
  const message = 'Authorization request contained an invalid provider.';
  const record = asRecord(value, message);
  const startUrl = requireString(record, 'startUrl', message);

  if (!isSameOriginPath(startUrl)) {
    throw new Error(message);
  }

  return {
    id: requireString(record, 'id', message),
    name: requireString(record, 'name', message),
    startUrl,
  };
}

export function parseAuthorizationRequest(value: unknown): AuthorizationRequest {
  const message = 'Authorization request response was invalid.';
  const record = asRecord(value, message);

  if (!isStatus(record.status) || !Array.isArray(record.providers)) {
    throw new Error(message);
  }

  return {
    clientName: requireString(record, 'clientName', message),
    id: requireString(record, 'id', message),
    providers: record.providers.map(parseProvider),
    status: record.status,
  };
}
