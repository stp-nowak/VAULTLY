import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { Eyebrow } from './Eyebrow';

describe('Eyebrow', () => {
  it('renders an accent label and merges className', () => {
    render(<Eyebrow className="text-base">Vaultly</Eyebrow>);

    const label = screen.getByText('Vaultly');
    expect(label.className).toContain('text-accent');
    expect(label.className).toContain('text-base');
  });
});
