import { findUsages, readSourceFiles, translationKeys, type TranslationKey } from './translationUsage';

const usage = 'Usage: pnpm i18n:where <namespace:key>   e.g. features/login:errors, common:brand.name, brand.name (common)';

function parseQuery(query: string): TranslationKey {
  const separator = query.indexOf(':');
  return separator < 0
    ? { key: query, namespace: 'common' }
    : { key: query.slice(separator + 1), namespace: query.slice(0, separator) };
}

function matches(query: TranslationKey, candidate: TranslationKey): boolean {
  return (
    candidate.namespace === query.namespace &&
    (query.key === '' || candidate.key === query.key || candidate.key.startsWith(`${query.key}.`))
  );
}

function report(query: TranslationKey): string[] {
  const files = readSourceFiles();
  const keys = translationKeys().filter((candidate) => matches(query, candidate));

  return keys.length === 0
    ? [`No translation key ${query.namespace}:${query.key}`]
    : keys.flatMap((translationKey) => {
        const usages = findUsages(files, translationKey);
        return [
          `${translationKey.namespace}:${translationKey.key}${usages.length === 0 ? '  (unused)' : ''}`,
          ...usages.map(({ file, line, text }) => `  ${file}:${line}  ${text}`),
        ];
      });
}

const [query] = process.argv.slice(2);
process.stdout.write(`${query ? report(parseQuery(query)).join('\n') : usage}\n`);
process.exitCode = query ? 0 : 1;
