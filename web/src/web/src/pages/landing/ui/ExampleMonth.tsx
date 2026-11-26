import { cn } from '@vaultly/ui';

interface BudgetLine {
  category: string;
  limit: number;
  spent: number;
}

type BudgetTone = 'over' | 'within';

const budgetLines: BudgetLine[] = [
  { category: 'Groceries', limit: 1500, spent: 1240 },
  { category: 'Transport', limit: 400, spent: 310 },
  { category: 'Subscriptions', limit: 150, spent: 129 },
  { category: 'Eating out', limit: 400, spent: 460 },
];

const toneClasses: Record<BudgetTone, { amount: string; bar: string }> = {
  over: { amount: 'text-danger', bar: 'bg-danger' },
  within: { amount: 'text-ink', bar: 'bg-accent' },
};

const zloty = new Intl.NumberFormat('pl-PL', { currency: 'PLN', maximumFractionDigits: 0, style: 'currency' });

const leftToSpend = budgetLines.reduce((left, line) => left + line.limit - line.spent, 0);

interface ExampleMonthProps {
  className?: string;
}

/** Illustration for the landing page: one month of budgets, with made-up numbers. */
export function ExampleMonth({ className }: ExampleMonthProps) {
  return (
    <figure
      className={cn(
        'grid gap-6 rounded-4xl border border-line bg-panel p-6 shadow-panel backdrop-blur-xl md:p-8',
        className,
      )}
    >
      <figcaption className="flex items-baseline justify-between gap-4 border-b border-dashed border-line-strong pb-5">
        <span className="font-display text-2xl font-semibold tracking-display">An example month</span>
        <span className="text-xs font-bold tracking-label text-muted uppercase">Budgets</span>
      </figcaption>

      <ul className="grid gap-5">
        {budgetLines.map((line) => {
          const tone = toneClasses[line.spent > line.limit ? 'over' : 'within'];

          return (
            <li className="grid gap-2" key={line.category}>
              <div className="flex items-baseline justify-between gap-4">
                <span>{line.category}</span>
                <span className="text-sm text-muted tabular-nums">
                  <strong className={tone.amount}>{zloty.format(line.spent)}</strong> of {zloty.format(line.limit)}
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
        <span className="text-muted">Left to spend</span>
        <span className="font-display text-3xl font-semibold tracking-display text-accent tabular-nums">
          {zloty.format(leftToSpend)}
        </span>
      </p>
    </figure>
  );
}
