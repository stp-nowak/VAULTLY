import { cn } from '../../lib/cn';

export interface SwitchProps
  extends Omit<React.ButtonHTMLAttributes<HTMLButtonElement>, 'children' | 'onChange' | 'role' | 'type'> {
  checked: boolean;
  /** Called with the new state when the switch is toggled. */
  onCheckedChange?: (checked: boolean) => void;
}

/** On/off toggle (`role="switch"`). Needs an accessible name (`aria-label` or `aria-labelledby`). */
export function Switch({ checked, className, onCheckedChange, ...props }: SwitchProps) {
  return (
    <button
      aria-checked={checked}
      className={cn(
        'relative inline-flex h-7 w-12 shrink-0 cursor-pointer items-center rounded-full border transition-colors',
        'focus-visible:outline focus-visible:outline-offset-2 focus-visible:outline-accent',
        'disabled:cursor-not-allowed disabled:opacity-45',
        checked ? 'border-transparent bg-accent' : 'border-line-strong bg-surface-raised',
        className,
      )}
      onClick={() => onCheckedChange?.(!checked)}
      role="switch"
      type="button"
      {...props}
    >
      <span
        aria-hidden
        className={cn('size-5 rounded-full transition-transform', checked ? 'translate-x-6 bg-accent-ink' : 'translate-x-1 bg-muted')}
      />
    </button>
  );
}
