import { cn } from '@vaultly/ui';

export type BudgetsIconProps = Omit<React.SVGAttributes<SVGSVGElement>, 'children'>;

/** A pie chart in the current text color, for budgets. Decorative. */
export function BudgetsIcon({ className, ...props }: BudgetsIconProps) {
  return (
    <svg aria-hidden className={cn('size-6 shrink-0', className)} fill="none" viewBox="0 0 24 24" {...props}>
      <path d="M12 4a8 8 0 1 0 8 8h-8V4Z" stroke="currentColor" strokeLinejoin="round" strokeWidth="1.75" />
      <path d="M15 3.6A8 8 0 0 1 20.4 9H15V3.6Z" stroke="currentColor" strokeLinejoin="round" strokeWidth="1.75" />
    </svg>
  );
}
