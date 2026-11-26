import type { UserPage, UserRole, UserSummary } from '../model/types';

const roles: readonly UserRole[] = ['admin', 'user'];
const invalidPage = 'User list response was invalid.';

function asRecord(value: unknown): Record<string, unknown> {
  if (!value || typeof value !== 'object' || Array.isArray(value)) {
    throw new Error(invalidPage);
  }

  return value as Record<string, unknown>;
}

function readString(record: Record<string, unknown>, key: string): string {
  const value = record[key];
  if (typeof value !== 'string') {
    throw new Error(invalidPage);
  }

  return value;
}

function readNumber(record: Record<string, unknown>, key: string): number {
  const value = record[key];
  if (typeof value !== 'number' || !Number.isInteger(value) || value < 0) {
    throw new Error(invalidPage);
  }

  return value;
}

export function isUserRole(value: unknown): value is UserRole {
  return typeof value === 'string' && (roles as readonly string[]).includes(value);
}

function parseUser(value: unknown): UserSummary {
  const record = asRecord(value);
  const { role, signInMethods } = record;

  if (!isUserRole(role) || !Array.isArray(signInMethods) || !signInMethods.every((method) => typeof method === 'string')) {
    throw new Error(invalidPage);
  }

  return {
    createdAt: readString(record, 'createdAt'),
    email: readString(record, 'email'),
    emailVerified: record.emailVerified === true,
    id: readString(record, 'id'),
    name: readString(record, 'name'),
    role,
    signInMethods,
  };
}

export function parseUserPage(value: unknown): UserPage {
  const record = asRecord(value);
  if (!Array.isArray(record.items)) {
    throw new Error(invalidPage);
  }

  return {
    items: record.items.map(parseUser),
    page: readNumber(record, 'page'),
    pageSize: readNumber(record, 'pageSize'),
    total: readNumber(record, 'total'),
  };
}
