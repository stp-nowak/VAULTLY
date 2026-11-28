import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import type { DailyBalance } from '@/domain/account';
import { useIntlLocale } from '@/shared/i18n';
import { formatDay, formatMoney } from '@/shared/lib';

import { chartOf, chartSize } from '../model/balanceChart';

interface BalanceChartProps {
  currency: string;
  /** From the opening balance to today; at least one day. */
  series: DailyBalance[];
}

const percentX = (x: number) => `${(x / chartSize.width) * 100}%`;
const percentY = (y: number) => `${(y / chartSize.height) * 100}%`;

/** The balance over time as a stepped line, amounts on the left axis and a readout of the day under the pointer. */
export function BalanceChart({ currency, series }: BalanceChartProps) {
  const { t } = useTranslation('pages/account');
  const locale = useIntlLocale();
  const [hovered, setHovered] = useState<number | null>(null);
  const chart = chartOf(series);
  const point = hovered === null ? null : chart.points[hovered];
  const axis = new Intl.NumberFormat(locale, { currency, maximumFractionDigits: 2, notation: 'compact', style: 'currency' });
  const labels = chart.ticks.map((tick) => axis.format(tick.value));
  const widest = labels.reduce((longest, label) => (label.length > longest.length ? label : longest), '');

  const hover = (event: React.PointerEvent<HTMLDivElement>) => {
    const box = event.currentTarget.getBoundingClientRect();
    const x = ((event.clientX - box.left) / box.width) * chartSize.width;
    const nearest = chart.points.reduce(
      (best, candidate, index) => (Math.abs(candidate.x - x) < Math.abs(chart.points[best].x - x) ? index : best),
      0,
    );
    setHovered(nearest);
  };

  return (
    <figure className="grid grid-cols-[auto_minmax(0,1fr)] gap-x-3 gap-y-2">
      <div aria-hidden className="relative text-xs text-muted tabular-nums">
        <span className="invisible block whitespace-nowrap">{widest}</span>
        {chart.ticks.map((tick, index) => (
          <span className="absolute right-0 -translate-y-1/2 whitespace-nowrap" key={tick.value} style={{ top: percentY(tick.y) }}>
            {labels[index]}
          </span>
        ))}
      </div>

      <div
        aria-label={t('history.chartLabel', { from: formatDay(chart.first.date, locale), to: formatDay(chart.last.date, locale) })}
        className="relative h-56 touch-none md:h-64"
        onPointerLeave={() => setHovered(null)}
        onPointerMove={hover}
        role="img"
      >
        <svg
          className="absolute inset-0 size-full overflow-visible"
          preserveAspectRatio="none"
          viewBox={`0 0 ${chartSize.width} ${chartSize.height}`}
        >
          {chart.ticks.map((tick) => (
            <line className="stroke-line" key={tick.value} vectorEffect="non-scaling-stroke" x1={0} x2={chartSize.width} y1={tick.y} y2={tick.y} />
          ))}
          {chart.zeroY !== null && (
            <line
              className="stroke-line-strong"
              strokeDasharray="4 4"
              vectorEffect="non-scaling-stroke"
              x1={0}
              x2={chartSize.width}
              y1={chart.zeroY}
              y2={chart.zeroY}
            />
          )}
          <path className="fill-none stroke-accent" d={chart.path} strokeLinejoin="round" strokeWidth={2} vectorEffect="non-scaling-stroke" />
          {point && (
            <line className="stroke-line-strong" vectorEffect="non-scaling-stroke" x1={point.x} x2={point.x} y1={0} y2={chartSize.height} />
          )}
        </svg>
        {point && (
          <>
            <span
              className="pointer-events-none absolute size-2.5 -translate-1/2 rounded-full bg-accent ring-2 ring-canvas"
              style={{ left: percentX(point.x), top: percentY(point.y) }}
            />
            <div
              className="pointer-events-none absolute -top-2 -translate-x-1/2 -translate-y-full rounded-xl border border-line-strong bg-canvas px-3 py-2 text-sm whitespace-nowrap shadow-panel"
              style={{ left: `clamp(4.5rem, ${percentX(point.x)}, calc(100% - 4.5rem))` }}
            >
              <p className="text-muted">{formatDay(point.date, locale)}</p>
              <p className="font-bold tabular-nums">{formatMoney(point.balance, currency, locale)}</p>
            </div>
          </>
        )}
      </div>

      <figcaption className="col-start-2 flex justify-between gap-4 text-xs text-muted">
        <span>{formatDay(chart.first.date, locale)}</span>
        <span>{formatDay(chart.last.date, locale)}</span>
      </figcaption>
    </figure>
  );
}
