import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { Button } from './Button';

describe('Button', () => {
  it('defaults to a non-submitting primary button', () => {
    render(<Button>Save</Button>);

    const button = screen.getByRole('button', { name: 'Save' });
    expect(button).toHaveAttribute('type', 'button');
    expect(button.className).toContain('text-accent-ink');
  });

  it('merges consumer classes over variant defaults', () => {
    render(
      <Button className="px-8" variant="secondary">
        Cancel
      </Button>,
    );

    const button = screen.getByRole('button', { name: 'Cancel' });
    expect(button.className).toContain('px-8');
    expect(button.className).not.toContain('px-5');
    expect(button.className).toContain('border-line-strong');
  });
});
