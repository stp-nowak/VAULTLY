import { cn } from '@vaultly/ui';

export type TransactionsIconProps = Omit<React.SVGAttributes<SVGSVGElement>, 'children'>;

/** Two opposite arrows in the current text color, for money coming in and going out. Decorative. */
export function TransactionsIcon({ className, ...props }: TransactionsIconProps) {
  return (
    <svg aria-hidden className={cn('size-6 shrink-0', className)} fill="none" viewBox="0 0 24 24" {...props}>
      <path d="M4 8h14m0 0-3.5-3.5M18 8l-3.5 3.5M20 16H6m0 0 3.5-3.5M6 16l3.5 3.5" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="1.75" />
    </svg>
  );
}
