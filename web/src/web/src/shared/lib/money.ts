/** Formats a signed amount in its currency, e.g. `-1 234,50 zł`. */
export function formatMoney(amount: number, currency: string, locale: string): string {
  return new Intl.NumberFormat(locale, { currency, style: 'currency' }).format(amount);
}

/**
 * Reads an amount typed by the user: digits with an optional `.` or `,` and up to four decimals, spaces ignored.
 * Returns `null` for anything else.
 */
export function parseAmount(text: string): number | null {
  const normalized = text.replace(/\s/g, '').replace(',', '.');
  return /^-?\d+(\.\d{1,4})?$/.test(normalized) ? Number(normalized) : null;
}
