import { useEffect, useId, useRef } from 'react';
import { createPortal } from 'react-dom';

import { CloseIcon } from '../../icons/CloseIcon';
import { cn } from '../../lib/cn';

export interface DialogProps {
  children: React.ReactNode;
  className?: string;
  /** Accessible name of the close button. */
  closeLabel: string;
  onClose: () => void;
  open: boolean;
  title: React.ReactNode;
}

const focusable = 'a[href], button:not([disabled]), input:not([disabled]), select, textarea, [tabindex]:not([tabindex="-1"])';

/**
 * Modal panel over a dimmed page, rendered into `document.body`. Focus moves into it and stays there until it closes
 * (Escape, the close button or a click outside), then returns to where it was. `className` styles the panel.
 */
export function Dialog({ children, className, closeLabel, onClose, open, title }: DialogProps) {
  const titleId = useId();
  const panel = useRef<HTMLDivElement>(null);
  const close = useRef(onClose);

  useEffect(() => {
    close.current = onClose;
  });

  useEffect(() => {
    if (!open) {
      return;
    }

    const returnTo = document.activeElement as HTMLElement | null;
    const overflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    const first = [...(panel.current?.querySelectorAll<HTMLElement>(focusable) ?? [])].find(
      (element) => !element.hasAttribute('data-dialog-close'),
    );
    (first ?? panel.current)?.focus();

    return () => {
      document.body.style.overflow = overflow;
      returnTo?.focus();
    };
  }, [open]);

  const onKeyDown = (event: React.KeyboardEvent<HTMLDivElement>) => {
    if (event.key === 'Escape') {
      event.stopPropagation();
      close.current();
      return;
    }

    if (event.key !== 'Tab') {
      return;
    }

    const elements = [...event.currentTarget.querySelectorAll<HTMLElement>(focusable)];
    const edge = event.shiftKey ? elements[0] : elements.at(-1);
    if (edge && document.activeElement === edge) {
      event.preventDefault();
      (event.shiftKey ? elements.at(-1) : elements[0])?.focus();
    }
  };

  return (
    open &&
    createPortal(
      <div
        className="fixed inset-0 z-40 grid place-items-center overflow-y-auto bg-canvas/75 p-4 backdrop-blur-sm"
        onMouseDown={(event) => event.target === event.currentTarget && close.current()}
      >
        <div
          aria-labelledby={titleId}
          aria-modal="true"
          className={cn(
            'grid w-full max-w-xl gap-5 rounded-3xl border border-line-strong bg-panel p-6 shadow-panel md:p-8',
            className,
          )}
          onKeyDown={onKeyDown}
          ref={panel}
          role="dialog"
          tabIndex={-1}
        >
          <div className="flex items-start justify-between gap-4">
            <h2 className="text-2xl" id={titleId}>
              {title}
            </h2>
            <button
              aria-label={closeLabel}
              className="grid size-9 shrink-0 cursor-pointer place-items-center rounded-xl text-muted transition-colors hover:bg-surface-raised hover:text-ink focus-visible:outline focus-visible:outline-accent"
              data-dialog-close
              onClick={() => close.current()}
              type="button"
            >
              <CloseIcon />
            </button>
          </div>
          {children}
        </div>
      </div>,
      document.body,
    )
  );
}
