import { describe, expect, it } from 'vitest';

import { languageName } from './languageName';

describe('languageName', () => {
  it('names a language in that language', () => {
    expect(languageName('en')).toBe('English');
    expect(languageName('pl')).toBe('polski');
  });
});
