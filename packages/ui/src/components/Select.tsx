import { cn } from '../lib/cn';

export type SelectProps = React.SelectHTMLAttributes<HTMLSelectElement>;

/**
 * Native select styled as a pill, with its own chevron inset from the rounded edge. Needs an accessible name
 * (`aria-label` or a `<label>`). `className` styles the wrapper (e.g. its width); every other prop goes to the
 * `<select>`.
 */
export function Select({ className, ...props }: SelectProps) {
  return (
    <div className={cn('relative inline-grid', className)}>
      <select
        className={cn(
          'min-h-10 w-full cursor-pointer appearance-none rounded-full border border-line-strong bg-surface-raised',
          'pr-11 pl-4 text-ink focus:border-accent focus:outline-none disabled:cursor-wait disabled:opacity-55',
          '[&>option]:bg-canvas',
        )}
        {...props}
      />
      <svg
        aria-hidden
        className="pointer-events-none absolute top-1/2 right-4 size-4 -translate-y-1/2 text-muted"
        fill="none"
        viewBox="0 0 16 16"
      >
        <path d="m4 6 4 4 4-4" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="1.5" />
      </svg>
    </div>
  );
}
