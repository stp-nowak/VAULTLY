import { useId } from 'react';

import { cn } from '../../lib/cn';

export interface TextFieldProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label: string;
}

/** Labelled text input. `className` styles the wrapper; every other prop goes to the `<input>`. */
export function TextField({ className, id, label, ...props }: TextFieldProps) {
  const generatedId = useId();
  const inputId = id ?? generatedId;

  return (
    <div className={cn('grid gap-1.5', className)}>
      <label className="text-xs font-bold tracking-label text-muted uppercase" htmlFor={inputId}>
        {label}
      </label>
      <input
        className="min-h-11 rounded-2xl border border-line-strong bg-surface-raised px-4 text-ink placeholder:text-muted focus:border-accent focus:outline-none"
        id={inputId}
        {...props}
      />
    </div>
  );
}
