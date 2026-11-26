import { cn } from '../lib/cn';

export type CloseIconProps = Omit<React.SVGAttributes<SVGSVGElement>, 'children'>;

/** A cross in the current text color, for a button that closes a menu or dialog. Decorative. */
export function CloseIcon({ className, ...props }: CloseIconProps) {
  return (
    <svg aria-hidden className={cn('size-5 shrink-0', className)} fill="none" viewBox="0 0 20 20" {...props}>
      <path d="m5 5 10 10M15 5 5 15" stroke="currentColor" strokeLinecap="round" strokeWidth="1.75" />
    </svg>
  );
}
