import { cn } from '../lib/cn';

export type ChevronDownIconProps = Omit<React.SVGAttributes<SVGSVGElement>, 'children'>;

/** Downward chevron in the current text color, e.g. for a select. Decorative. */
export function ChevronDownIcon({ className, ...props }: ChevronDownIconProps) {
  return (
    <svg aria-hidden className={cn('size-4 shrink-0', className)} fill="none" viewBox="0 0 16 16" {...props}>
      <path d="m4 6 4 4 4-4" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="1.5" />
    </svg>
  );
}
