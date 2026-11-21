import { parseSearchWith, stringifySearchWith } from '@tanstack/react-router';

/**
 * Search params of this app are opaque strings minted by the Identity API (`?request=<id>`, `?code=<code>`).
 * The router's default parser JSON-parses values, which would turn an id such as `8e10` or `1234` into a number,
 * so values are kept exactly as they appear in the URL.
 */
export const parseRawSearch = parseSearchWith((value) => value);

/** Counterpart of {@link parseRawSearch}: strings are written as-is, objects as JSON. */
export const stringifyRawSearch = stringifySearchWith(JSON.stringify);

/** Reads a non-empty string search param; anything else counts as missing. */
export function readSearchString(value: unknown): string | undefined {
  if (typeof value === 'number' || typeof value === 'boolean') {
    return String(value);
  }

  return typeof value === 'string' && value.trim() ? value : undefined;
}
