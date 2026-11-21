import type { AuthSession, AuthUser, CurrentUser } from '../model/types';

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
    throw new Error('Response did not include a valid user payload.');
  }

  const record = value as Record<string, unknown>;
  const id = getString(record, 'id', 'Id');
  const email = getString(record, 'email', 'Email');

  if (!id || !email) {
    throw new Error('Response did not include a valid user payload.');
  }

  const name = getString(record, 'name', 'Name') ?? email;

  return { email, id, name };
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

export function parseCurrentUser(value: unknown): CurrentUser {
  if (!value || typeof value !== 'object') {
    throw new Error('Current user response was invalid.');
  }

  const record = value as Record<string, unknown>;
  const sessionId = getString(record, 'sessionId', 'SessionId');
  if (!sessionId) {
    throw new Error('Current user response was missing the session id.');
  }

  return { sessionId, user: parseUser(record.user ?? record.User) };
}

export function parseLogoutResponse(value: unknown): string {
  const logoutUrl = value && typeof value === 'object' ? getString(value as Record<string, unknown>, 'logoutUrl') : null;
  if (!logoutUrl || !/^https?:\/\//.test(logoutUrl)) {
    throw new Error('Logout response was invalid.');
  }

  return logoutUrl;
}
