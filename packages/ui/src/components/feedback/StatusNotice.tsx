import { cn } from '../../lib/cn';

const tones = {
  error: 'border-danger-line bg-danger-soft text-danger-ink',
  info: 'border-line bg-accent-soft text-ink',
  success: 'border-success-line bg-success-soft text-success-ink',
} as const;

export type StatusTone = keyof typeof tones;

export interface StatusNoticeProps {
  children: React.ReactNode;
  className?: string;
  tone?: StatusTone;
}

/** Inline status message. Errors are announced to assistive technology (`role="alert"`). */
export function StatusNotice({ children, className, tone = 'info' }: StatusNoticeProps) {
  return (
    <div
      className={cn('rounded-2xl border px-4 py-3.5', tones[tone], className)}
      role={tone === 'error' ? 'alert' : 'status'}
    >
      {children}
    </div>
  );
}
