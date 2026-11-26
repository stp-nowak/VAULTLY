/**
 * Returns the value when it is a path on this origin (`/…`), otherwise `undefined`. `//host` and `/\host` are
 * rejected because browsers resolve them to another origin.
 */
export function readLocalPath(value: unknown): string | undefined {
  return typeof value === 'string' && /^\/(?![/\\])/.test(value) ? value : undefined;
}
