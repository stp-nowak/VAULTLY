import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';

import { server } from '@/shared/test/msw/server';
import { pickOption } from '@/shared/test/pickOption';

import { startFakeMoneyApi } from './test/fakeMoneyApi';
import { renderApp } from './test/renderApp';

const settled = { timeout: 4000 };

let api: ReturnType<typeof startFakeMoneyApi>;

beforeEach(() => {
  server.use(
    http.get('*/api/auth/user', () =>
      HttpResponse.json({ sessionId: 'session-1', user: { email: 'alex@example.com', id: 'user-1', name: 'Alex Kowalski' } }),
    ),
    http.get('*/api/auth/sessions', () => HttpResponse.json({ sessions: [] })),
  );
  api = startFakeMoneyApi();
});

function transactionRow(description: string) {
  return screen.getByText(description).closest('tr')!;
}

describe('accounts', () => {
  it('lists the accounts with their balances and the net worth', async () => {
    api.addAccount({ name: 'Everyday', openingBalance: 1200 });
    api.addAccount({ name: 'Credit card', openingBalance: -300, type: 'credit_card' });

    renderApp('/app');

    const everyday = await screen.findByRole('link', { name: /Everyday/ });
    expect(everyday).toHaveTextContent('PLN 1,200.00');
    expect(screen.getByRole('link', { name: /Credit card/ })).toHaveTextContent('-PLN 300.00');
    const netWorth = screen.getByRole('region', { name: 'Net worth' });
    expect(await within(netWorth).findByText('PLN 900.00')).toBeInTheDocument();
  });

  it('shows three accounts and three currencies on the overview and links to all of them', async () => {
    for (const [index, currency] of ['PLN', 'EUR', 'USD', 'GBP', 'CHF'].entries()) {
      api.addAccount({ currency, name: `Account ${index + 1}`, openingBalance: 100 });
    }

    const { router } = renderApp('/app');

    await screen.findByRole('link', { name: /Account 1/ });
    expect(screen.getAllByRole('link', { name: /^Account \d/ })).toHaveLength(3);
    const netWorth = screen.getByRole('region', { name: 'Net worth' });
    expect(await within(netWorth).findAllByText(/^Net worth · /)).toHaveLength(3);

    await userEvent.click(within(netWorth).getByRole('link', { name: 'View all 5 currencies' }));
    expect(await screen.findByRole('heading', { name: 'What you are worth' })).toBeInTheDocument();
    expect(await screen.findAllByText(/^Net worth · /)).toHaveLength(5);

    await userEvent.click(screen.getByRole('link', { name: 'Overview' }));
    await userEvent.click(await screen.findByRole('link', { name: 'View all 5' }));
    expect(await screen.findByRole('heading', { name: 'All your accounts' })).toBeInTheDocument();
    expect(await screen.findAllByRole('link', { name: /^Account \d/ })).toHaveLength(5);
    expect(router.state.location.pathname).toBe('/app/accounts');
  });

  it('shows closed accounts on the accounts page on request', async () => {
    api.addAccount({ name: 'Everyday' });
    api.addAccount({ closedAt: '2026-11-20T10:00:00Z', name: 'Old savings' });

    renderApp('/app/accounts');

    await screen.findByRole('link', { name: /Everyday/ });
    expect(screen.queryByRole('link', { name: /Old savings/ })).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole('switch', { name: 'Show closed' }));

    expect(await screen.findByRole('link', { name: /Old savings/ })).toHaveTextContent('Checking · Closed');
  });

  it('opens an account', async () => {
    renderApp('/app');

    await userEvent.click(await screen.findByRole('button', { name: 'Open an account' }));
    const form = screen.getByRole('form', { name: 'Open an account' });
    await userEvent.type(within(form).getByLabelText('Name'), 'Holiday savings');
    await pickOption(within(form).getByRole('combobox', { name: 'Type' }), 'Savings');
    await pickOption(within(form).getByRole('combobox', { name: 'Currency' }), /^EUR/);
    await userEvent.clear(within(form).getByLabelText('Opening balance'));
    await userEvent.type(within(form).getByLabelText('Opening balance'), '250,50');
    await userEvent.click(within(form).getByRole('button', { name: 'Open account' }));

    expect(await screen.findByRole('link', { name: /Holiday savings/ })).toHaveTextContent('€250.50');
    expect(screen.queryByRole('form', { name: 'Open an account' })).not.toBeInTheDocument();
  });

  it('asks for a name before opening an account', async () => {
    renderApp('/app');

    await userEvent.click(await screen.findByRole('button', { name: 'Open an account' }));
    await userEvent.click(screen.getByRole('button', { name: 'Open account' }));

    expect(screen.getByRole('alert')).toHaveTextContent('Give the account a name');
  });

  it('renames and closes an account', async () => {
    const account = api.addAccount({ name: 'Everyday' });
    renderApp(`/app/accounts/${account.id}`);

    await userEvent.click(await screen.findByRole('button', { name: 'Rename' }));
    const name = screen.getByLabelText('New name');
    await userEvent.clear(name);
    await userEvent.type(name, 'Main account');
    await userEvent.click(screen.getByRole('button', { name: 'Save' }));
    expect(await screen.findByRole('heading', { level: 2, name: 'Main account' })).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Close account' }));
    await userEvent.click(screen.getByRole('button', { name: 'Yes, close it' }));

    expect(await screen.findByText('Checking · Closed')).toBeInTheDocument();
    expect(screen.queryByRole('form', { name: 'Add a transaction' })).not.toBeInTheDocument();
  });

  it('says when the account does not exist', async () => {
    renderApp('/app/accounts/missing');

    expect(await screen.findByRole('alert')).toHaveTextContent('This account does not exist.');
  });
});

describe('transactions of an account', () => {
  it('records an expense, shows it pending, then the new balance and its history', async () => {
    const account = api.addAccount({ name: 'Everyday', openingBalance: 100 });
    renderApp(`/app/accounts/${account.id}`);

    await userEvent.click(await screen.findByRole('button', { name: 'New transaction' }));
    const form = screen.getByRole('form', { name: 'Add a transaction' });
    await userEvent.type(within(form).getByLabelText('Amount'), '40');
    await userEvent.clear(within(form).getByLabelText('Day'));
    await userEvent.type(within(form).getByLabelText('Day'), '2026-11-27');
    await userEvent.type(within(form).getByLabelText('Description'), 'Groceries');
    await userEvent.click(within(form).getByRole('button', { name: 'Add transaction' }));

    expect(await screen.findByText('Posting…')).toBeInTheDocument();
    await waitFor(() => expect(screen.queryByText('Posting…')).not.toBeInTheDocument(), settled);
    expect(transactionRow('Groceries')).toHaveTextContent('-PLN 40.00');
    expect(await screen.findByText('PLN 60.00', { selector: 'p' }, settled)).toBeInTheDocument();
    expect(screen.getByRole('img', { name: /Balance from/ })).toBeInTheDocument();
  });

  it('explains what is wrong with the transaction before sending it', async () => {
    const account = api.addAccount({ name: 'Everyday' });
    renderApp(`/app/accounts/${account.id}`);

    await userEvent.click(await screen.findByRole('button', { name: 'New transaction' }));
    const form = screen.getByRole('form', { name: 'Add a transaction' });
    await userEvent.type(within(form).getByLabelText('Amount'), '0');
    await userEvent.click(within(form).getByRole('button', { name: 'Add transaction' }));

    expect(within(form).getByRole('alert')).toHaveTextContent('Enter an amount greater than zero');
  });

  it('changes and deletes a transaction', async () => {
    const account = api.addAccount({ name: 'Everyday', openingBalance: 100 });
    api.addTransaction(account.id, -20, '2026-11-20', 'Cinema');
    renderApp(`/app/accounts/${account.id}`);

    await userEvent.click(await screen.findByRole('button', { name: 'Change Cinema' }));
    const form = screen.getByRole('form', { name: 'Change the transaction' });
    await userEvent.clear(within(form).getByLabelText('Amount'));
    await userEvent.type(within(form).getByLabelText('Amount'), '35');
    await userEvent.click(within(form).getByRole('button', { name: 'Save' }));

    expect(await screen.findByText('Saving change…')).toBeInTheDocument();
    await waitFor(() => expect(transactionRow('Cinema')).toHaveTextContent('-PLN 35.00'), settled);
    await waitFor(() => expect(screen.queryByText('Saving change…')).not.toBeInTheDocument(), settled);

    await userEvent.click(screen.getByRole('button', { name: 'Delete Cinema' }));

    expect(await screen.findByText('Deleting…')).toBeInTheDocument();
    expect(await screen.findByText('No transactions here yet.', undefined, settled)).toBeInTheDocument();
  });

  it('shows a transaction Accounts rejected and lets the user delete it', async () => {
    const account = api.addAccount({ name: 'Everyday' });
    api.addTransaction(account.id, -15, '2026-11-20', 'Taxi', true);
    renderApp(`/app/accounts/${account.id}`);

    expect(await screen.findByText('Rejected: the account is closed')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Change Taxi' })).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Delete Taxi' }));

    expect(await screen.findByText('No transactions here yet.')).toBeInTheDocument();
  });

  it('explains that a closed account takes no new transactions', async () => {
    const account = api.addAccount({ name: 'Everyday' });
    server.use(http.post('*/api/transactions', () => new HttpResponse(null, { status: 409 })));
    renderApp(`/app/accounts/${account.id}`);

    await userEvent.click(await screen.findByRole('button', { name: 'New transaction' }));
    const form = screen.getByRole('form', { name: 'Add a transaction' });
    await userEvent.type(within(form).getByLabelText('Amount'), '15');
    await userEvent.type(within(form).getByLabelText('Description'), 'Taxi');
    await userEvent.click(within(form).getByRole('button', { name: 'Add transaction' }));

    expect(await within(form).findByRole('alert')).toHaveTextContent('The account is closed');
  });
});

describe('transactions page', () => {
  it('lists every account newest first and loads older pages', async () => {
    const everyday = api.addAccount({ name: 'Everyday' });
    const savings = api.addAccount({ name: 'Savings', type: 'savings' });
    for (let day = 1; day <= 26; day += 1) {
      api.addTransaction(everyday.id, -day, `2026-10-${String(day).padStart(2, '0')}`, `Coffee ${day}`);
    }
    api.addTransaction(savings.id, 500, '2026-11-02', 'Salary');

    renderApp('/app/transactions');

    const table = await screen.findByRole('table', { name: 'Transactions' });
    const rows = within(table).getAllByRole('row');
    expect(rows[1]).toHaveTextContent('Salary');
    expect(rows[1]).toHaveTextContent('Savings');
    expect(screen.queryByText('Coffee 1')).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Show older transactions' }));

    expect(await screen.findByText('Coffee 1')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Show older transactions' })).not.toBeInTheDocument();
  });

  it('filters by account and keeps the filter in the address', async () => {
    const everyday = api.addAccount({ name: 'Everyday' });
    const savings = api.addAccount({ name: 'Savings', type: 'savings' });
    api.addTransaction(everyday.id, -12, '2026-11-02', 'Lunch');
    api.addTransaction(savings.id, 500, '2026-11-02', 'Salary');

    const { router } = renderApp('/app/transactions');

    await screen.findByText('Lunch');
    const list = screen.getByRole('region', { name: 'History' });
    await pickOption(within(list).getByRole('combobox', { name: 'Account' }), 'Savings');

    await waitFor(() => expect(screen.queryByText('Lunch')).not.toBeInTheDocument());
    expect(screen.getByText('Salary')).toBeInTheDocument();
    expect(router.state.location.search).toEqual({ accountId: savings.id });
  });

  it('records a transaction on the picked account', async () => {
    api.addAccount({ name: 'Everyday' });
    api.addAccount({ name: 'Savings', type: 'savings' });

    renderApp('/app/transactions');

    await userEvent.click(await screen.findByRole('button', { name: 'New transaction' }));
    const form = screen.getByRole('form', { name: 'Add a transaction' });
    await pickOption(within(form).getByRole('combobox', { name: 'Account' }), 'Savings · PLN');
    await pickOption(within(form).getByRole('combobox', { name: 'Type' }), 'Income');
    await userEvent.type(within(form).getByLabelText('Amount'), '1000');
    await userEvent.type(within(form).getByLabelText('Description'), 'Bonus');
    await userEvent.click(within(form).getByRole('button', { name: 'Add transaction' }));

    await waitFor(() => expect(transactionRow('Bonus')).toHaveTextContent('Savings'));
    expect(transactionRow('Bonus')).toHaveTextContent('PLN 1,000.00');
  });

  it('asks to open an account first when there is none', async () => {
    renderApp('/app/transactions');

    expect(await screen.findByText('You need an open account to add transactions.')).toBeInTheDocument();
  });
});
