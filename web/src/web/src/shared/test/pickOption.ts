import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

/** Opens a `Select` from `@vaultly/ui` and picks the option with the given name. */
export async function pickOption(select: HTMLElement, option: string | RegExp) {
  await userEvent.click(select);
  await userEvent.click(screen.getByRole('option', { name: option }));
}
