/**
 * Full-page navigation that leaves the SPA (sign-in, sign-out).
 * Wrapped so tests can replace it; jsdom does not implement navigation.
 */
export function navigateToUrl(url: string): void {
  window.location.assign(url);
}
