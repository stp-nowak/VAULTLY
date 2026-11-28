import { useId, useState } from 'react';

import { ChevronDownIcon } from '../../icons/ChevronDownIcon';
import { cn } from '../../lib/cn';

export interface SelectOption<TValue extends string = string> {
  disabled?: boolean;
  label: string;
  value: TValue;
}

export interface SelectProps<TValue extends string = string> {
  'aria-label'?: string;
  'aria-labelledby'?: string;
  className?: string;
  defaultValue?: TValue;
  disabled?: boolean;
  id?: string;
  /** Called with the picked option's value. */
  onValueChange?: (value: TValue) => void;
  options: readonly SelectOption<TValue>[];
  value?: TValue;
}

/**
 * A pill that opens a list of options in the app's own style (a native select's list cannot be styled). Follows the
 * select-only combobox pattern: focus stays on the button, arrows move through the options, Enter or Space picks one,
 * Escape and leaving close the list. Needs an accessible name (`aria-label` or `aria-labelledby`); `className`
 * styles the wrapper (e.g. its width).
 */
export function Select<TValue extends string = string>({
  'aria-label': ariaLabel,
  'aria-labelledby': ariaLabelledBy,
  className,
  defaultValue,
  disabled,
  id,
  onValueChange,
  options,
  value,
}: SelectProps<TValue>) {
  const listId = useId();
  const [ownValue, setOwnValue] = useState(defaultValue ?? options[0]?.value);
  const [isOpen, setIsOpen] = useState(false);
  const [active, setActive] = useState(-1);
  const current = value ?? ownValue;
  const selected = options.findIndex((option) => option.value === current);
  const enabled = options.flatMap((option, index) => (option.disabled ? [] : [index]));
  const optionId = (index: number) => `${listId}-${index}`;

  const open = () => {
    setActive(selected >= 0 ? selected : (enabled[0] ?? -1));
    setIsOpen(true);
  };

  const pick = (index: number) => {
    const option = options[index];
    if (!option || option.disabled) {
      return;
    }

    setIsOpen(false);
    if (option.value !== current) {
      setOwnValue(option.value);
      onValueChange?.(option.value);
    }
  };

  const move = (to: (position: number) => number) => {
    const position = Math.min(Math.max(to(enabled.indexOf(active)), 0), enabled.length - 1);
    setActive(enabled[position] ?? -1);
  };

  const keysWhenOpen: Record<string, () => void> = {
    ' ': () => pick(active),
    ArrowDown: () => move((position) => position + 1),
    ArrowUp: () => move((position) => position - 1),
    End: () => move(() => enabled.length - 1),
    Enter: () => pick(active),
    Escape: () => setIsOpen(false),
    Home: () => move(() => 0),
  };
  const keysWhenClosed: Record<string, () => void> = { ' ': open, ArrowDown: open, ArrowUp: open, Enter: open };

  const onKeyDown = (event: React.KeyboardEvent) => {
    const action = (isOpen ? keysWhenOpen : keysWhenClosed)[event.key];
    if (action) {
      event.preventDefault();
      action();
    }
  };

  return (
    <div className={cn('relative inline-grid', className)}>
      <button
        aria-activedescendant={isOpen && active >= 0 ? optionId(active) : undefined}
        aria-controls={listId}
        aria-expanded={isOpen}
        aria-haspopup="listbox"
        aria-label={ariaLabel}
        aria-labelledby={ariaLabelledBy}
        className={cn(
          'flex min-h-10 w-full cursor-pointer items-center justify-between gap-3 rounded-full border border-line-strong',
          'bg-surface-raised px-4 text-left text-ink focus:border-accent focus:outline-none',
          'disabled:cursor-wait disabled:opacity-55',
          isOpen && 'border-accent',
        )}
        disabled={disabled}
        id={id}
        onBlur={() => setIsOpen(false)}
        onClick={() => (isOpen ? setIsOpen(false) : open())}
        onKeyDown={onKeyDown}
        role="combobox"
        type="button"
      >
        <span className="truncate">{options[selected]?.label}</span>
        <ChevronDownIcon className={cn('text-muted transition-transform', isOpen && 'rotate-180')} />
      </button>
      {isOpen && (
        <ul
          aria-label={ariaLabel}
          aria-labelledby={ariaLabelledBy}
          className={cn(
            'absolute top-full left-0 z-30 mt-1.5 grid max-h-72 w-full min-w-max gap-0.5 overflow-auto rounded-2xl',
            'border border-line-strong bg-panel p-1.5 shadow-panel backdrop-blur-xl',
          )}
          id={listId}
          // Keeps focus on the button, so picking an option does not close the list before the click lands.
          onMouseDown={(event) => event.preventDefault()}
          role="listbox"
        >
          {options.map((option, index) => (
            <li
              aria-disabled={option.disabled || undefined}
              aria-selected={index === selected}
              className={cn(
                'cursor-pointer rounded-xl px-3 py-2 text-ink',
                index === active && 'bg-surface-raised',
                index === selected && 'text-accent',
                option.disabled && 'cursor-not-allowed opacity-45',
              )}
              id={optionId(index)}
              key={option.value}
              onClick={() => pick(index)}
              onPointerMove={() => !option.disabled && setActive(index)}
              role="option"
            >
              {option.label}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
