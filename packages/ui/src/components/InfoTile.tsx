import { cn } from '../lib/cn';

export interface InfoTileProps {
  className?: string;
  label: React.ReactNode;
  value: React.ReactNode;
}

/**
 * Label/value pair rendered as a bordered tile. Uses `<dt>`/`<dd>` and must be placed inside a `<dl>`.
 */
export function InfoTile({ className, label, value }: InfoTileProps) {
  return (
    <div className={cn('rounded-2xl border border-line bg-surface px-4 py-3.5', className)}>
      <dt className="text-xs tracking-label text-muted uppercase">{label}</dt>
      <dd className="mt-1.5 break-words">{value}</dd>
    </div>
  );
}
