import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { BrandMark } from './BrandMark';
import { ChevronDownIcon } from './ChevronDownIcon';
import { CloseIcon } from './CloseIcon';
import { LogOutIcon } from './LogOutIcon';
import { MenuIcon } from './MenuIcon';
import { SearchIcon } from './SearchIcon';
import { SettingsIcon } from './SettingsIcon';

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
  ['MenuIcon', MenuIcon],
  ['CloseIcon', CloseIcon],
  ['SettingsIcon', SettingsIcon],
  ['LogOutIcon', LogOutIcon],
])('%s', (_, Icon) => {
  it('is decorative and merges className', () => {
    const { container } = render(<Icon className="size-7" />);

    const icon = container.querySelector('svg');
    expect(icon).toHaveAttribute('aria-hidden', 'true');
    expect(icon?.getAttribute('class')).toContain('size-7');
  });
});
