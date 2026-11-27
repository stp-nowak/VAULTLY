import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

import { SearchField } from './SearchField';
import { Select } from './Select';
import { Switch } from './Switch';
import { TextField } from './TextField';

describe('Select', () => {
  const roles = [
    { label: 'User', value: 'user' },
    { label: 'Admin', value: 'admin' },
    { disabled: true, label: 'Owner', value: 'owner' },
  ] as const;

  it('renders its options in a labelled native select whose wrapper takes className', () => {
    const { container } = render(<Select aria-label="Role" className="w-36" defaultValue="admin" options={roles} />);

    const select = screen.getByRole('combobox', { name: 'Role' });
    expect(select).toHaveValue('admin');
    expect(select.className).toContain('appearance-none');
    expect(screen.getAllByRole('option').map((option) => option.textContent)).toEqual(['User', 'Admin', 'Owner']);
    expect(screen.getByRole('option', { name: 'Owner' })).toBeDisabled();
    expect(container.firstElementChild?.className).toContain('w-36');
  });

  it('reports the picked value', () => {
    const onValueChange = vi.fn();
    render(<Select aria-label="Role" defaultValue="user" onValueChange={onValueChange} options={roles} />);

    fireEvent.change(screen.getByRole('combobox', { name: 'Role' }), { target: { value: 'admin' } });

    expect(onValueChange).toHaveBeenCalledExactlyOnceWith('admin');
  });
});

describe('SearchField', () => {
  it('searches for the trimmed term from the icon button inside the box, without a form', () => {
    const onSearch = vi.fn();
    const { container } = render(<SearchField className="w-80" defaultValue="old" label="Search users" onSearch={onSearch} submitLabel="Search" />);

    const box = screen.getByRole('searchbox', { name: 'Search users' });
    const button = screen.getByRole('button', { name: 'Search' });
    fireEvent.change(box, { target: { value: '  ada ' } });
    fireEvent.click(button);

    expect(onSearch).toHaveBeenCalledExactlyOnceWith('ada');
    expect(button).toHaveAttribute('type', 'button');
    expect(box.parentElement).toContainElement(button);
    expect(screen.getByRole('search').className).toContain('w-80');
    expect(container.querySelector('form')).toBeNull();
  });

  it('searches on Enter without submitting a surrounding form', () => {
    const onSearch = vi.fn();
    const onSubmit = vi.fn((event: React.FormEvent) => event.preventDefault());
    render(
      <form onSubmit={onSubmit}>
        <SearchField defaultValue="grace" label="Search users" onSearch={onSearch} submitLabel="Search" />
      </form>,
    );

    fireEvent.keyDown(screen.getByRole('searchbox', { name: 'Search users' }), { key: 'Enter' });

    expect(onSearch).toHaveBeenCalledExactlyOnceWith('grace');
    expect(onSubmit).not.toHaveBeenCalled();
  });
});

describe('Switch', () => {
  it('is a named switch that reports the toggled state', () => {
    const onCheckedChange = vi.fn();
    render(<Switch aria-label="Google" checked onCheckedChange={onCheckedChange} />);

    const toggle = screen.getByRole('switch', { name: 'Google' });
    expect(toggle).toBeChecked();

    fireEvent.click(toggle);

    expect(onCheckedChange).toHaveBeenCalledExactlyOnceWith(false);
  });

  it('does not toggle while disabled', () => {
    const onCheckedChange = vi.fn();
    render(<Switch aria-label="Password" checked={false} disabled onCheckedChange={onCheckedChange} />);

    fireEvent.click(screen.getByRole('switch', { name: 'Password' }));

    expect(onCheckedChange).not.toHaveBeenCalled();
  });
});

describe('TextField', () => {
  it('links the label to the input and styles the wrapper with className', () => {
    const { container } = render(<TextField className="w-64" label="Search" name="search" />);

    expect(screen.getByRole('textbox', { name: 'Search' })).toHaveAttribute('name', 'search');
    expect(container.firstElementChild?.className).toContain('w-64');
  });
});
