import { describe, expect, it } from 'vitest';

import { cn } from './cn';

describe('cn', () => {
  it('lets later Tailwind classes override earlier ones', () => {
    expect(cn('p-6 text-ink', false, 'p-4')).toBe('text-ink p-4');
  });
});
