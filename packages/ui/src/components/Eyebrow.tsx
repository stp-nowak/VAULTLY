import { cn } from '../lib/cn';

export type EyebrowProps = React.HTMLAttributes<HTMLParagraphElement>;

/** Small uppercase accent label placed above headings. */
export function Eyebrow({ className, ...props }: EyebrowProps) {
  return <p className={cn('text-xs font-bold tracking-eyebrow text-accent uppercase', className)} {...props} />;
}
