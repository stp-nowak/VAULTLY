import { describe, expect, it } from 'vitest';

import { initialsOf } from './initials';

describe('initialsOf', () => {
  it.each([
    ['Sebastian Nowak', 'SN'],
    ['Anna Maria Kowalska', 'AK'],
    ['  ada  ', 'A'],
  ])('takes the initials of %s', (name, initials) => {
    expect(initialsOf(name, 'someone@example.com')).toBe(initials);
  });

  it('falls back to the email, then to a question mark', () => {
    expect(initialsOf('', 'grace@example.com')).toBe('G');
    expect(initialsOf('', '')).toBe('?');
  });
});
