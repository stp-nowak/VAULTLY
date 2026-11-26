import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { Badge } from './Badge';
import { InfoTile } from './InfoTile';
import { Table } from './Table';

describe('Badge', () => {
  it('renders its label with the chosen tone', () => {
    render(<Badge tone="accent">Admin</Badge>);

    expect(screen.getByText('Admin').className).toContain('text-accent');
  });
});

describe('InfoTile', () => {
  it('renders a label/value pair', () => {
    render(
      <dl>
        <InfoTile label="Email" value="alex@example.com" />
      </dl>,
    );

    expect(screen.getByRole('term')).toHaveTextContent('Email');
    expect(screen.getByRole('definition')).toHaveTextContent('alex@example.com');
  });
});

describe('Table', () => {
  it('renders an accessible table', () => {
    render(
      <Table aria-label="Users">
        <tbody>
          <tr>
            <td>Ada</td>
          </tr>
        </tbody>
      </Table>,
    );

    expect(screen.getByRole('table', { name: 'Users' })).toHaveTextContent('Ada');
  });
});
