import type {
  AuthSession,
  AuthUser,
  IdentityDiscoveryDocument,
  TokenExchangeResult,
} from '@/types/auth';

function getNumber(record: Record<string, unknown>, ...keys: string[]): number | null {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === 'number' && Number.isFinite(value)) {
      return value;
    }
  }

  return null;
}

function getString(record: Record<string, unknown>, ...keys: string[]): string | null {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === 'string' && value.trim()) {
      return value;
    }
  }

  return null;
}

function parseUser(value: unknown): AuthUser {
  if (!value || typeof value !== 'object') {
    throw new Error('Token response did not include a valid user payload.');
  }

  const record = value as Record<string, unknown>;
  const id = getString(record, 'id', 'Id');
  const email = getString(record, 'email', 'Email');

  if (!id || !email) {
    throw new Error('Token response did not include a valid user payload.');
  }

  const name = getString(record, 'name', 'Name') ?? email;

  return { email, id, name };
}

export function parseDiscoveryDocument(value: unknown): IdentityDiscoveryDocument {
  if (!value || typeof value !== 'object') {
    throw new Error('Discovery document response was invalid.');
  }

  const record = value as Record<string, unknown>;
  const issuer = getString(record, 'issuer');
  const authorizationEndpoint = getString(record, 'authorization_endpoint');
  const tokenEndpoint = getString(record, 'token_endpoint');
  const jwksUri = getString(record, 'jwks_uri');

  if (!issuer || !authorizationEndpoint || !tokenEndpoint || !jwksUri) {
    throw new Error('Discovery document response was missing required fields.');
  }

  return {
    authorizationEndpoint,
    issuer,
    jwksUri,
    tokenEndpoint,
  };
}

export function parseSessionsResponse(value: unknown): AuthSession[] {
  const source =
    value && typeof value === 'object' && Array.isArray((value as Record<string, unknown>).sessions)
      ? (value as Record<string, unknown>).sessions
      : value;

  if (!Array.isArray(source)) {
    throw new Error('Sessions response was invalid.');
  }

  return source.map((session) => {
    if (!session || typeof session !== 'object') {
      throw new Error('Sessions response contained an invalid session.');
    }

    const record = session as Record<string, unknown>;
    const id = getString(record, 'id', 'Id');
    const createdAt = getString(record, 'createdAt', 'CreatedAt');
    const lastUsedAt = getString(record, 'lastUsedAt', 'LastUsedAt') ?? createdAt;

    if (!id || !createdAt || !lastUsedAt) {
      throw new Error('Sessions response contained an invalid session.');
    }

    return {
      createdAt,
      id,
      ipAddress: getString(record, 'ipAddress', 'IpAddress'),
      isCurrent: Boolean(record.isCurrent ?? record.IsCurrent),
      isRevoked: Boolean(record.isRevoked ?? record.IsRevoked),
      lastUsedAt,
      userAgent: getString(record, 'userAgent', 'UserAgent'),
    };
  });
}

export function parseTokenExchangeResult(value: unknown): TokenExchangeResult {
  if (!value || typeof value !== 'object') {
    throw new Error('Token response was invalid.');
  }

  const record = value as Record<string, unknown>;
  const accessToken = getString(record, 'accessToken', 'AccessToken');
  const sessionId = getString(record, 'sessionId', 'SessionId');
  const expiresIn = getNumber(record, 'expiresIn', 'ExpiresIn');

  if (!accessToken || !sessionId || expiresIn === null) {
    throw new Error('Token response was missing required fields.');
  }

  return {
    accessToken,
    expiresIn,
    sessionId,
    user: parseUser(record.user ?? record.User),
  };
}
