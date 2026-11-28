import { describe, expect, it } from 'vitest';

import { formatDay, isIsoDay, today } from './day';

describe('days', () => {
  it('gives today in local time', () => {
    expect(today(new Date(2026, 0, 5, 23, 30))).toBe('2026-01-05');
  });

  it.each([
    ['2026-11-28', true],
    ['2026-13-01', false],
    ['28.11.2026', false],
    [20261128, false],
  ])('recognises %s as a day: %s', (value, expected) => {
    expect(isIsoDay(value)).toBe(expected);
  });

  it('formats a day as it is', () => {
    expect(formatDay('2026-11-28', 'en-GB')).toBe('28 Nov 2026');
  });
});
