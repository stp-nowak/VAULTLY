import { cn } from '../lib/cn';

export type SearchIconProps = Omit<React.SVGAttributes<SVGSVGElement>, 'children'>;

/** Magnifying glass in the current text color. Decorative: name the control it sits in (e.g. `aria-label`). */
export function SearchIcon({ className, ...props }: SearchIconProps) {
  return (
    <svg aria-hidden className={cn('size-5 shrink-0', className)} fill="none" viewBox="0 0 20 20" {...props}>
      <circle cx="9" cy="9" r="5.75" stroke="currentColor" strokeWidth="1.75" />
      <path d="m13.25 13.25 3.5 3.5" stroke="currentColor" strokeLinecap="round" strokeWidth="1.75" />
    </svg>
  );
}
