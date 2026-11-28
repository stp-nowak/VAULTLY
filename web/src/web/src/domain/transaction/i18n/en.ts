export const en = {
  fields: {
    amount: 'Amount',
    amountPlaceholder: '0.00',
    bookedOn: 'Day',
    description: 'Description',
    direction: 'Type',
    expense: 'Expense',
    income: 'Income',
    note: 'Note (optional)',
  },
  problems: {
    amount: 'Enter an amount greater than zero, with at most four decimals.',
    bookedOn: 'Pick the day of the transaction.',
    description: 'Describe the transaction.',
    descriptionLength: 'Keep the description under 200 characters.',
    noteLength: 'Keep the note under 1000 characters.',
  },
  reasons: {
    account_closed: 'the account is closed',
    account_not_found: 'the account was not found',
    currency_mismatch: 'the account uses another currency',
  },
  states: {
    pending: {
      change: 'Saving change…',
      delete: 'Deleting…',
      record: 'Posting…',
    },
    deleted: 'Deleted',
    rejected: 'Rejected: {{reason}}',
    undone: 'Change undone: {{reason}}',
  },
} as const;
