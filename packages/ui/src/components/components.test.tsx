import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { cn } from '../lib/cn';
import { BrandMark } from './BrandMark';
import { Button } from './Button';
import { Card } from './Card';
import { InfoTile } from './InfoTile';
import { StatusNotice } from './StatusNotice';

describe('cn', () => {
  it('lets later Tailwind classes override earlier ones', () => {
    expect(cn('p-6 text-ink', false, 'p-4')).toBe('text-ink p-4');
  });
});

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

describe('StatusNotice', () => {
  it('announces errors as alerts and other tones as status', () => {
    render(
      <>
        <StatusNotice tone="error">Failed</StatusNotice>
        <StatusNotice>Loading</StatusNotice>
      </>,
    );

    expect(screen.getByRole('alert')).toHaveTextContent('Failed');
    expect(screen.getByRole('status')).toHaveTextContent('Loading');
  });
});

describe('Card and InfoTile', () => {
  it('render a labelled section with label/value pairs', () => {
    render(
      <Card aria-label="Profile">
        <dl>
          <InfoTile label="Email" value="alex@example.com" />
        </dl>
      </Card>,
    );

    expect(screen.getByRole('region', { name: 'Profile' })).toBeInTheDocument();
    expect(screen.getByRole('term')).toHaveTextContent('Email');
    expect(screen.getByRole('definition')).toHaveTextContent('alex@example.com');
  });
});
