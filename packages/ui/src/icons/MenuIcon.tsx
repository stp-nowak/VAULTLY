import { cn } from '../lib/cn';

export type MenuIconProps = Omit<React.SVGAttributes<SVGSVGElement>, 'children'>;

/** Three horizontal lines ("hamburger") in the current text color, for a button that opens a menu. Decorative. */
export function MenuIcon({ className, ...props }: MenuIconProps) {
  return (
    <svg aria-hidden className={cn('size-5 shrink-0', className)} fill="none" viewBox="0 0 20 20" {...props}>
      <path d="M3.5 5.5h13M3.5 10h13M3.5 14.5h13" stroke="currentColor" strokeLinecap="round" strokeWidth="1.75" />
    </svg>
  );
}
