// Captures PR screenshots of the Vaultly SPA with mocked /api responses.
// Usage: node capture-screenshots.mjs <scenarios.json>
// Requires `playwright-core` installed next to this script and a local Edge or Chrome (no browser download).
import fs from 'node:fs';
import path from 'node:path';

import { chromium } from 'playwright-core';

const configPath = process.argv[2];
if (!configPath) {
  console.error('Usage: node capture-screenshots.mjs <scenarios.json>');
  process.exit(1);
}

const config = JSON.parse(fs.readFileSync(configPath, 'utf8'));
const outDir = path.resolve(path.dirname(configPath), config.outDir ?? 'shots');
const viewports = config.viewports ?? [{ name: 'desktop', width: 1440, height: 900 }];
const colorSchemes = config.colorSchemes ?? ['dark'];
fs.mkdirSync(outDir, { recursive: true });

const browser = await chromium.launch({ channel: config.channel ?? 'msedge' });

try {
  for (const scenario of config.scenarios) {
    for (const viewport of viewports) {
      for (const colorScheme of colorSchemes) {
        const context = await browser.newContext({
          colorScheme,
          deviceScaleFactor: 1,
          viewport: { height: viewport.height, width: viewport.width },
        });
        const page = await context.newPage();

        // Scenario mocks override shared mocks with the same URL pattern.
        const mocks = { ...(config.mocks ?? {}), ...(scenario.mocks ?? {}) };
        for (const [pattern, mock] of Object.entries(mocks)) {
          await page.route(pattern, (route) =>
            route.fulfill({
              body: mock.body === undefined ? '' : JSON.stringify(mock.body),
              contentType: 'application/json',
              status: mock.status ?? 200,
            }),
          );
        }

        const errors = [];
        page.on('pageerror', (error) => errors.push(error.message));

        await page.goto(new URL(scenario.path, config.baseUrl).toString(), { waitUntil: 'networkidle' });
        if (scenario.waitFor) {
          await page.waitForSelector(scenario.waitFor, { timeout: 10_000 });
        }

        const suffix = [viewports.length > 1 ? viewport.name : null, colorSchemes.length > 1 ? colorScheme : null]
          .filter(Boolean)
          .join('-');
        const file = path.join(outDir, `${scenario.name}${suffix ? `-${suffix}` : ''}.png`);
        await page.screenshot({ fullPage: true, path: file });
        console.log(`${errors.length ? 'WARN' : 'ok  '} ${file}${errors.length ? ` (page errors: ${errors.join('; ')})` : ''}`);

        await context.close();
      }
    }
  }
} finally {
  await browser.close();
}
