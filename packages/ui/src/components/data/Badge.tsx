import { cn } from '../../lib/cn';

const tones = {
  accent: 'border-line-strong bg-accent-soft text-accent',
  neutral: 'border-line text-muted',
} as const;

export type BadgeTone = keyof typeof tones;

export interface BadgeProps extends React.HTMLAttributes<HTMLSpanElement> {
  tone?: BadgeTone;
}

/** Small uppercase label for a status or category, e.g. a role or a sign-in method. */
export function Badge({ className, tone = 'neutral', ...props }: BadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-bold tracking-label uppercase',
        tones[tone],
        className,
      )}
      {...props}
    />
  );
}
