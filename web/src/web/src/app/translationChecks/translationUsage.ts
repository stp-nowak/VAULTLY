import { readdirSync, readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { namespaces } from '../providers/i18n/resources';

export const srcDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');

const translationFile = /[\\/]i18n[\\/][a-z]{2}\.ts$/;
const pluralSuffix = /_(zero|one|two|few|many|other)$/;

interface SourceFile {
  lines: string[];
  path: string;
}

export interface TranslationKey {
  key: string;
  namespace: string;
}

export interface Usage {
  file: string;
  line: number;
  text: string;
}

export function readSourceFiles(): SourceFile[] {
  return readdirSync(srcDir, { recursive: true, withFileTypes: true })
    .filter((entry) => entry.isFile() && /\.tsx?$/.test(entry.name))
    .map((entry) => path.join(entry.parentPath, entry.name))
    .filter((file) => !/\.(test|gen)\.tsx?$/.test(file) && !translationFile.test(file))
    .map((file) => ({ lines: readFileSync(file, 'utf8').split(/\r?\n/), path: file }));
}

function leafKeys(texts: object, prefix = ''): string[] {
  return Object.entries(texts).flatMap(([key, value]) =>
    typeof value === 'string' ? [`${prefix}${key}`] : leafKeys(value as object, `${prefix}${key}.`),
  );
}

/** Every English key, plural forms (`count_one`, `count_other`) folded into one (`count`). */
export function translationKeys(): TranslationKey[] {
  return Object.entries(namespaces).flatMap(([namespace, texts]) =>
    [...new Set(leafKeys(texts.en).map((key) => key.replace(pluralSuffix, '')))].map((key) => ({ key, namespace })),
  );
}

function escapeRegExp(text: string): string {
  return text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

/** Keys built at runtime, e.g. `` `notices.${kind}` ``, as patterns matching every key they can produce. */
function dynamicKeyPatterns(line: string): RegExp[] {
  return [...line.matchAll(/`([\w./:-]*\$\{[^`]*)`/g)].map(
    ([, template]) => new RegExp(`^${template.split(/\$\{[^}]*\}/).map(escapeRegExp).join('[\\w-]+')}$`),
  );
}

function mentions(line: string, key: string): boolean {
  return (
    new RegExp(`['"\`]${escapeRegExp(key)}['"\`]`).test(line) ||
    dynamicKeyPatterns(line).some((pattern) => pattern.test(key))
  );
}

/** Whether a file may use the namespace's keys without the `ns:` prefix: its own slice, or any file for `common`. */
function isOwnFile(namespace: string, file: SourceFile): boolean {
  return namespace === 'common' || file.path.startsWith(path.join(srcDir, namespace));
}

export function findUsages(files: SourceFile[], { key, namespace }: TranslationKey): Usage[] {
  return files.flatMap((file) =>
    file.lines.flatMap((text, index) => {
      const isUsage = (isOwnFile(namespace, file) && mentions(text, key)) || mentions(text, `${namespace}:${key}`);
      return isUsage ? [{ file: path.relative(srcDir, file.path).split(path.sep).join('/'), line: index + 1, text: text.trim() }] : [];
    }),
  );
}
