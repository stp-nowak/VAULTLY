import { cn } from '../lib/cn';

export type LogOutIconProps = Omit<React.SVGAttributes<SVGSVGElement>, 'children'>;

/** An arrow leaving a door in the current text color, for signing out. Decorative. */
export function LogOutIcon({ className, ...props }: LogOutIconProps) {
  return (
    <svg aria-hidden className={cn('size-6 shrink-0', className)} fill="none" viewBox="0 0 24 24" {...props}>
      <path d="M10 4.5H6.5A1.5 1.5 0 0 0 5 6v12a1.5 1.5 0 0 0 1.5 1.5H10" stroke="currentColor" strokeLinecap="round" strokeWidth="1.75" />
      <path d="M14 8l4 4-4 4M18 12H9.5" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="1.75" />
    </svg>
  );
}
