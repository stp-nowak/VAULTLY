import { cn } from '../../lib/cn';

export type CenteredScreenProps = React.HTMLAttributes<HTMLElement>;

/** Full-viewport `<main>` that centers its content (login, error and status pages). */
export function CenteredScreen({ className, ...props }: CenteredScreenProps) {
  return <main className={cn('grid min-h-screen place-items-center p-6 md:p-8', className)} {...props} />;
}
