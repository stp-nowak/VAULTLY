import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { StatusNotice } from './StatusNotice';

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
