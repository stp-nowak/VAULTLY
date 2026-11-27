import { describe, expect, it } from 'vitest';

import { findUsages, readSourceFiles, translationKeys } from './translationUsage';

describe('translations', () => {
  it('uses every English key somewhere in the code', () => {
    const files = readSourceFiles();
    const unused = translationKeys()
      .filter((translationKey) => findUsages(files, translationKey).length === 0)
      .map(({ key, namespace }) => `${namespace}:${key}`);

    expect(unused).toEqual([]);
  });
});
