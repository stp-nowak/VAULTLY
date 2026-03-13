import { clsx } from 'clsx';

import styles from './Button.module.scss';

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'ghost' | 'primary' | 'secondary';
}

export function Button({ className, type = 'button', variant = 'primary', ...props }: ButtonProps) {
  return <button className={clsx(styles.button, styles[variant], className)} type={type} {...props} />;
}
