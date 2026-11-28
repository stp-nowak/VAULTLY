import { cn } from '@vaultly/ui';

export type AccountsIconProps = Omit<React.SVGAttributes<SVGSVGElement>, 'children'>;

/** A wallet in the current text color, for accounts. Decorative. */
export function AccountsIcon({ className, ...props }: AccountsIconProps) {
  return (
    <svg aria-hidden className={cn('size-6 shrink-0', className)} fill="none" viewBox="0 0 24 24" {...props}>
      <path d="M4 7.5A2.5 2.5 0 0 1 6.5 5h10A1.5 1.5 0 0 1 18 6.5V8" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="1.75" />
      <path d="M4 7.5v10A2.5 2.5 0 0 0 6.5 20h12a1.5 1.5 0 0 0 1.5-1.5v-9A1.5 1.5 0 0 0 18.5 8h-12A2.5 2.5 0 0 1 4 5.5" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="1.75" />
      <circle cx="16" cy="14" fill="currentColor" r="1.25" />
    </svg>
  );
}
