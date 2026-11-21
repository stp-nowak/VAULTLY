import { cn } from '../lib/cn';

const variants = {
  ghost: 'border-line bg-transparent text-muted',
  primary: 'bg-linear-135 from-accent to-accent-strong text-accent-ink',
  secondary: 'border-line-strong bg-surface-raised text-ink',
} as const;

export type ButtonVariant = keyof typeof variants;

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
}

/** Pill-shaped action button. Defaults to `type="button"` so it never submits a form by accident. */
export function Button({ className, type = 'button', variant = 'primary', ...props }: ButtonProps) {
  return (
    <button
      className={cn(
        'inline-flex min-h-12.5 cursor-pointer items-center justify-center gap-2 rounded-full border border-transparent px-5',
        'transition-[translate,background-color,border-color] duration-120 ease-out',
        'enabled:hover:-translate-y-px disabled:cursor-wait disabled:opacity-55',
        variants[variant],
        className,
      )}
      type={type}
      {...props}
    />
  );
}
