import { describe, expect, it } from 'vitest';

import { resolveMethodStatus } from './methodStatus';

describe('resolveMethodStatus', () => {
  it.each([
    [true, true, 'shown'],
    [true, false, 'hidden'],
    [false, true, 'notConfigured'],
    [false, false, 'notConfigured'],
  ] as const)('configured %s, enabled %s → %s', (isConfigured, isEnabled, expected) => {
    expect(resolveMethodStatus({ id: 'google', isConfigured, isEnabled, name: 'Google' })).toBe(expected);
  });
});
