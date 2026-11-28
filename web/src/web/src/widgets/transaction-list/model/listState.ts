import type { QueryStatus } from '@tanstack/react-query';

export type ListState = 'empty' | 'error' | 'list' | 'loading';

const byStatus: Record<QueryStatus, (count: number) => ListState> = {
  error: () => 'error',
  pending: () => 'loading',
  success: (count) => (count > 0 ? 'list' : 'empty'),
};

export function listStateOf(status: QueryStatus, count: number): ListState {
  return byStatus[status](count);
}
