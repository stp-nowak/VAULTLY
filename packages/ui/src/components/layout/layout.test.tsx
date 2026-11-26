import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { Card } from './Card';
import { CenteredScreen } from './CenteredScreen';

describe('Card', () => {
  it('renders a labelled section', () => {
    render(<Card aria-label="Profile">Content</Card>);

    expect(screen.getByRole('region', { name: 'Profile' })).toHaveTextContent('Content');
  });
});

describe('CenteredScreen', () => {
  it('is the main landmark and merges className', () => {
    render(<CenteredScreen className="p-2">Content</CenteredScreen>);

    expect(screen.getByRole('main').className).toContain('p-2');
  });
});
