import { describe, expect, it } from 'vitest';

import { chartOf, chartSize, seriesOf } from './balanceChart';

describe('balance series', () => {
  const opening = { balance: 100, date: '2026-11-01' };

  it('starts at the opening balance and runs on to today', () => {
    expect(seriesOf([{ balance: 60, date: '2026-11-10' }], opening, '2026-11-28')).toEqual([
      { balance: 100, date: '2026-11-01' },
      { balance: 60, date: '2026-11-10' },
      { balance: 60, date: '2026-11-28' },
    ]);
  });

  it('starts the opening balance on a transaction dated before the account was opened', () => {
    expect(seriesOf([{ balance: 160, date: '2026-10-20' }], opening, '2026-10-20')).toEqual([
      { balance: 100, date: '2026-10-20' },
      { balance: 160, date: '2026-10-20' },
    ]);
  });

  it('does not repeat today when the last change was today', () => {
    expect(seriesOf([{ balance: 60, date: '2026-11-28' }], opening, '2026-11-28')).toHaveLength(2);
  });

  it('shows the opening balance until today when nothing changed', () => {
    expect(seriesOf([], opening, '2026-11-28')).toEqual([opening, { balance: 100, date: '2026-11-28' }]);
  });
});

describe('balance chart', () => {
  it('spreads the days over time and steps between balances', () => {
    const chart = chartOf([
      { balance: 100, date: '2026-11-01' },
      { balance: 50, date: '2026-11-03' },
      { balance: 80, date: '2026-11-05' },
    ]);

    expect(chart.points.map((point) => point.x)).toEqual([0, chartSize.width / 2, chartSize.width]);
    expect(chart.points[0].y).toBeLessThan(chart.points[1].y);
    expect(chart.path.startsWith(`M0,${chart.points[0].y}H${chartSize.width / 2}V`)).toBe(true);
    expect(chart.zeroY).toBeNull();
  });

  it('puts round amounts on the axis around the balances, top first', () => {
    const chart = chartOf([
      { balance: 11_000, date: '2026-11-10' },
      { balance: 8_513.6, date: '2026-11-27' },
    ]);

    expect(chart.ticks.map((tick) => tick.value)).toEqual([12_000, 11_000, 10_000, 9_000, 8_000]);
    expect(chart.ticks[0].y).toBe(0);
    expect(chart.ticks.at(-1)?.y).toBe(chartSize.height);
  });

  it('marks zero when the balance crosses it', () => {
    const chart = chartOf([
      { balance: 20, date: '2026-11-01' },
      { balance: -30, date: '2026-11-02' },
    ]);

    expect(chart.ticks.some((tick) => tick.value === 0)).toBe(true);
    expect(chart.zeroY).toBeGreaterThan(chart.points[0].y);
    expect(chart.zeroY).toBeLessThan(chart.points[1].y);
  });

  it('keeps a flat balance in the middle of the axis', () => {
    const chart = chartOf([
      { balance: 60, date: '2026-11-01' },
      { balance: 60, date: '2026-11-02' },
    ]);

    expect(chart.points[0].y).toBe(chartSize.height / 2);
    expect(chart.ticks.length).toBeGreaterThan(1);
  });
});
