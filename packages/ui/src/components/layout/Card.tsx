import { cn } from '../../lib/cn';

const sizes = {
  default: 'gap-5 rounded-3xl p-6',
  hero: 'w-full max-w-152 gap-5 rounded-4xl p-6 md:p-10',
} as const;

export interface CardProps extends React.HTMLAttributes<HTMLElement> {
  /** `hero` is the single centered panel of full-screen pages (login, errors). */
  size?: keyof typeof sizes;
}

/** Raised glass panel used for every content block. Renders a `<section>`. */
export function Card({ className, size = 'default', ...props }: CardProps) {
  return (
    <section
      className={cn('grid border border-line bg-panel shadow-panel backdrop-blur-xl', sizes[size], className)}
      {...props}
    />
  );
}
