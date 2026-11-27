// @vitest-environment node
import { ESLint } from 'eslint';
import path from 'node:path';
import { describe, expect, it } from 'vitest';

import { srcDir } from './translationUsage';

const rule = 'i18next/no-literal-string';

describe('UI texts', () => {
  it('come from translations, never from literals in JSX', { timeout: 120_000 }, async () => {
    const eslint = new ESLint({ cwd: path.dirname(srcDir), ruleFilter: ({ ruleId }) => ruleId === rule });
    const results = await eslint.lintFiles(['src/**/*.tsx']);

    const literals = results.flatMap((result) =>
      result.messages
        .filter((message) => message.ruleId === rule)
        .map(({ line, message }) => `${path.relative(srcDir, result.filePath).split(path.sep).join('/')}:${line} ${message}`),
    );

    expect(literals).toEqual([]);
  });
});
