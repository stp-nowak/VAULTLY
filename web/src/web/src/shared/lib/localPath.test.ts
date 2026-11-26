import { describe, expect, it } from 'vitest';

import { readLocalPath } from './localPath';

describe('readLocalPath', () => {
  it.each(['/app', '/app/budgets?month=2026-11#top', '/'])('keeps the local path %s', (path) => {
    expect(readLocalPath(path)).toBe(path);
  });

  it.each([
    ['another origin', 'https://evil.example/app'],
    ['a protocol-relative URL', '//evil.example/app'],
    ['a backslash host', '/\\evil.example'],
    ['a relative path', 'app'],
    ['an empty string', ''],
    ['a non-string', 42],
    ['nothing', undefined],
  ])('rejects %s', (_, value) => {
    expect(readLocalPath(value)).toBeUndefined();
  });
});
