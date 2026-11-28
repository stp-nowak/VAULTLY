import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

import { Dialog } from './Dialog';

function renderDialog(onClose = vi.fn()) {
  render(
    <>
      <button type="button">Opener</button>
      <Dialog className="max-w-2xl" closeLabel="Close" onClose={onClose} open title="New transaction">
        <input aria-label="Amount" />
        <button type="button">Save</button>
      </Dialog>
    </>,
  );
  return onClose;
}

describe('Dialog', () => {
  it('is a labelled modal that takes focus into its first field', () => {
    renderDialog();

    const dialog = screen.getByRole('dialog', { name: 'New transaction' });
    expect(dialog).toHaveAttribute('aria-modal', 'true');
    expect(dialog.className).toContain('max-w-2xl');
    expect(screen.getByLabelText('Amount')).toHaveFocus();
  });

  it('renders nothing while closed', () => {
    render(
      <Dialog closeLabel="Close" onClose={vi.fn()} open={false} title="New transaction">
        Content
      </Dialog>,
    );

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('closes with Escape, the close button and a click outside, not a click inside', () => {
    const onClose = renderDialog();
    const dialog = screen.getByRole('dialog');

    fireEvent.mouseDown(dialog);
    expect(onClose).not.toHaveBeenCalled();

    fireEvent.keyDown(dialog, { key: 'Escape' });
    fireEvent.click(screen.getByRole('button', { name: 'Close' }));
    fireEvent.mouseDown(dialog.parentElement!);

    expect(onClose).toHaveBeenCalledTimes(3);
  });

  it('keeps Tab inside the panel', () => {
    renderDialog();
    const save = screen.getByRole('button', { name: 'Save' });
    save.focus();

    fireEvent.keyDown(save, { key: 'Tab' });

    expect(document.activeElement).not.toBe(screen.getByRole('button', { name: 'Opener', hidden: true }));
    expect(screen.getByRole('dialog')).toContainElement(document.activeElement as HTMLElement);
  });
});
