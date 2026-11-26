import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';

import { renderRoute } from '@/shared/test';

import { ConsoleShell } from './ConsoleShell';

const admin = { email: 'ada@example.com', id: 'u-1', name: 'Ada' };

function renderShell() {
  return renderRoute({
    component: () => <ConsoleShell admin={admin}>Page content</ConsoleShell>,
    initialEntry: '/admin/users',
    path: '/admin/users',
  });
}

describe('ConsoleShell', () => {
  it('shows the page with navigation and the signed-in admin', async () => {
    await renderShell();

    expect(screen.getByText('Page content')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Users' })).toHaveAttribute('href', '/admin/users');
    expect(screen.getByText('ada@example.com')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Sign out' })).toHaveAttribute('href', '/logout');
  });

  it('opens the drawer menu, moves focus into it and closes it on Escape', async () => {
    await renderShell();
    const menuButton = screen.getByRole('button', { name: 'Open menu' });
    expect(menuButton).toHaveAttribute('aria-expanded', 'false');

    await userEvent.click(menuButton);

    expect(menuButton).toHaveAttribute('aria-expanded', 'true');
    expect(screen.getByRole('button', { name: 'Close menu' })).toHaveFocus();

    await userEvent.keyboard('{Escape}');

    expect(menuButton).toHaveAttribute('aria-expanded', 'false');
  });

  it('closes the drawer when a page is picked', async () => {
    await renderShell();
    const menuButton = screen.getByRole('button', { name: 'Open menu' });
    await userEvent.click(menuButton);

    await userEvent.click(screen.getByRole('link', { name: 'Users' }));

    expect(menuButton).toHaveAttribute('aria-expanded', 'false');
  });
});
