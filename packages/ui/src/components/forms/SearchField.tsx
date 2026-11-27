import { useId, useRef } from 'react';

import { SearchIcon } from '../../icons/SearchIcon';
import { cn } from '../../lib/cn';

export interface SearchFieldProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'className' | 'type'> {
  className?: string;
  label: string;
  /** Called with the trimmed term when the user presses Enter or the search button. */
  onSearch: (term: string) => void;
  /** Accessible name of the icon button. */
  submitLabel: string;
}

/**
 * Labelled search box with the search button inside it, on the right. Not a form: Enter and the button call
 * `onSearch`, so it also works inside another form. `className` styles the wrapper; other props go to the `<input>`.
 */
export function SearchField({ className, id, label, onKeyDown, onSearch, submitLabel, ...props }: SearchFieldProps) {
  const generatedId = useId();
  const inputId = id ?? generatedId;
  const inputRef = useRef<HTMLInputElement>(null);
  const search = () => onSearch(inputRef.current?.value.trim() ?? '');

  return (
    <div className={cn('grid gap-1.5', className)} role="search">
      <label className="text-xs font-bold tracking-label text-muted uppercase" htmlFor={inputId}>
        {label}
      </label>
      <div className="relative">
        <input
          className="min-h-11 w-full rounded-2xl border border-line-strong bg-surface-raised pr-12 pl-4 text-ink placeholder:text-muted focus:border-accent focus:outline-none"
          id={inputId}
          ref={inputRef}
          type="search"
          {...props}
          onKeyDown={(event) => {
            onKeyDown?.(event);
            if (event.key === 'Enter' && !event.defaultPrevented) {
              event.preventDefault();
              search();
            }
          }}
        />
        <button
          aria-label={submitLabel}
          className="absolute top-1/2 right-1.5 grid size-8 -translate-y-1/2 cursor-pointer place-items-center rounded-xl text-muted transition-colors hover:bg-surface-raised hover:text-ink focus-visible:text-ink focus-visible:outline focus-visible:outline-accent"
          onClick={search}
          type="button"
        >
          <SearchIcon />
        </button>
      </div>
    </div>
  );
}
