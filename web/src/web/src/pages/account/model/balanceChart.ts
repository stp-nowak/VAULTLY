import type { DailyBalance } from '@/domain/account';

/** The drawing's own units; the SVG stretches them to its box, so positions are also used as percentages. */
export const chartSize = { height: 100, width: 1000 } as const;

const dayMs = 86_400_000;
const minIntervals = 4;

export interface ChartPoint extends DailyBalance {
  x: number;
  y: number;
}

export interface AxisTick {
  value: number;
  y: number;
}

export interface BalanceChartModel {
  first: DailyBalance;
  last: DailyBalance;
  /** Stepped: a balance holds until the next day it changed. */
  path: string;
  points: ChartPoint[];
  ticks: AxisTick[];
  /** Where zero is, when it lies inside the axis. */
  zeroY: number | null;
}

/**
 * The opening balance, every day with a transaction and today, so the line starts at the opening balance and runs on
 * to the present. Transactions may be dated before the account was opened; the opening balance then starts on the
 * earliest of them, as the balance before it.
 */
export function seriesOf(history: DailyBalance[], opening: DailyBalance, today: string): DailyBalance[] {
  const start = history.length > 0 && history[0].date < opening.date ? history[0].date : opening.date;
  const days = [{ balance: opening.balance, date: start }, ...history];
  const last = days[days.length - 1];
  return last.date < today ? [...days, { balance: last.balance, date: today }] : days;
}

function niceStep(span: number): number {
  const raw = span / 4;
  const magnitude = 10 ** Math.floor(Math.log10(raw));
  const normalized = raw / magnitude;
  const nice = [1, 2, 2.5, 5, 10].find((candidate) => normalized <= candidate) ?? 10;
  return nice * magnitude;
}

/** Lays out a non-empty series on a time axis and a balance axis with round ticks around it. */
export function chartOf(series: DailyBalance[]): BalanceChartModel {
  const first = series[0];
  const last = series[series.length - 1];
  const balances = series.map((day) => day.balance);
  const lowest = Math.min(...balances);
  const highest = Math.max(...balances);
  const step = niceStep(highest - lowest || Math.max(Math.abs(highest) * 0.1, 1));
  let bottom = Math.floor(lowest / step) * step;
  let top = Math.ceil(highest / step) * step;
  for (let extend = 0; Math.round((top - bottom) / step) < minIntervals; extend += 1) {
    if (extend % 2 === 0) {
      top += step;
    } else {
      bottom -= step;
    }
  }
  const yOf = (balance: number) => ((top - balance) / (top - bottom)) * chartSize.height;

  const start = Date.parse(first.date);
  const span = Math.max(Date.parse(last.date) - start, dayMs);
  const xOf = (date: string) => (series.length === 1 ? chartSize.width / 2 : ((Date.parse(date) - start) / span) * chartSize.width);

  const points = series.map((day) => ({ ...day, x: xOf(day.date), y: yOf(day.balance) }));
  const stepped = points.map((point, index) => (index === 0 ? `M${point.x},${point.y}` : `H${point.x}V${point.y}`)).join('');
  const count = Math.round((top - bottom) / step);
  const ticks = Array.from({ length: count + 1 }, (_, index) => {
    const value = top - index * step;
    return { value, y: yOf(value) };
  });

  return {
    first,
    last,
    path: points.length === 1 ? `M0,${points[0].y}H${chartSize.width}` : stepped,
    points,
    ticks,
    zeroY: bottom < 0 && top > 0 ? yOf(0) : null,
  };
}
