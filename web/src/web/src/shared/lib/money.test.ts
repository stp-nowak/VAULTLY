import { describe, expect, it } from 'vitest';

import { formatMoney, parseAmount } from './money';

describe('parseAmount', () => {
  it.each([
    ['12', 12],
    ['12.5', 12.5],
    ['12,50', 12.5],
    ['1 234,5678', 1234.5678],
    ['-40', -40],
  ])('reads %s', (text, amount) => {
    expect(parseAmount(text)).toBe(amount);
  });

  it.each(['', 'abc', '1.23456', '1,2,3', '12.'])('rejects %s', (text) => {
    expect(parseAmount(text)).toBeNull();
  });
});

describe('formatMoney', () => {
  it('formats the amount in its currency', () => {
    expect(formatMoney(-1234.5, 'EUR', 'en-GB')).toBe('-€1,234.50');
  });
});
