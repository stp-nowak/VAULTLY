import { cn } from '@vaultly/ui';

export type OverviewIconProps = Omit<React.SVGAttributes<SVGSVGElement>, 'children'>;

/** Four tiles in the current text color, for an overview or dashboard. Decorative. */
export function OverviewIcon({ className, ...props }: OverviewIconProps) {
  return (
    <svg aria-hidden className={cn('size-6 shrink-0', className)} fill="none" viewBox="0 0 24 24" {...props}>
      <rect height="7" rx="1.5" stroke="currentColor" strokeWidth="1.75" width="7" x="4" y="4" />
      <rect height="7" rx="1.5" stroke="currentColor" strokeWidth="1.75" width="7" x="13" y="4" />
      <rect height="7" rx="1.5" stroke="currentColor" strokeWidth="1.75" width="7" x="4" y="13" />
      <rect height="7" rx="1.5" stroke="currentColor" strokeWidth="1.75" width="7" x="13" y="13" />
    </svg>
  );
}
