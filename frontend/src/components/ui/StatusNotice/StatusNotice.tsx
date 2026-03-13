import { clsx } from 'clsx';

import styles from './StatusNotice.module.scss';

interface StatusNoticeProps {
  children: React.ReactNode;
  tone?: 'error' | 'info' | 'success';
}

export function StatusNotice({ children, tone = 'info' }: StatusNoticeProps) {
  return <div className={clsx(styles.statusNotice, styles[tone])}>{children}</div>;
}
