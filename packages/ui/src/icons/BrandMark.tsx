import { cn } from '../lib/cn';

export interface BrandMarkProps extends Omit<React.SVGAttributes<SVGSVGElement>, 'children'> {
  /** Accessible name. Omit when the mark sits next to the written app name, so it stays decorative. */
  title?: string;
}

/** The Vaultly logo mark. */
export function BrandMark({ className, title, ...props }: BrandMarkProps) {
  return (
    <svg
      aria-hidden={title ? undefined : true}
      aria-label={title}
      className={cn('size-9 shrink-0', className)}
      fill="none"
      role={title ? 'img' : undefined}
      viewBox="0 0 64 64"
      {...props}
    >
      <rect className="fill-canvas stroke-line-strong" height="62" rx="17" strokeWidth="2" width="62" x="1" y="1" />
      <path
        className="fill-accent stroke-accent-strong"
        d="M19 22.5L32 15l13 7.5v15L32 45l-13-7.5v-15Z"
        strokeWidth="2.5"
      />
      <path className="stroke-accent-ink" d="M32 15v30" strokeLinecap="round" strokeWidth="2.5" />
      <path className="stroke-accent-ink" d="M19 22.5 32 30l13-7.5" strokeLinecap="round" strokeWidth="2.5" />
    </svg>
  );
}
