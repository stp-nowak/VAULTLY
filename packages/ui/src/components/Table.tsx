import { cn } from '../lib/cn';

export type TableProps = React.TableHTMLAttributes<HTMLTableElement>;

/**
 * Data table: compose it with plain `<thead>`, `<tbody>`, `<tr>`, `<th>` and `<td>`; the styles apply to them. Scrolls
 * horizontally on narrow screens. Give it a `<caption>` or `aria-label`.
 */
export function Table({ className, ...props }: TableProps) {
  return (
    <div className="overflow-x-auto">
      <table
        className={cn(
          'w-full border-collapse text-left',
          '[&_th]:border-b [&_th]:border-line-strong [&_th]:px-4 [&_th]:py-3 [&_th]:text-xs [&_th]:font-bold',
          '[&_th]:tracking-label [&_th]:text-muted [&_th]:uppercase',
          '[&_td]:border-b [&_td]:border-line [&_td]:px-4 [&_td]:py-3.5 [&_td]:align-middle',
          className,
        )}
        {...props}
      />
    </div>
  );
}
