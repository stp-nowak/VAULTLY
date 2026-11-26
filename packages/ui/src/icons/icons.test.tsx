import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { BrandMark } from './BrandMark';
import { ChevronDownIcon } from './ChevronDownIcon';
import { SearchIcon } from './SearchIcon';

describe('BrandMark', () => {
  it('is decorative unless it gets a title', () => {
    const { container } = render(
      <>
        <BrandMark />
        <BrandMark title="Vaultly" />
      </>,
    );

    expect(screen.getAllByRole('img')).toHaveLength(1);
    expect(screen.getByRole('img', { name: 'Vaultly' })).toBeInTheDocument();
    expect(container.querySelector('svg[aria-hidden="true"]')).toBeInTheDocument();
  });
});

describe.each([
  ['SearchIcon', SearchIcon],
  ['ChevronDownIcon', ChevronDownIcon],
])('%s', (_, Icon) => {
  it('is decorative and merges className', () => {
    const { container } = render(<Icon className="size-6" />);

    const icon = container.querySelector('svg');
    expect(icon).toHaveAttribute('aria-hidden', 'true');
    expect(icon?.getAttribute('class')).toContain('size-6');
  });
});
