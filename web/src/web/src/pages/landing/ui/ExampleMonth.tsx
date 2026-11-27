import { cn } from '@vaultly/ui';
import { Trans, useTranslation } from 'react-i18next';

import { useIntlLocale } from '@/shared/i18n';

type BudgetCategory = 'eatingOut' | 'groceries' | 'subscriptions' | 'transport';

interface BudgetLine {
  category: BudgetCategory;
  limit: number;
  spent: number;
}

type BudgetTone = 'over' | 'within';

const budgetLines: BudgetLine[] = [
  { category: 'groceries', limit: 1500, spent: 1240 },
  { category: 'transport', limit: 400, spent: 310 },
  { category: 'subscriptions', limit: 150, spent: 129 },
  { category: 'eatingOut', limit: 400, spent: 460 },
];

const toneClasses: Record<BudgetTone, { amount: string; bar: string }> = {
  over: { amount: 'text-danger', bar: 'bg-danger' },
  within: { amount: 'text-ink', bar: 'bg-accent' },
};

function toneOf(line: BudgetLine): BudgetTone {
  return line.spent > line.limit ? 'over' : 'within';
}

const exampleCurrency = 'PLN';

const leftToSpend = budgetLines.reduce((left, line) => left + line.limit - line.spent, 0);

interface ExampleMonthProps {
  className?: string;
}

/** Illustration for the landing page: one month of budgets, with made-up numbers. */
export function ExampleMonth({ className }: ExampleMonthProps) {
  const { t } = useTranslation('pages/landing');
  const money = new Intl.NumberFormat(useIntlLocale(), {
    currency: exampleCurrency,
    maximumFractionDigits: 0,
    style: 'currency',
  });

  return (
    <figure
      className={cn(
        'grid gap-6 rounded-4xl border border-line bg-panel p-6 shadow-panel backdrop-blur-xl md:p-8',
        className,
      )}
    >
      <figcaption className="flex items-baseline justify-between gap-4 border-b border-dashed border-line-strong pb-5">
        <span className="font-display text-2xl font-semibold tracking-display">{t('exampleMonth.caption')}</span>
        <span className="text-xs font-bold tracking-label text-muted uppercase">{t('exampleMonth.label')}</span>
      </figcaption>

      <ul className="grid gap-5">
        {budgetLines.map((line) => {
          const tone = toneClasses[toneOf(line)];

          return (
            <li className="grid gap-2" key={line.category}>
              <div className="flex items-baseline justify-between gap-4">
                <span>{t(`exampleMonth.categories.${line.category}`)}</span>
                <span className="text-sm text-muted tabular-nums">
                  <Trans
                    components={{
                      limit: <>{money.format(line.limit)}</>,
                      spent: <strong className={tone.amount}>{money.format(line.spent)}</strong>,
                    }}
                    i18nKey="exampleMonth.spentOfLimit"
                    t={t}
                  />
                </span>
              </div>
              <div aria-hidden className="h-1.5 overflow-hidden rounded-full bg-surface-raised">
                <div
                  className={cn('h-full rounded-full', tone.bar)}
                  style={{ width: `${Math.min(line.spent / line.limit, 1) * 100}%` }}
                />
              </div>
            </li>
          );
        })}
      </ul>

      <p className="flex items-baseline justify-between gap-4 border-t border-dashed border-line-strong pt-5">
        <span className="text-muted">{t('exampleMonth.leftToSpend')}</span>
        <span className="font-display text-3xl font-semibold tracking-display text-accent tabular-nums">
          {money.format(leftToSpend)}
        </span>
      </p>
    </figure>
  );
}
