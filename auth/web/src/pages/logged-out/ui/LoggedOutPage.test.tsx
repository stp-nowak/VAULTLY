import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { LoggedOutPage } from './LoggedOutPage';

describe('LoggedOutPage', () => {
  it('confirms the user is signed out', () => {
    render(<LoggedOutPage />);

    expect(screen.getByRole('heading', { name: "You're signed out." })).toBeInTheDocument();
    expect(screen.getByText('You can close this window.')).toBeInTheDocument();
  });
});
