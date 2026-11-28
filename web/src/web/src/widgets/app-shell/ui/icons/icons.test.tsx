import { render } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { AccountsIcon } from './AccountsIcon';
import { BudgetsIcon } from './BudgetsIcon';
import { OverviewIcon } from './OverviewIcon';
import { TransactionsIcon } from './TransactionsIcon';

describe.each([
  ['OverviewIcon', OverviewIcon],
  ['AccountsIcon', AccountsIcon],
  ['TransactionsIcon', TransactionsIcon],
  ['BudgetsIcon', BudgetsIcon],
])('%s', (_, Icon) => {
  it('is decorative and merges className', () => {
    const { container } = render(<Icon className="size-7" />);

    const icon = container.querySelector('svg');
    expect(icon).toHaveAttribute('aria-hidden', 'true');
    expect(icon?.getAttribute('class')).toContain('size-7');
  });
});
