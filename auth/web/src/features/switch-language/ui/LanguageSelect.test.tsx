import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import i18next from 'i18next';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { LanguageSelect } from './LanguageSelect';

describe('LanguageSelect', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('shows the current language and switches to the picked one', async () => {
    const changeLanguage = vi.spyOn(i18next, 'changeLanguage').mockResolvedValue(i18next.t);
    const user = userEvent.setup();
    render(<LanguageSelect languages={['en', 'pl']} />);

    const select = screen.getByRole('combobox', { name: 'Language' });
    expect(select).toHaveTextContent('English');

    await user.click(select);
    await user.click(screen.getByRole('option', { name: 'polski' }));

    expect(changeLanguage).toHaveBeenCalledExactlyOnceWith('pl');
  });
});
