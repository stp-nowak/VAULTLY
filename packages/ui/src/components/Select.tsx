import { cn } from '../lib/cn';

export type SelectProps = React.SelectHTMLAttributes<HTMLSelectElement>;

/** Native select styled as a pill. Needs an accessible name (`aria-label` or a `<label>`). */
export function Select({ className, ...props }: SelectProps) {
  return (
    <select
      className={cn(
        'min-h-10 cursor-pointer rounded-full border border-line-strong bg-surface-raised px-4 text-ink',
        'focus:border-accent focus:outline-none disabled:cursor-wait disabled:opacity-55 [&>option]:bg-canvas',
        className,
      )}
      {...props}
    />
  );
}
