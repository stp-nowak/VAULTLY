// @vitest-environment node
import madge from 'madge';
import path from 'path';
import { fileURLToPath } from 'url';
import { madgeConfig } from './madge.config.ts';
import { describe, it, expect } from 'vitest';

const TestOptions = { timeout: 60_000 };

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

/**
 * To create a report of circular dependencies present in the project:
 * - run `pnpm --filter @vaultly/auth-web deps:circular` (runs `circularDependenciesReport.ts`),
 * - report will be generated as a json file in the same folder.
 * */
describe('Protect the app against circular dependencies', TestOptions, () => {
  it("shouldn't have any circular dependencies in the project", async () => {
    // *** Arrange ***
    const entryPath = path.resolve(__dirname, '../App.tsx');

    // *** Act ***
    const response = await madge(entryPath, madgeConfig);

    // *** Assert ***
    expect(response.circular().length).toEqual(0);
  });
});
